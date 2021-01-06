# Nager.CertificateManagement

## Supported DNS Provider

- Hetzner
- CloudFlare

## Start Application

Update configuration in docker-compose.yml

```bash
docker-compose up
```

### Run a local minio instance
```bash
docker run -p 9000:9000 -e "MINIO_ACCESS_KEY=AKIAIOSFODNN7EXAMPLE" -e "MINIO_SECRET_KEY=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY" minio/minio server /data
```
