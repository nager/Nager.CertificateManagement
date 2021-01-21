# Nager.CertificateManagement

Nager.CertificateManagement provides Lets Encrypt certificates for internal servers, switches etc.. The certificates are requested via the DNS challenge, so the internal servers do not need an external connection.

![Nager.CertificateManagement](doc/screenshot.png)

![Nager.CertificateManagement](doc/architecture%20overview.png)
The certificates can then be obtained by the individual servers from the S3 storage. In contrast to other solutions, the access data does not have to be stored at a central location.

## Supported DNS Provider

- Hetzner
- CloudFlare

## Start Application

Update the configuration in the `docker-compose.yml` file before start the application.
After the start the application is available under http://localhost

```bash
docker-compose up
```

Update application

```bash
docker-compose pull
```
