# Demo Kubernetes cho QLyDatPhongKhachSan trên máy ảo

> Vai trò từng file trong folder `k8s-demo/`:
> - `00-secret.yaml` (gitignored, tạo từ `00-secret.yaml.example`) — password SA + connection string
> - `02-sqlserver.yaml` — Namespace + headless Service + StatefulSet SQL Server (PVC 10Gi)
> - `01-web-deployment.yaml` — Deployment + ClusterIP Service + Ingress (domain `uhotel.click`)
> - `03-cert-manager.yaml` — ClusterIssuer + Certificate Let's Encrypt

Thứ tự apply: `00` → `02` (SQL chạy trước) → `01` (web) → `03` (cert TLS).

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
Nếu là **private**, tạo pull secret rồi sửa `01-web-deployment.yaml` thêm:

```yaml
      imagePullSecrets:
        - name: ghcr-cred
```

```bash
kubectl create secret docker-registry ghcr-cred \
  --docker-server=ghcr.io \
  --docker-username=<github-username> \
  --docker-password=<github-PAT-token>
```

### A3. Apply manifests theo thứ tự

```bash
kubectl apply -f k8s-demo/00-secret.yaml
kubectl apply -f k8s-demo/02-sqlserver.yaml

# Đợi SQL sẵn sàng (1-3 phút)
kubectl -n k8s-hoteluneti get pods -w
# cho tới khi mssql-0 chạy "Running / Ready 1/1"

kubectl apply -f k8s-demo/01-web-deployment.yaml
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

Browser truy cập `https://uhotel.click` (với HTTPS but cert tự ký, chọn "Continue" để tiếp tục).

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

### B2. Cài app QLyDatPhong

```bash
kubectl apply -f k8s-demo/00-secret.yaml
kubectl apply -f k8s-demo/02-sqlserver.yaml
kubectl -n k8s-hoteluneti get pods -w   # đợi mssql-0 sẵn sàng

kubectl apply -f k8s-demo/01-web-deployment.yaml
kubectl apply -f k8s-demo/03-cert-manager.yaml
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

### B5. Thao tác thao tác thay đổi

```bash
# Sau khi sửa yaml:
kubectl apply -f k8s-demo/01-web-deployment.yaml
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

## Các lỗi thường gặp

| Triệu chứng | Nguyên nhân | Cách xử lý |
|---|---|---|
| `ImagePullBackOff` | GHCR private → không pull được | Thêm `imagePullSecrets` hoặc dùng tag public |
| mssql `OOMKilled` (CrashLoopBackOff, exit 137) | Máy ảo / node hết RAM | Trả VM có ít nhất 6GB, hoặc giảm `limits.memory` xuống 2Gi |
| mssql log `Failed to initialize, exiting` (exit 18) | Password SA không đạt yêu cầu cường độ | Đổi trong `00-secret.yaml`, re-apply; nếu PVC đã init password cũ → delete PVC để init lại |
| Web restart loop + log `Failed to connect` | SQL chưa sẵn sàng khi web khởi động | `kubectl rollout restart deploy/qldpkh-web` sau khi SQL Ready |
| Deployment `Pending` | Node không đủ CPU/memory | `kubectl describe pod <name>` để xem lý do cụ thể |
| Certificate `ready=False` lâu | DNS chưa trỏ về Node/LB, hoặc firewall chặn port 80 | `dig uhotel.click`, mở 80/443 |
| Browser mở https nhưng "certificate invalid" | Cert chưa (chưa apply 03, hoặc cert-manager chưa cài) | `kubectl get certificate -A`, cài lại cert-manager |

---

## Lưu ý quan trọng

1. **`00-secret.yaml` KHÔNG commit** - đã được đưa vào `.gitignore`. Password chỉ tồn tại trên máy.
2. **Lua chon VPS:** App chạy HTTP trong cluster, HTTPS do Ingress lo — tương tự docker-compose + nginx trước đó.
3. **SQL của StatefulSet** volume PVC giữ nguyên khi pod restart, chỉ bị mất khi `kubectl delete pvc`.
4. **Chỉ deploy với 1 replica SQL** (CDeveloper free edition giới hạn 10GB/data instance).
5. **Sau khi deploy, app tự chạy Migration + Seed** nhờ `SeedSampleData=true` — chỉ có dữ liệu khi DB còn trống.
