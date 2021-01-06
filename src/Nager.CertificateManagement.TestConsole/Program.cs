using Microsoft.Extensions.Logging;
using Nager.CertificateManagement.Library;
using Nager.CertificateManagement.Library.DnsManagementProvider;
using Nager.CertificateManagement.Library.ObjectStorage;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Nager.CertificateManagement
{
    class Program
    {
        static void Main(string[] args)
        {
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            ILogger<S3ObjectStorage> loggerObjectStorage = loggerFactory.CreateLogger<S3ObjectStorage>();
            ILogger<CertificateProcessor> loggerCertificateProcessor = loggerFactory.CreateLogger<CertificateProcessor>();

            var myAcmeEmailAddress = "employee@company.com";

            var certificateSigningInfo = new CertificateSigningInfo
            {
                CountryName = "AT",
                State = "Vorarlberg",
                Locality = "Dornbirn",
                Organization = "Company",
                OrganizationUnit = "Dev"
            };

            var domains = new[]
            {
                "*.devops.company.com"
            };

            var s3Configuration = new S3Configuration
            {
                Endpoint = "http://localhost:9000",
                AccessKey = "",
                SecretKey = ""
            };

            IDnsManagementProvider dnsManagementProvider = new HetznerDnsManagementProvider("HetznerDnsApiKey");
            IObjectStorage objectStorage = new S3ObjectStorage(loggerObjectStorage, s3Configuration);

            var certificateManagement = new CertificateProcessor(
                loggerCertificateProcessor,
                dnsManagementProvider,
                objectStorage,
                myAcmeEmailAddress,
                certificateSigningInfo,
                CertificateRequestMode.Test);

            certificateManagement.ProcessAsync(domains).GetAwaiter().GetResult();
        }

        public static void Verify()
        {
            var certificate = LoadPemCertificate("certificate.pem", "key.pem").GetAwaiter().GetResult();
            var status = certificate.Verify();
            Console.WriteLine(status);
        }

        public static async Task<X509Certificate2> LoadPemCertificate(string certificatePath, string privateKeyPath)
        {
            using var publicKey = new X509Certificate2(certificatePath);

            var privateKeyText = await File.ReadAllTextAsync(privateKeyPath);
            var privateKeyBlocks = privateKeyText.Split("-", StringSplitOptions.RemoveEmptyEntries);
            var privateKeyBytes = Convert.FromBase64String(privateKeyBlocks[1]);

            var ecd = ECDsa.Create();
            ecd.ImportECPrivateKey(privateKeyBytes, out _);

            //using var rsa = RSA.Create();

            //if (privateKeyBlocks[0] == "BEGIN PRIVATE KEY")
            //{
            //    rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
            //}
            //else if (privateKeyBlocks[0] == "BEGIN RSA PRIVATE KEY")
            //{
            //    rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
            //}

            //var keyPair = publicKey.CopyWithPrivateKey(rsa);
            var keyPair = publicKey.CopyWithPrivateKey(ecd);

            return new X509Certificate2(keyPair.Export(X509ContentType.Pfx));
        }
    }
}
