using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace AI_DependencyTracking
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .Build();

            host.Run();
        }
    }

    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry();

            // Call DisableDependencyTracking(services)  or DisableCorrelationIdHeaders(services)
        }

        /// <summary>
        /// Disables dependency tracking completely
        /// </summary>
        /// <param name="services"></param>
        private void DisableDependencyTracking(IServiceCollection services)
        {
            var module = services.FirstOrDefault(t => t.ImplementationFactory?.GetType() == typeof(Func<IServiceProvider, DependencyTrackingTelemetryModule>));
            if (module != null)
            {
                services.Remove(module);
            }
        }

        /// <summary>
        /// Dsiables correlation id headers, but leaves dependency tracking on
        /// </summary>
        /// <param name="services"></param>
        private void DisableCorrelationIdHeaders(IServiceCollection services)
        {
            var module = services.FirstOrDefault(t => t.ImplementationFactory?.GetType() == typeof(Func<IServiceProvider, DependencyTrackingTelemetryModule>));
            if (module != null)
            {
                services.Remove(module);
                services.AddSingleton<ITelemetryModule>(provider => new DependencyTrackingTelemetryModule() { SetComponentCorrelationHttpHeaders = false });
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Run(async (context) =>
            {
                var client = new HttpClient();
                var response = await client.GetAsync("http://blog.wille-zone.de");
            });

            // Call DisableCorrelationIdHeadersForDomain(app);
        }

        private void DisableCorrelationIdHeadersForDomain(IApplicationBuilder app)
        {
            var modules = app.ApplicationServices.GetServices<ITelemetryModule>();
            var dependencyModule = modules.OfType<DependencyTrackingTelemetryModule>().FirstOrDefault();
            if (dependencyModule != null)
            {
                var domains = dependencyModule.ExcludeComponentCorrelationHttpHeadersOnDomains;
                domains.Add("blog.wille-zone.de");
            }
        }
    }
}
