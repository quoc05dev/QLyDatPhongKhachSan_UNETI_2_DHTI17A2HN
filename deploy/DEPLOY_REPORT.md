# BÁO CÁO DEPLOY + RUNBOOK TÁI SỬ DỤNG

> Dự án: QLyDatPhongKhachSan_UNETI_2_DHTI17A2HN (ASP.NET Core 10 MVC + SQL Server + Docker)
> Kết quả: pipeline CD chạy thành công, app truy cập qua `http://<domain>` (Cloudflare proxy → nginx origin :80 → web container :8090).
> Tài liệu này gồm 2 phần: **Phần A** là báo cáo debug thực tế, **Phần B** là runbook chuẩn tái sử dụng cho project khác.

---

# PHẦN A — BÁO CÁO DEBUG THỰC TẾ

Pipeline: push `main` → CI gate → build & push image GHCR → SSH lên server → pull image → `docker compose up` → healthcheck → nginx.

Tổng cộng 6 sự cố, sửa theo thứ tự từ client → network → config → hạ tầng.

## Sự cố 1 — SSH key bị passphrase bảo vệ

**Log:**

```
ssh.ParsePrivateKey: ssh: this private key is passphrase protected
ssh: handshake failed: ssh: unable to authenticate, attempted methods [none], no supported methods remain
Error: Process completed with exit code 1
```

**Nguyên nhân:** Secret `DEPLOY_SSH_KEY` chứa private key được mã hóa bằng passphrase, nhưng step `appleboy/ssh-action` không truyền input `passphrase`. Key không parse được → không có method auth nào gửi lên server (chỉ còn `none`) → handshake fail.

**Phân biệt 2 case auth:**

| Log | Nghĩa |
|---|---|
| `attempted methods [none]` | Key không load được (lỗi phía client) |
| `attempted methods [none publickey]` | Key load được nhưng server từ chối (sai authorized_keys / sai user) |

**Cách xác minh key có bị mã hóa không (PowerShell):**

```powershell
# Đọc 64 byte đầu của blob base64 -> hiện ASCII "none" hay "aes256-ctr"
$b64 = ((Get-Content "$env:USERPROFILE\.ssh\gha_deploy") | Where-Object { $_ -notmatch 'BEGIN|END' }) -join ''
$bytes = [Convert]::FromBase64String($b64)
[Text.Encoding]::ASCII.GetString($bytes[0..64])
```

- Chứa `none` → key sạch, dùng được.
- Chứa `aes256-ctr` + `bcrypt` → key bị passphrase bảo vệ, phải tạo lại.

**Cách sửa:** tạo key mới không passphrase (xem Part B, GĐ 1), update secret (GĐ 3).

**Bẫy gặp phải khi debug:**
- `ssh-keygen -t ... -N ""` fail "Too many arguments" — PowerShell/cmd nuốt chuỗi rỗng `""`, khiến `-N` nuốt luôn `-f`. Bỏ `-N ""`, nhập passphrase rỗng tương tác (2 lần Enter).
- Key v1 được tạo vô tình có passphrase (shell nuốt `""` lúc test không verify được) → phải dùng lệnh verify cipher ở trên thay vì tin vào thao tác gõ phím.

## Sự cố 2 — Thư mục deploy không tồn tại trên server

**Log:**

```
bash: line 9: cd: /***/Quoc/QLyDatPhongKhachSan: No such file or directory
2026/10/20 07:25:08 Process exited with status 1
```

**Nguyên nhân:** Variable `DEPLOY_PATH` = `/root/Quoc/QLyDatPhongKhachSan` nhưng repo được clone thành `/root/Quoc/QLyDatPhongKhachSan_UNETI_2_DHTI17A2HN`. Script deploy `cd "$WORKDIR"` fail ngay dòng đầu.

**Lưu ý về mask `***`:** GitHub Actions che (mask) các chuỗi trùng với giá trị secret trong log. `DEPLOY_USER=root` → chuỗi `/root/...` bị che thành `/***/...`. Đây không phải ký tự lạ, đừng tìm "path có dấu sao".

**Cách sửa:** Settings → Secrets and variables → Actions → **Variables** → sửa `DEPLOY_PATH` thành đường dẫn thật.

## Sự cố 3 — Thiếu `.env` trên server

**Phát hiện:** sau khi fix `DEPLOY_PATH`, `ls .env` báo `No such file or directory` trong khi `docker-compose.yml` đọc `${MSSQL_SA_PASSWORD}`, `${WEB_PORT}`, `${DB_PORT}` từ `.env`.

