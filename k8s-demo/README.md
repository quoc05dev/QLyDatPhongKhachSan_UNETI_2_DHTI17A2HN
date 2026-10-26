# Demo Kubernetes cho QLyDatPhongKhachSan trên máy ảo

> Cấu trúc thư mục `k8s-demo/`:
> - `00-secret.yaml` (gitignored, tạo từ `00-secret.yaml.example`) — password SA + connection string, KHÔNG commit
> - `manifests/01-mssql.yaml` — Namespace + headless Service + StatefulSet SQL Server (PVC 10Gi)
> - `manifests/02-web.yaml` — Deployment + ClusterIP Service + Ingress (domain `uhotel.click`)
> - `optional/03-cert-manager.yaml` — ClusterIssuer + Certificate Let's Encrypt (chỉ áp trên VPS đã cài cert-manager)
> - `argocd-app.yaml` — Application ArgoCD (apply thủ công 1 lần, sau đó GitOps tự chạy)

Thứ tự apply: `00` → `manifests/01` (SQL chạy trước) → `manifests/02` (web) → `optional/03` (cert).
Số tiền tố 00 < 01 < 02 để nếu dùng ArgoCD (sync theo bảng chữ cái) thì SQL luôn đi trước web.

---

## PHẦN A - LOCAL TRÊN MÁY ẢO (minikube, không cần domain thật)

Phù hợp máy ảo có **tối thiểu 8GB RAM** (SQL Server cần 2Gi request).

### A1. Cài minikube trên máy ảo Linux

```bash
curl -LO https://storage.googleapis.com/minikube/releases/latest/minikube-linux-amd64
sudo install minikube-linux-amd64 /usr/local/bin/minikube
```

Start cluster dùng Docker driver:

```bash
minikube start --driver=docker --cpus=4 --memory=8g --kubernetes-version=v1.31.0
```

Kích hoạt addon Ingress (thay cho ingress-nginx tự cài):

```bash
minikube addons enable ingress
```

### A2. Cần image GHCR pull được

Nếu image `ghcr.io/quoc05dev/...-web` là **public**, bỏ qua bước này.
Nếu là **private**, tạo pull secret rồi bỏ comment khối `imagePullSecrets` trong `manifests/02-web.yaml`:

```bash
kubectl create secret docker-registry ghcr-cred \
  --docker-server=ghcr.io \
  --docker-username=<github-username> \
  --docker-password=<github-PAT-token>
```

```yaml
      imagePullSecrets:
        - name: ghcr-cred
```

### A3. Apply manifests theo thứ tự

```bash
kubectl apply -f k8s-demo/00-secret.yaml
kubectl apply -f k8s-demo/manifests/01-mssql.yaml

# Đợi SQL sẵn sàng (1-3 phút)
kubectl -n k8s-hoteluneti get pods -w
# cho tới khi mssql-0 chạy "Running / Ready 1/1"

kubectl apply -f k8s-demo/manifests/02-web.yaml
kubectl -n k8s-hoteluneti get pods -w
# cho tới khi qldpkh-web-* "Running / Ready 1/1"
```

### A4. Kiểm tra logs & thử truy cập

```bash
# Log web, chú ý dòng "Da ap dung Migration cho database"
kubectl -n k8s-hoteluneti logs -l app=qldpkh-web -f

# Test /health từ trong cluster
kubectl -n k8s-hoteluneti run curl --rm -it --image=curlimages/curl \
  --restart=Never -- curl http://qldpkh-web:8080/health

# Test SQL
kubectl -n k8s-hoteluneti exec -it mssql-0 -- \
  /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P '<password>' \
  -Q "SELECT name FROM sys.databases"
```

### A5. Truy cập từ máy PC

Cách đơn giản nhất - **port-forward**:

```bash
kubectl -n k8s-hoteluneti port-forward svc/qldpkh-web 8080:8080 --address 0.0.0.0
```

Mở browser trên PC: `http://<IP_MAY_AO>:8080`

Cách qua Ingress (nếu muốn test domain ảo):

```bash
minikube tunnel          # hoặc cấu hình Minikube IP
```

Thêm vào `/etc/hosts` trên PC:

```
<IP_MAY_AO>  uhotel.click
```

Browser truy cập `https://uhotel.click` (với HTTPS cert tự ký, chọn "Continue" để tiếp tục).

