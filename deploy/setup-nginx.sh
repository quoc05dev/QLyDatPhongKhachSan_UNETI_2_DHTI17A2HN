#!/usr/bin/env bash
# =============================================================================
# Cau hinh nginx reverse proxy cho web container.
#
# HTTPS:
#   - Luon phuc vu HTTP :80 (Cloudflare co the dang o che do Flexible).
#   - Neu da co cert Let's Encrypt cho domain thi them server :443 de ho tro
#     Cloudflare Full/Full (strict), tranh 502 do request HTTPS roi vao
#     default_server cua domain khac tren VPS.
#   - Tao cert lan dau (sau khi DNS da tro ve VPS):
#       certbot certonly --nginx -d example.com -d www.example.com
#
# An toan chay lai nhieu lan (idempotent) - deploy chay moi lan push.
#
# Bien doc duoc:
#   NGINX_DOMAIN - bat buoc. De trong = bo qua cau hinh nginx.
#   WEB_PORT     - mac dinh lay trong .env cua compose (8080). Doc tu do de
#                  khong lech voi docker-compose.yml.
#   PROXY_TARGET - upstream backend. Mac dinh: 127.0.0.1:WEB_PORT (Docker Compose).
#                  Sau chuyen production sang Kubernetes, dat trong .env:
#                    PROXY_TARGET=127.0.0.1:30444   (NodePort cua Service K8s)
#                  Chi dat PROXY_TARGET la du, khong can doi WEB_PORT.
# =============================================================================
set -euo pipefail

DOMAIN="${NGINX_DOMAIN:-}"
if [ -z "$DOMAIN" ]; then
  echo "[nginx] NGINX_DOMAIN chua dat -> bo qua cau hinh nginx."
  exit 0
fi

# Lay WEB_PORT tu .env (truong hop khong truyen vao tu CI)
if [ -z "${WEB_PORT:-}" ] && [ -f .env ]; then
  WEB_PORT="$(grep -E '^[[:space:]]*WEB_PORT=' .env | tail -1 | cut -d= -f2- | tr -d '[:space:]' || true)"
fi
WEB_PORT="${WEB_PORT:-8080}"

# PROXY_TARGET: upstream hien tai. Doc tu .env neu khong truyen vao tu ben ngoai.
# Sau khi chuyen sang K8s: PROXY_TARGET=127.0.0.1:30444 (NodePort Service).
if [ -z "${PROXY_TARGET:-}" ] && [ -f .env ]; then
  PROXY_TARGET="$(grep -E '^[[:space:]]*PROXY_TARGET=' .env | tail -1 | cut -d= -f2- | tr -d '[:space:]' || true)"
fi
PROXY_TARGET="${PROXY_TARGET:-127.0.0.1:$WEB_PORT}"

# Server nay SSH bang root -> khong dung/ khong can sudo.
# Nhung may nao chay bang user thuong van dung duoc sudo.
SUDO=""
if [ "$(id -u)" -ne 0 ]; then
  SUDO="sudo"
fi

CONF_AVAIL="/etc/nginx/sites-available"
CONF_EN="/etc/nginx/sites-enabled"
CONF="$CONF_AVAIL/${DOMAIN}.conf"
SSL_CERT="/etc/letsencrypt/live/${DOMAIN}/fullchain.pem"
SSL_KEY="/etc/letsencrypt/live/${DOMAIN}/privkey.pem"
HAS_SSL=0
if [ -f "$SSL_CERT" ] && [ -f "$SSL_KEY" ]; then
  HAS_SSL=1
fi

echo "[nginx] domain=$DOMAIN proxy_target=$PROXY_TARGET ssl=$HAS_SSL chay_quyen=$(id -un)"

# --- 1. cai nginx neu thieu ---------------------------------------------------
if ! command -v nginx >/dev/null 2>&1; then
  echo "[nginx] cai dat nginx..."
  $SUDO apt-get update -y
  $SUDO DEBIAN_FRONTEND=noninteractive apt-get install -y nginx
fi

$SUDO mkdir -p "$CONF_AVAIL" "$CONF_EN"
$SUDO systemctl enable --now nginx >/dev/null 2>&1 || true