**Nguyên nhân:** `.env` nằm trong `.gitignore` → không bao giờ sync qua `git pull`/`git reset`. Phải tạo thủ công trên từng server.

**Cách sửa:**

```bash
cd /root/Quoc/QLyDatPhongKhachSan_UNETI_2_DHTI17A2HN
cp .env.example .env
sed -i 's/MSSQL_SA_PASSWORD=Your_password123/MSSQL_SA_PASSWORD=<mat-khau-manh>/' .env
cat .env   # xác nhận đã thay
```

**An toàn với CD:** `.env` là untracked file → lệnh `git reset --hard origin/$REF` trong script deploy **không** xoá nó.

## Sự cố 4 — Container db unhealthy vì RAM bị chiếm

**Log:**

```
Container qlydatphongkhachsan_uneti_2_dhti17a2hn-db-1  Error
dependency failed to start: container ...-db-1 is unhealthy
```

**Chẩn đoán (trên server):**

```bash
docker inspect --format 'restarts={{.RestartCount}} health={{.State.Health.Status}}' <db-container>
docker stats --no-stream
ps aux --sort=-%mem | head -12
free -m
df -h /
```

**Kết quả thực tế:** `restarts=5 health=unhealthy`, RAM máy 188GB/190GB, swap đầy 100%. `docker stats` chỉ ra `rental-activemq` (thuộc project cũ `Managerment-rent-room`, xác nhận bằng `docker inspect rental-activemq --format '{{index .Config.Labels "com.docker.compose.project.working_dir"}}'`) đang chiếm **172.6GB (92.6%), CPU 1887%, 4095 PIDs** → ActiveMQ runaway. SQL Server bị OOM kill liên tục, không qua được healthcheck.

**Cách sửa:**

```bash
docker stop rental-activemq
docker update --memory=2g --memory-swap=2g rental-activemq   # limit phòng tái diễn
docker start rental-activemq    # hoặc bỏ dòng này nếu project cũ không còn dùng
free -m    # xác nhận available >= 3-4GB
```

## Sự cố 5 — Container db exit 18, model DB hỏng

**Sau khi nhả RAM, db vẫn chết:**

```
docker inspect ... → restarts=5 exit=18 oom=false
```

**Log cuối:**

```
Error: 945, Severity: 14, State: 2.
Database 'model' cannot be opened due to inaccessible files or insufficient memory or disk space.
```

**Nguyên nhân:** các lần bị OOM kill trước xảy ra giữa chừng lúc SQL Server init → file `model.mdf` trong volume `mssql-data` hỏng. SQL Server không tự phục hồi, exit 18 mỗi lần start. `restart: on-failure:5` đã chạm giới hạn → container dừng hẳn.

**Lưu ý — log noise:** đại lượng lỗi `FCB::Open failed: Could not open file F:\dbs\sh\...\model.mdf` là **noise nổi tiếng** của image `mcr.microsoft.com/mssql/server` (đường dẫn build-time của Microsoft). Đừng mất công debug dòng này. Lỗi thật là 945 + `exit=18`.

**Cách sửa:** xoá volume hỏng, init lại sạch (chưa có data quan trọng, seed data tự tạo lại):

```bash
cd /root/Quoc/QLyDatPhongKhachSan_UNETI_2_DHTI17A2HN
docker compose down -v    # -v xoá cả volume mssql-data
docker compose up -d
watch docker compose ps   # db: starting -> healthy (~40-90s)
```

**Kết quả:** `db Healthy 5.6s`, `web Started` — volume sạch hoạt động ngay.

## Sự cố 6 — Healthcheck state cũ làm compose fail ngay lập tức

**Triệu chứng:** `docker compose up -d` báo `db is unhealthy` chỉ sau ~7s (không đợi healthcheck timeout thật).

**Nguyên nhân:** container db cũ đang ở state `unhealthy` → compose đọc state cũ, không tự recreate. Health state chỉ reset khi **tái tạo container**.

**Cách sửa:**

```bash
docker compose up -d --force-recreate db
```

---

# PHẦN B — RUNBOOK TÁI SỬ DỤNG

Quy trình chuẩn đưa project Docker lên server bằng GitHub Actions CD, tái sử dụng cho project khác. Mỗi giai đoạn gồm: mục tiêu → lệnh từng bước → kết quả mong đợi → **kiểm thử** (pass/fail rõ ràng).