---

## PHẦN B - VPS THẬT (uhotel.click + TLS thật ruột)

Phù hợp VPS có domain `uhotel.click` đã trỏ A record về IP của VPS.

### B1. Cài Kubernetes nhẹ (k3s)

```bash
curl -sfL https://get.k3s.io | sh -
# Kiểm tra:
sudo kubectl get nodes
```

Cài ingress-nginx + cert-manager:

```bash
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update
helm install ingress-nginx ingress-nginx/ingress-nginx \
  --namespace ingress-nginx --create-namespace

kubectl apply -f https://github.com/cert-manager/cert-manager/releases/latest/download/cert-manager.yaml
kubectl -n cert-manager get pods -w   # cho tới khi Running
```

Nếu image GHCR là private, tạo pull secret (thay bằng PAT của bạn):

```bash
kubectl create ns k8s-hoteluneti
kubectl -n k8s-hoteluneti create secret docker-registry ghcr-cred \
  --docker-server=ghcr.io \
  --docker-username=<github-username> \
  --docker-password=<github-PAT-token>
# rồi bỏ comment imagePullSecrets trong k8s-demo/manifests/02-web.yaml
```

### B2. Cài app QLyDatPhong

```bash
kubectl apply -f k8s-demo/00-secret.yaml
kubectl apply -f k8s-demo/manifests/01-mssql.yaml
kubectl -n k8s-hoteluneti get pods -w   # đợi mssql-0 sẵn sàng

kubectl apply -f k8s-demo/manifests/02-web.yaml
kubectl apply -f k8s-demo/optional/03-cert-manager.yaml
```

### B3. Đợi TLS được cấp

```bash
kubectl -n k8s-hoteluneti get certificate -w
# READY=True sau 1-3 phút, nếu False:
kubectl -n k8s-hoteluneti describe certificate uhotel-tls
kubectl -n cert-manager logs deploy/cert-manager
```

Mở browser:

```
https://uhotel.click
```

### B4. Lệnh kiểm tra thường dùng

| Lệnh | Mục đích |
|---|---|
| `kubectl -n k8s-hoteluneti get all` | Xem toàn bộ resource của namespace |
| `kubectl -n k8s-hoteluneti logs -l app=qldpkh-web -f` | Log web, kiểm tra "Da ap dung Migration" |
| `kubectl -n k8s-hoteluneti get pv,pvc` | Volume SQL đã bind 10Gi |
| `kubectl -n k8s-hoteluneti get ingress` | Ingress + host + TLS secret |
| `kubectl -n k8s-hoteluneti get certificate` | TLS đã cấp READY=True? |
| `kubectl -n k8s-hoteluneti get events --sort-by=.lastTimestamp` | Sự kiện gần nhất, debug Pending/CrashLoop |

### B5. Thao tác thay đổi

```bash
# Sau khi sửa yaml:
kubectl apply -f k8s-demo/manifests/02-web.yaml
kubectl -n k8s-hoteluneti rollout restart deploy/qldpkh-web

# Cập nhật image mới:
kubectl -n k8s-hoteluneti set image deploy/qldpkh-web \
  web=ghcr.io/quoc05dev/qlydatphongkhachsan_uneti_2_dhti17a2hn-web:<tag-moi>

# Rollback nếu deploy mới lỗi:
kubectl -n k8s-hoteluneti rollout undo deploy/qldpkh-web
```

### B6. Dọn dẹp khi muốn xóa sạch

```bash
kubectl delete namespace k8s-hoteluneti
kubectl delete clusterissuer letsencrypt-prod
```

---

## PHẦN C - GITOPS VỚI ARGocd

Đã setup ArgoCD trên VPS (namespace `argocd`, UI qua port-forward/NodePort). Mọi thay đổi manifest giờ đi qua Git.

### C1. Bước 1 lần duy nhất — tạo secret + apply Application

```bash
# 1. Secret không có trên Git (đúng thiết kế) — tạo tay:
kubectl apply -f k8s-demo/00-secret.yaml

# 2. Đăng ký Application (giờ Push lên main là ArgoCD tự sync các file trong k8s-demo/manifests)
kubectl -n argocd apply -f k8s-demo/argocd-app.yaml

# 3. Kiểm tra
argocd app list
# hoặc trong UI: App qldpkh-demo
```

