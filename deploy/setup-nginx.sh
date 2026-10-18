#!/usr/bin/env bash
# =============================================================================
# Cau hinh nginx reverse proxy cho web container.
#
# HTTPS: KHONG cai SSL tai origin.
#   Domain dang dung Cloudflare proxy (giong du an Managerment-rent-room
#   dang chay tren cung VPS nay), nen:
#       khach truy cap --HTTPS--> Cloudflare --HTTP--> nginx (origin) --> web
#   nginx chi can listen 80. Neu sau nay chuyen Cloudflare sang Full (strict)
#   thi moi can them cert origin (Let's Encrypt hoac Cloudflare Origin CA).
#
# An toan chay lai nhieu lan (idempotent) - deploy chay moi lan push.
#
# Bien doc duoc:
#   NGINX_DOMAIN - bat buoc. De trong = bo qua cau hinh nginx.
#   WEB_PORT     - mac dinh lay trong .env cua compose (8080). Doc tu do de
#                  khong lech voi docker-compose.yml.
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

# Server nay SSH bang root -> khong dung/ khong can sudo.
# Nhung may nao chay bang user thuong van dung duoc sudo.
SUDO=""
if [ "$(id -u)" -ne 0 ]; then
  SUDO="sudo"
fi

CONF_AVAIL="/etc/nginx/sites-available"
CONF_EN="/etc/nginx/sites-enabled"
CONF="$CONF_AVAIL/${DOMAIN}.conf"

echo "[nginx] domain=$DOMAIN web_port=$WEB_PORT chay_quyen=$(id -un)"

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
{
  echo "# Tu dong tao boi deploy/setup-nginx.sh - khong sua tay, se bi ghi de."
  echo "# HTTPS do Cloudflare xu ly, origin chi phuc vu HTTP."
  echo "server {"
  echo "    listen 80;"
  echo "    listen [::]:80;"
  echo "    server_name $DOMAIN www.$DOMAIN;"
  echo ""
  echo "    # mac dinh nginx chi cho 1MB -> an qua se chan upload anh phong"
  echo "    client_max_body_size 20m;"
  echo ""
  echo "    location / {"
  echo "        proxy_pass http://127.0.0.1:$WEB_PORT;"
  echo "        proxy_http_version 1.1;"
  echo "        proxy_set_header Host \$host;"
  echo "        proxy_set_header X-Real-IP \$remote_addr;"
  echo "        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;"
  # Cloudflare da gui X-Forwarded-Proto (https) -> giu nguyen, khong de $scheme
  # ghi de thanh http, de app biet dung scheme khi sinh URL tuyet doi.
  echo "        proxy_set_header X-Forwarded-Proto \$http_x_forwarded_proto;"
  echo "        proxy_read_timeout 120s;"
  echo "        proxy_send_timeout 120s;"
  echo "    }"
  echo "}"
} | $SUDO tee "$CONF" >/dev/null

$SUDO ln -sfn "$CONF" "$CONF_EN/$(basename "$CONF")"
$SUDO nginx -t
$SUDO systemctl reload nginx

echo "[nginx] hoan tat: http://$DOMAIN  (khach van thay https nho Cloudflare)"