Placeholder throughout: `<user>` = user SSH, `<host>` = IP/domain server, `<repo-dir>` = thư mục clone trên server, `<fp>` = giá trị fingerprint SHA256.

---

## GĐ 0 — Chuẩn bị server

**Mục tiêu:** server sẵn sàng chạy Docker Compose, đủ tài nguyên.

**0.1 Cài Docker + Compose** (bỏ qua nếu đã có):

```bash
apt-get update -y
apt-get install -y docker.io docker-compose-v2
systemctl enable --now docker
```

**0.2 Kiểm tra năng lực — bắt buộc trước mọi lần deploy:**

```bash
docker version                 # client + server phải hiện version
docker compose version         # phải là "Docker Compose version v2..."
free -m                        # available >= 4000 (MB)
df -h /                        # Use% < 85%
ss -tlnp | grep -E ':(80|443|8080|8090)\b'   # xem port đã bị chiếm
```

**Kiểm thử GĐ 0 (pass):** cả 4 lệnh trên không报 error; `free -m` available >= 4GB; port muốn dùng (`8090`, `14330` trong project này) **không** xuất hiện trong output `ss`.

> Quy tắc vàng: **không rerun pipeline khi RAM available < 4GB.** SQL Server cần ~2GB riêng nó.

---

## GĐ 1 — Tạo SSH key pair cho CI

**Mục tiêu:** key ed25519 **không passphrase**, riêng cho CI (không dùng chung key cá nhân).

**1.1 Tạo key** (PowerShell trên Windows hoặc terminal Linux/macOS):

```powershell
ssh-keygen -t ed25519 -C "gha-deploy" -f "$env:USERPROFILE\.ssh\gha_deploy"
```

Khi hiện `Enter passphrase (empty for no passphrase):` → **chỉ bấm Enter, không gõ gì** (terminal không hiện ký tự, dễ tưởng đã gõ). Làm tương tự dòng xác nhận.

**1.2 KIỂM THỬ 1a — cipher phải là `none` (kiểm tra bắt buộc):**

```powershell
$b64 = ((Get-Content "$env:USERPROFILE\.ssh\gha_deploy") | Where-Object { $_ -notmatch 'BEGIN|END' }) -join ''
$bytes = [Convert]::FromBase64String($b64)
[Text.Encoding]::ASCII.GetString($bytes[0..64])
```

- PASS: output chứa `none`.
- FAIL: output chứa `aes256-ctr` / `bcrypt` → key có passphrase → tạo lại từ 1.1.

**1.3 KIỂM THỬ 1b — private key sinh ra đúng public key:**

```powershell
ssh-keygen -y -f "$env:USERPROFILE\.ssh\gha_deploy"        # sinh pub từ private
Get-Content "$env:USERPROFILE\.ssh\gha_deploy.pub"          # đọc pub có sẵn
```

PASS: 2 output khớp nhau (trừ comment cuối dòng).

> Nếu `ssh-keygen -y` báo "Too many arguments": bạn đang ở shell nuốt `""` — dùng đúng lệnh trên, **không** thêm `-P ""`.

**1.4 KIỂM THỬ 1c — ghi lại fingerprint cho GĐ 2:**

```powershell
ssh-keygen -lf "$env:USERPROFILE\.ssh\gha_deploy.pub"
# Output: 256 SHA256:<fp> gha-deploy (ED25519)
```

Lưu giá trị `<fp>` (kiểu `SHA256:abc...`).

---

## GĐ 2 — Cài public key lên server

**Mục tiêu:** server chấp nhận đăng nhập bằng key vừa tạo.

**2.1 Cách chuẩn** (từ máy có sẵn ssh-copy-id, ví dụ Git Bash):

```bash
ssh-copy-id -i ~/.ssh/gha_deploy.pub <user>@<host>
```

**Fallback copy tay** (Windows → server):

```powershell
# Lấy nội dung pub
Get-Content "$env:USERPROFILE\.ssh\gha_deploy.pub" | Set-Clipboard
```

```bash
# Trên server — paste nội dung vừa copy vào chỗ <PUBLIC_KEY>
mkdir -p ~/.ssh && chmod 700 ~/.ssh
echo '<PUBLIC_KEY>' >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys
```

**2.2 KIỂM THỬ 2a — kết nối được, không hỏi passphrase, không hỏi mật khẩu:**

```powershell
ssh -i "$env:USERPROFILE\.ssh\gha_deploy" -o IdentitiesOnly=yes <user>@<host> 'echo ok && hostname'
```

