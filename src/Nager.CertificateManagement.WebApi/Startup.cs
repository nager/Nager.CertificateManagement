using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Nager.CertificateManagement.Library.CertificateJobRepository;
using Nager.CertificateManagement.Library.DnsManagementProvider;
using Nager.CertificateManagement.Library.ObjectStorage;
using Nager.CertificateManagement.WebApi.Services;
using Nager.PublicSuffix;
using System.Text.Json.Serialization;

namespace Nager.CertificateManagement.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var s3Configuration = new S3Configuration
            {
                Endpoint = Configuration["ObjectStorage:Endpoint"],
                AccessKey = Configuration["ObjectStorage:AccessKey"],
                SecretKey = Configuration["ObjectStorage:SecretKey"]
            };

            services.AddSingleton(s3Configuration);
            services.AddSingleton<IObjectStorage, S3ObjectStorage>();

            services.AddSingleton<IDomainParser>(new DomainParser(new WebTldRuleProvider()));
            //services.AddSingleton<ICertificateJobRepository, MockCertificateJobRepository>();
            services.AddSingleton<ICertificateJobRepository, S3CertificateJobRepository>();

            this.AddDnsManagementProvider(services);
            services.AddTransient<ICertificateService, CertificateService>();

            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Nager.CertificateManagement.WebApi", Version = "v1" });
            });
        }

        private void AddDnsManagementProvider(IServiceCollection services)
        {
            var hetznerApiKey = Configuration["DnsProvider:Hetzner:ApiKey"];
            if (!string.IsNullOrEmpty(hetznerApiKey))
            {
                services.AddTransient<IDnsManagementProvider>(provider => new HetznerDnsManagementProvider(hetznerApiKey));
            }

            var cloudFlareApiKey = Configuration["DnsProvider:CloudFlare:ApiKey"];
            if (!string.IsNullOrEmpty(cloudFlareApiKey))
            {
                services.AddTransient<IDnsManagementProvider>(provider => new CloudFlareDnsManagementProvider(cloudFlareApiKey));
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nager.CertificateManagement.WebApi v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
