# syntax=docker/dockerfile:1

# =============================================================================
# Giai doan 1: build & publish (.NET 10 SDK)
# Ghim phien ban cu the (khong dung tag truot "10.0") de build on dinh, tai lap duoc.
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src

# Restore rieng de tan dung cache layer: chi doi source se khong restore lai.
COPY QLyDatPhongKhachSan/QLyDatPhongKhachSan.csproj QLyDatPhongKhachSan/
RUN dotnet restore QLyDatPhongKhachSan/QLyDatPhongKhachSan.csproj

# Copy toan bo source con lai roi publish ban Release.
COPY . .
RUN dotnet publish QLyDatPhongKhachSan/QLyDatPhongKhachSan.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# =============================================================================
# Giai doan 2: runtime (.NET 10 ASP.NET 10.0.12, chi chua ban da publish)
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0.12 AS final
WORKDIR /app

# curl          : phuc vu HEALTHCHECK
# libfontconfig1: QuestPDF (SkiaSharp) can de render PDF tren Linux
# fonts-liberation: font thay the metric-compatible voi Arial
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl libfontconfig1 fonts-liberation \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

COPY --from=build /app/publish .
# Script du lieu mau de DbSeeder nap khi bat SeedSampleData=true.
COPY --from=build /src/QLyDatPhongKhachSan/Database ./Database

# Chay bang user khong phai root (user "app", UID 1654 co san trong image .NET 8+).
USER $APP_UID

# Kiem tra song: /health tra 200 khi app + database san sang.
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "QLyDatPhongKhachSan.dll"]