# --- 2. dam bao nginx.conf co include sites-enabled ---------------------------
# (VPS nay da chay du an Managerment-rent-room -> co the da co; chi them khi thieu)
if ! grep -Eq 'include[[:space:]]+/etc/nginx/sites-enabled/' /etc/nginx/nginx.conf; then
  echo "[nginx] them 'include sites-enabled' vao nginx.conf"
  $SUDO sed -i '/http[[:space:]]*{/a\    include /etc/nginx/sites-enabled/*;' /etc/nginx/nginx.conf
fi

# --- 3. viet conf --------------------------------------------------------------
emit_proxy_location() {
  local indent="${1:-    }"
  echo "${indent}location / {"
  echo "${indent}    proxy_pass http://$PROXY_TARGET;"
  echo "${indent}    proxy_http_version 1.1;"
  echo "${indent}    proxy_set_header Host \$host;"
  echo "${indent}    proxy_set_header X-Real-IP \$client_ip;"
  echo "${indent}    proxy_set_header X-Forwarded-For \$client_ip;"
  # Origin chi nhan ket noi qua Cloudflare (da terminate TLS), nen luon bao
  # https cho app. Khong dung \$http_x_forwarded_proto vi client co the spoof.
  echo "${indent}    proxy_set_header X-Forwarded-Proto https;"
  echo "${indent}    proxy_set_header X-Forwarded-Host \$host;"
  echo "${indent}    proxy_read_timeout 120s;"
  echo "${indent}    proxy_send_timeout 120s;"
  echo "${indent}}"
}

{
  echo "# Tu dong tao boi deploy/setup-nginx.sh - khong sua tay, se bi ghi de."
  echo "# HTTP :80 luon bat; HTTPS :443 bat khi co cert Let's Encrypt."
  echo "# Lay IP that cua khach tu Cloudflare; fallback remote_addr khi khong qua CF."
  echo "map \$http_cf_connecting_ip \$client_ip {"
  echo "    default \$http_cf_connecting_ip;"
  echo "    \"\"      \$remote_addr;"
  echo "}"
  echo ""
  echo "server {"
  echo "    listen 80;"
  echo "    listen [::]:80;"
  echo "    server_name $DOMAIN www.$DOMAIN;"
  echo ""
  echo "    # mac dinh nginx chi cho 1MB -> an qua se chan upload anh phong"
  echo "    client_max_body_size 20m;"
  echo ""
  emit_proxy_location "    "
  echo "}"
  echo ""
  if [ "$HAS_SSL" -eq 1 ]; then
    echo "server {"
    echo "    listen 443 ssl http2;"
    echo "    listen [::]:443 ssl http2;"
    echo "    server_name $DOMAIN www.$DOMAIN;"
    echo ""
    echo "    ssl_certificate $SSL_CERT;"
    echo "    ssl_certificate_key $SSL_KEY;"
    echo "    ssl_protocols TLSv1.2 TLSv1.3;"
    echo "    ssl_ciphers HIGH:!aNULL:!MD5;"
    echo "    ssl_session_cache shared:SSL:10m;"
    echo "    ssl_session_timeout 10m;"
    echo ""
    echo "    # mac dinh nginx chi cho 1MB -> an qua se chan upload anh phong"
    echo "    client_max_body_size 20m;"
    echo ""
    emit_proxy_location "    "
    echo "}"
  fi
} | $SUDO tee "$CONF" >/dev/null

if [ "$HAS_SSL" -eq 0 ]; then
  echo "[nginx] CANH BAO: chua co $SSL_CERT -> chi cau hinh HTTP :80." >&2
  echo "[nginx] De ho tro Cloudflare Full/Full strict, chay:" >&2
  echo "[nginx]   certbot certonly --nginx -d $DOMAIN -d www.$DOMAIN" >&2
fi

$SUDO ln -sfn "$CONF" "$CONF_EN/$(basename "$CONF")"
$SUDO nginx -t
$SUDO systemctl reload nginx

echo "[nginx] hoan tat: http://$DOMAIN (https=$HAS_SSL)"
