FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine

# Listen ports
EXPOSE 80

# Install cultures (same approach as Alpine SDK image)
RUN apk add --no-cache icu-libs

# Disable the invariant mode (set in base image)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY project project
ENTRYPOINT ["dotnet", "project/Nager.CertificateManagement.WebApi.dll"]