- PASS: in `ok` + tên host. Lần đầu hỏi `Are you sure you want to continue connecting (yes/no)` → gõ `yes`.
- FAIL lần đầu `Permission denied (publickey)` → xem 2.4.

**2.3 KIỂM THỬ 2b — fingerprint server khớp máy cá nhân:**

```bash
# Trên server
ssh-keygen -lf ~/.ssh/authorized_keys | grep <fp>
```

PASS: có dòng chứa đúng `<fp>` từ GĐ 1.4. Nếu không khớp → pub key trên server là của key khác (trùng tên file, paste nhầm), paste lại.

**2.4 Sự cố `Permission denied (publickey)` — checklist:**

1. Đúng user chưa? (`DEPLOY_USER` trên GitHub = user đang login server, ví dụ `root`).
2. Đúng file key chưa? (`-i` trỏ đúng private key tương ứng pub đã cài).
3. Perm đúng chưa? `~/.ssh` = 700, `authorized_keys` = 600, home dir không group-writable.
4. Dòng key trong `authorized_keys` còn nguyên vẹn (1 dòng, đủ `ssh-ed25519 AAAA... comment`)?
5. Server có `authorized_keys` bị override bởi cloud-init/PAM? Xem `/var/log/auth.log` hoặc `journalctl -u ssh`.

---

## GĐ 3 — GitHub Secrets & Variables

**Mục tiêu:** cấu hình đầy đủ cho job deploy. Vào repo → **Settings → Secrets and variables → Actions**.

**3.1 Bảng cấu hình bắt buộc:**

| Kind | Tên | Giá trị | Nguồn |
|---|---|---|---|
| Secret | `DEPLOY_HOST` | IP hoặc domain server | server |
| Secret | `DEPLOY_USER` | `<user>` (vd: `root`) | server |
| Secret | `DEPLOY_SSH_KEY` | **toàn bộ nội dung private key** | GĐ 1 |
| Secret | `DEPLOY_PORT` | `22` (hoặc port SSH thật) | server |
| Variable | `DEPLOY_ENABLED` | `true` (bật auto deploy) | tay |
| Variable | `DEPLOY_PATH` | `/abs/path/đến/repo` trên server | GĐ 4 |
| Variable | `NGINX_DOMAIN` | domain trỏ về VPS (qua Cloudflare) hoặc rỗng để bỏ qua nginx | tay |

**3.2 Paste key vào secret không lỗi:**

```powershell
Get-Content "$env:USERPROFILE\.ssh\gha_deploy" -Raw | Set-Clipboard
```

Paste nguyên trạng vào ô giá trị secret. Checklist trước khi Save:
- Dòng đầu: `-----BEGIN OPENSSH PRIVATE KEY-----`
- Dòng cuối: `-----END OPENSSH PRIVATE KEY-----`
- **Không** escape `\n` thủ công, không thêm/xoá khoảng trắng đầu dòng.

**3.3 KIỂM THỬ 3 — pipeline SSH tối giản trước khi ghép script thật:**

Tạo workflow tạm (hoặc `workflow_dispatch` với script 1 dòng) chỉ có bước SSH:

```yaml
- uses: appleboy/ssh-action@v1
  with:
    host: ${{ secrets.DEPLOY_HOST }}
    username: ${{ secrets.DEPLOY_USER }}
    key: ${{ secrets.DEPLOY_SSH_KEY }}
    port: ${{ secrets.DEPLOY_PORT || 22 }}
    script: |
      echo ok && hostname && whoami
```

PASS: job chạy xong, log có `out: ok`. Fail ở bước này = lỗi key/host/user, xử lý ngay ở GĐ 1-3, **chưa** đụng tới script deploy phức tạp.

**3.4 Mask `***`:** GitHub che mọi chuỗi trong log trùng giá trị secret (kể cả một phần đường dẫn). Thấy `/***/...` đừng hoảng — đó là mask.

**3.5 Package GHCR phải Public:** image container phải public để server `docker compose pull` không cần login. Vào Packages → package `-web` → Package settings → Change visibility → **Public**.

---

## GĐ 4 — Chuẩn bị server cho project

**Mục tiêu:** repo + `.env` + cấu hình sẵn trên server, boot được tay trước khi qua pipeline.

**4.1 Clone repo:**