Từ giờ: **sửa yaml → commit → push → ArgoCD tự sync** (retry 5 lần x30s nếu web đợi SQL).

### C2. Pipeline update image mới

1. Giờ GitHub Actions build image mới (tag mới hoặc tag `main` mới nhất).
2. Sửa `k8s-demo/manifests/02-web.yaml` đổi tag image.
3. Commit + push → ArgoCD tự rollout.

### C3. Lệnh ArgoCD hàng ngày

```bash
argocd app list                         # các Application
argocd app get qldpkh-demo              # trạng thái sync + health
argocd app sync qldpkh-demo             # sync ngay (không đợi tự động)
argocd app history qldpkh-demo          # lịch sử deploy
argocd app rollback qldpkh-demo <id>    # rollback về revision cũ
argocd app diffs qldpkh-demo            # so sánh Git vs cluster (diffs)
```

Trong UI: click `qldpkh-demo` → tab `Sync Status` để xem tree của resource đã sync, học/log gì cần patched.

### C4. Lưu ý & thứ tự mount resources

- ArgoCD sync **theo bảng chữ cái tên file** trong path `manifests/`: `01-mssql.yaml` chạy trước `02-web.yaml` → SQL luôn đi trước.
- `prune=false` - Khách khác xóa file thì resource vẫn còn ở cluster (an toàn khi đổi tên file, tránh mất DB clusterState).
- `selfHeal=true` - nếu ai kubectl tay chỉnh cluster, ArgoCD tự kéo về đúng cấu hình trong Git.

---

## Các lỗi thường gặp

| Triệu chứng | Nguyên nhân | Cách xử lý |
|---|---|---|
| `ImagePullBackOff` | GHCR private → không pull được | Thêm `imagePullSecrets` hoặc dùng tag public |
| mssql `OOMKilled` (CrashLoopBackOff, exit 137) | Máy ảo / node hết RAM | Trả VM có ít nhất 6GB, hoặc giảm `limits.memory` xuống 2Gi |
| mssql log `Failed to initialize, exiting` (exit 18) | Password SA không đạt yêu cầu cường độ | Đổi trong `00-secret.yaml`, re-apply; nếu PVC đã init password cũ → delete PVC để init lại |
| Web restart loop + log `Failed to connect` | SQL chưa sẵn sàng khi web khởi động | `kubectl rollout restart deploy/qldpkh-web` sau khi SQL Ready |
| Deployment `Pending` | Node không đủ CPU/memory | `kubectl describe pod <name>` để xem lý do cụ thể |
| Certificate `ready=False` lâu | DNS chưa trỏ về Node/LB, hoặc firewall chặn port 80 | `dig uhotel.click`, mở 80/443 |
| Browser mở https nhưng "certificate invalid" | Cert chưa cấp hoặc cert-manager chưa cài | `kubectl get certificate -A`, cài lại cert-manager |
| ArgoCD app `OutOfSync` sau khi kubectl tay | Có người sửa cluster ngoài Git | Đúng logic với `selfHeal=true`, sẽ tự đồng bộ về Git trong vài giây |
| ArgoCD `ComparisonError Unknown apiVersion cert-manager.io/v1` | Application trỏ nhầm path (đã gồm `optional/03-cert-manager.yaml`) | Path phải là `k8s-demo/manifests`, cert-manager apply tay riêng |

---

## Lưu ý quan trọng

1. **`00-secret.yaml` KHÔNG commit** - đã được đưa vào `.gitignore`. Password chỉ tồn tại trên máy. Khi dùng ArgoCD phải apply tay secret 1 lần (xem C1).
2. **App chạy HTTP trong cluster, HTTPS do Ingress lo** — tương tự docker-compose + nginx trước đó.
3. **SQL của StatefulSet** volume PVC giữ nguyên khi pod restart, chỉ bị mất khi `kubectl delete pvc`.
4. **Chỉ deploy với 1 replica SQL** (Developer free edition giới hạn 10GB/data instance).
5. **Sau khi deploy, app tự chạy Migration + Seed** nhờ `SeedSampleData=true` — chỉ có dữ liệu khi DB còn trống.
6. **Pipeline CD cũ vẫn deploy compose** — sau khi chuyển hẳn sang ArgoCD, tắt workflow cũ đi để tránh 2 nguồn deploy xung đột nhau.
