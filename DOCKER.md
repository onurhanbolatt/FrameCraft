# 🐳 FrameCraft Docker Rehberi

## 📁 Dosya Yapısı

```
FrameCraft/
├── docker-compose.yml          # Development: API + SQL + Seq
├── docker-compose.prod.yml     # Production: Tüm servisler + resource limits
├── docker-compose.infra.yml    # Sadece SQL + Seq (API local)
├── .dockerignore               # Docker build ignore
├── .env.example                # Environment variables template
└── src/FrameCraft.API/
    └── Dockerfile              # Multi-stage API Dockerfile
```

---

## 🚀 Hızlı Başlangıç

### Seçenek 1: Sadece Infrastructure (Önerilen - Development)
API'yi Visual Studio/Rider'da debug ederken SQL Server ve Seq'i Docker'da çalıştır.

```bash
# Infrastructure'ı başlat
docker-compose -f docker-compose.infra.yml up -d

# API'yi local çalıştır
cd src/FrameCraft.API
dotnet run
```

**Erişim:**
- API: https://localhost:7xxx (Visual Studio port)
- Seq: http://localhost:5341
- SQL Server: localhost,1433

---

### Seçenek 2: Tüm Stack (Development)
Her şeyi Docker'da çalıştır.

```bash
# Tüm servisleri başlat
docker-compose up -d

# Logları izle
docker-compose logs -f api
```

**Erişim:**
- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- Seq: http://localhost:5341
- SQL Server: localhost,1433

---

### Seçenek 3: Production
```bash
# .env dosyasını oluştur
cp .env.example .env
# .env dosyasını düzenle ve güvenli şifreler gir

# Production modda başlat
docker-compose -f docker-compose.prod.yml up -d --build
```

---

## 🔧 Sık Kullanılan Komutlar

### Servis Yönetimi
```bash
# Tüm servisleri başlat
docker-compose up -d

# Sadece belirli servisleri başlat
docker-compose up -d sqlserver seq

# Servisleri durdur
docker-compose down

# Servisleri ve volume'ları sil (DİKKAT: Data silinir!)
docker-compose down -v

# Servisleri yeniden başlat
docker-compose restart api
```

### Log Yönetimi
```bash
# Tüm logları izle
docker-compose logs -f

# Sadece API loglarını izle
docker-compose logs -f api

# Son 100 satır
docker-compose logs --tail=100 api
```

### Build & Rebuild
```bash
# Image'ı yeniden build et
docker-compose build api

# Cache olmadan build
docker-compose build --no-cache api

# Build edip başlat
docker-compose up -d --build api
```

### Container İçine Erişim
```bash
# API container'ına bash ile gir
docker exec -it framecraft-api /bin/bash

# SQL Server'a bağlan
docker exec -it framecraft-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "FrameCraft123!" -C
```

---

## 🔌 Bağlantı Bilgileri

### Development
| Servis | Host | Port | Kullanıcı | Şifre |
|--------|------|------|-----------|-------|
| SQL Server | localhost | 1433 | sa | FrameCraft123! |
| Seq | localhost | 5341 | - | - |
| API | localhost | 5000 | - | - |

### Container İçi (Docker Network)
| Servis | Host | Port |
|--------|------|------|
| SQL Server | sqlserver | 1433 |
| Seq | seq | 5341 |
| API | api | 8080 |

---

## 🗄️ Database Migration

### Local Development
```bash
cd src/FrameCraft.API
dotnet ef database update --project ../FrameCraft.Infrastructure
```

### Docker Container İçinde
```bash
# Container'a gir
docker exec -it framecraft-api /bin/bash

# Migration çalıştır
dotnet ef database update
```

### İlk Kurulumda
API başlatıldığında otomatik migration çalışmıyor. Manual çalıştırman gerekiyor:

```bash
# Connection string ile
dotnet ef database update --connection "Server=localhost,1433;Database=FrameCraftDb;User Id=sa;Password=FrameCraft123!;TrustServerCertificate=True"
```

---

## 🔍 Troubleshooting

### SQL Server başlamıyor
```bash
# Logları kontrol et
docker-compose logs sqlserver

# Container durumunu kontrol et
docker ps -a | grep sqlserver

# Yeniden başlat
docker-compose restart sqlserver
```

### API SQL Server'a bağlanamıyor
1. SQL Server health check'i geçiyor mu kontrol et:
   ```bash
   docker-compose ps
   ```
2. Network bağlantısını test et:
   ```bash
   docker exec framecraft-api ping sqlserver
   ```

### Port çakışması
```bash
# 1433 portunu kim kullanıyor?
netstat -ano | findstr :1433  # Windows
lsof -i :1433                  # Linux/Mac

# Farklı port kullan
# docker-compose.yml'de: "1434:1433"
```

### Volume temizleme
```bash
# Sadece FrameCraft volume'larını sil
docker volume rm framecraft-sqlserver-data framecraft-seq-data

# Tüm kullanılmayan volume'ları sil
docker volume prune
```

---

## 📊 Resource Usage

### Development Tahmini
| Servis | RAM | CPU |
|--------|-----|-----|
| SQL Server | 1-2 GB | Low |
| Seq | 200-500 MB | Low |
| API | 100-300 MB | Low |
| **Toplam** | **~2-3 GB** | Low |

### Production Limitleri (docker-compose.prod.yml)
| Servis | RAM Limit | RAM Reserved |
|--------|-----------|--------------|
| SQL Server | 2 GB | 1 GB |
| Seq | 1 GB | 512 MB |
| API | 1 GB | 512 MB |

---

## ✅ Checklist

### İlk Kurulum
- [ ] Docker Desktop kurulu
- [ ] `docker-compose -f docker-compose.infra.yml up -d` çalıştırıldı
- [ ] SQL Server healthy durumda (`docker ps`)
- [ ] Seq erişilebilir (http://localhost:5341)
- [ ] Database migration yapıldı
- [ ] API başlatıldı ve Swagger açılıyor

### Production Deployment
- [ ] `.env` dosyası oluşturuldu ve güvenli şifreler girildi
- [ ] `.env` dosyası `.gitignore`'da
- [ ] SSL sertifikaları hazır (Nginx kullanılacaksa)
- [ ] Backup stratejisi belirlendi
- [ ] Monitoring/Alerting kurulu (Seq alerts)