```bash
# Repo public
git clone https://github.com/<org>/<repo>.git /abs/path/<repo-dir>

# Repo private — embed token (token chỉ có quyền read:contents)
git clone https://<TOKEN>@github.com/<org>/<repo>.git /abs/path/<repo-dir>
```

**4.2 Tạo `.env`:**

```bash
cd /abs/path/<repo-dir>
cp .env.example .env
```

Sinh mật khẩu mạnh (PowerShell, đủ 4 nhóm ký tự — SQL Server bắt buộc):

```powershell
-join ((48..57)+(65..90)+(97..122)+(35,64,37,38,42,33) | Get-Random -Count 20 | ForEach-Object {[char]$_})
```

```bash
sed -i 's/MSSQL_SA_PASSWORD=Your_password123/MSSQL_SA_PASSWORD=<mat-khau-sinh-duoc>/' .env
# Điều chỉnh port cho khớp .env.example (kiểm tra lại ss -tlnp ở GĐ 0.2)
cat .env
```

Lưu ý: `.env` là untracked → `git reset --hard` trong CD **không** xoá nó.

**4.3 KIỂM THỬ 4a — git remote/branch đúng:**

```bash
cd /abs/path/<repo-dir>
git remote -v          # origin trỏ đúng repo
git branch --show-current   # main (hoặc ref bạn sẽ deploy)
```

**4.4 KIỂM TH_TRY 4b — compose parse được .env:**

```bash
docker compose config >/dev/null && echo "config OK"
```

PASS: `config OK`, không lỗi "variable not set" / "invalid interpolation".

**4.5 KIỂM THỬ 4c — boot tay lần đầu (quan trọng nhất):**

```bash
docker compose up -d
watch docker compose ps        # chờ db healthy rồi web healthy (Ctrl+C thoát)
curl -fsS http://localhost:<WEB_PORT>/health && echo OK
```

- PASS: db `healthy`, web `healthy`, curl ra `OK`.
- db unhealthy → xem GĐ 6 (OOM / exit 18 / state cũ).

> Boot tay trước để pipeline không bị chi phối bởi thời gian init lâu (first-boot SQL Server + seed có thể > 90s healthcheck timeout của workflow).

---

## GĐ 5 — Workflow CD

**Mục tiêu:** pipeline tự động: push `main` → build image → SSH deploy.

**5.1 Cấu trúc 3 job:**

```
ci (CI gate) ──► publish-image (build & push GHCR) ──► deploy (SSH)
```

**5.2 Input SSH giải thích:**

```yaml
uses: appleboy/ssh-action@v1
with:
  host:            ${{ secrets.DEPLOY_HOST }}
  username:        ${{ secrets.DEPLOY_USER }}
  key:             ${{ secrets.DEPLOY_SSH_KEY }}
  port:            ${{ secrets.DEPLOY_PORT || 22 }}
  envs:            WORKDIR,REF,WEB_IMAGE,NGINX_DOMAIN   # biến truyền vào script
  command_timeout: 10m
  script: |          # chạy trên server, qua bash
```

**5.3 Các bước script remote (giải thích):**

1. `WORKDIR="${WORKDIR/#\~/$HOME}"` — mở rộng `~` vì `cd "..."` có ngoặc kép không tự mở rộng.
2. `PREV_IMAGE=$(docker inspect ...)` — lưu image cũ để rollback.
3. `git fetch --all && git reset --hard origin/$REF` — đồng bộ code (`.env` untracked không bị xoá).
4. `docker compose pull web && docker compose up -d --remove-orphans`.
5. Loop 90s chờ container `web` healthy.
6. Không healthy → `WEB_IMAGE=$PREV_IMAGE docker compose up -d` (rollback) + `exit 1`.
7. Healthy → `bash deploy/setup-nginx.sh` (idempotent; lỗi nginx chỉ warning vì app đã chạy OK).
8. `docker image prune -f` + `docker compose ps`.

**5.4 KIỂM THỬ 5 — 5 mốc phải thấy trong log job thành công:**

| # | Mốc trong log |
|---|---|
| 1 | `Fetching origin` + `HEAD is now at ...` |
| 2 | `web Pulled` / `web Built` |
| 3 | `Container ...-web-1 Started` (không có `dependency failed`) |
| 4 | `[nginx] hoan tat: http://<domain>` (nếu set `NGINX_DOMAIN`) |
| 5 | `Deploy thanh cong!` + bảng `docker compose ps` |

**5.5 KIỂM THỬ 5b — end-to-end từ bên ngoài:**

```bash
curl -fsS http://<domain>/health    # hoặc http://<host>:<WEB_PORT>/health nếu chưa có nginx
```

---

## GĐ 6 — Kịch bản sự cố (bảng tra nhanh)

| Symptom (log) | Nguyên nhân | Lệnh chẩn đoán | Fix |
|---|---|---|---|
| `this private key is passphrase protected` | Secret chứa key có passphrase | Decode base64 header key (GĐ 1.2) | Tạo key không passphrase, update secret |
| `attempted methods [none publickey]` | Sai authorized_keys / sai user | `ssh -i key -o IdentitiesOnly=yes ...` từ máy cá nhân | Xem checklist GĐ 2.4 |
| `cd: ... No such file or directory` | `DEPLOY_PATH` sai | So `DEPLOY_PATH` với `pwd` trên server | Sửa variable |
| `db is unhealthy` (fail > 2 phút) | RAM đầy / port trùng / password sai | `docker stats --no-stream`, `free -m`, `docker logs <db> \| tail -30` | Dừng container runaway, limit RAM |
| `exit=18 oom=false` + lỗi 945 model | Volume SQL Server hỏng (bị kill giữa init) | `docker inspect --format 'exit={{.State.ExitCode}} oom={{.State.OOMKilled}}' <db>` | `docker compose down -v` + `up -d` |
| `oom=true` | Container bị OOM kill | `docker inspect --format 'oom={{.State.OOMKilled}}'` | `docker update --memory=2g ...` |
| `db is unhealthy` fail sau ~7s | Health state cũ, compose không recreate | `docker compose ps -a` | `docker compose up -d --force-recreate db` |
| nginx 502 Bad Gateway | Web container chưa healthy / sai `WEB_PORT` | `docker compose ps`, `curl localhost:<WEB_PORT>/health` | Fix web trước, reload nginx |
| Healthcheck timeout 90s nhưng web chạy tay OK | First-boot init lâu hơn 90s | So thời gian `docker compose up` tay với 90s | Boot tay trước GĐ 4.5, hoặc tăng loop |

**Quy tắc đọc log:**
1. Luôn đọc **từ cuối lên** — nguyên nhân gốc nằm ở dòng cuối cùng có `Error`/`FAIL`.
2. Phân biệt noise: `F:\dbs\...` (image mssql) là noise. `attempted methods`, `dependency failed`, `exit=` là signal thật.
3. Không rerun khi chưa biết nguyên nhân — rerun vô ích nếu lỗi là config/hạ tầng.

---

## GĐ 7 — Vận hành & phòng ngừa

**7.1 Giới hạn RAM mọi container mới** (chống runaway kiểu ActiveMQ):

```bash
docker update --memory=2g --memory-swap=2g <container>
```

**7.2 Giám sát định kỳ:**

```bash
docker stats --no-stream | sort -k4 -hr | head     # top container theo RAM
free -m
```

**7.3 Checklist trước mỗi lần deploy project mới:**

- [ ] GĐ 1.2/1.3/1c: key cipher = `none`, private↔public khớp, fingerprint đã lưu
- [ ] GĐ 2.2: `ssh -i ... 'echo ok'` pass
- [ ] GĐ 3.1: đủ 5 secret/variable + `DEPLOY_ENABLED=true`
- [ ] GĐ 3.2: secret `DEPLOY_SSH_KEY` cột *Updated* = hôm nay
- [ ] GĐ 3.5: package GHCR = Public
- [ ] GĐ 4.2: `.env` tồn tại trên server, password mạnh, port không trùng (`ss -tlnp`)
- [ ] GĐ 4.5: boot tay OK, `curl /health` ra `OK`
- [ ] `free -m` available >= 4GB
- [ ] GĐ 5.4: log job có đủ 5 mốc
- [ ] GĐ 5.5: `curl` từ ngoài được

**7.4 Quy tắc vàng:**

1. Key CI không bao giờ có passphrase — CI không gõ passphrase được. Muốn passphrase thì phải thêm input `passphrase` + secret riêng (phương án thay thế).
2. `.env` không bao giờ commit — luôn tạo tay từ `.env.example` trên từng server.
3. Verify biến (secret/variable) trước, đọc log pipeline sau — ⅔ lỗi trong báo cáo này là do config sai, không phải do code.
4. Không deploy khi tài nguyên server đã đầy — pipeline fail symptom cuối cùng luôn là hạ tầng, nhưng root cause thường là project khác trên cùng máy.
