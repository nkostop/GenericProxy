using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Nbg.AspNetCore.Http.Extensions;
using Nbg.AspNetCore.Identity.iBank.OAuth2;

using Nbg.AspNetCore.ProxyMiddleware;
using Nbg.AspNetCore.ProxyMiddleware.Transform.Modules;
using Nbg.NetCore.Healthchecks.Checks;
using Nbg.NetCore.Healthchecks.Reports;
using Nbg.NetCore.Proxy.Utilities;

namespace Nbg.NetCore.DevPortal.Proxy
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            configuration.ConfigureServicePointManager();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            
            var logger = NLog.LogManager.GetCurrentClassLogger();   
            logger.Info("Configuring services...");
            services.AddDistributedMemoryCache();
            logger.Info("Added distributed memory cache...");
            // Add IIdentityService implementation to dependency injection
            // services.AddintranetIdentityOAuth2();
            //add ibank services OAuth2
            services.AddiBankIdentityOAuth2();
            logger.Info("Added iBank Identity OAuth2...");

            services.AddHttpClient("default", "HttpClient:default");
            logger.Info("Added default HttpClient...");


            // services.AddHealthChecks()
            //     .AddAllocatedMemoryCheck()
            //     .AddUriCheck();


            services.AddProxyMiddleware();

            services.AddJwtAuthentication();
            services.AddControllers();

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConsole();
                loggingBuilder.AddDebug();
            });

   

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            

            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Info($"Environment: {env.EnvironmentName}");
            logger.Info("Starting request pipeline configuration...");

            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseRouting();

            // IMPORTANT, enable JWT authentication
            app.UseAuthentication();
            app.UseAuthorization();

            app.Use(async (context, next) =>
                {
                    Console.WriteLine($"[Pre-Proxy] Full request URL: {context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}");
                    var destinationUrl = $"http://localhost:7032{context.Request.Path}{context.Request.QueryString}";
                    Console.WriteLine($"[Proxy] Actually forwarding to: {destinationUrl}");

                    await next();
                    Console.WriteLine($"[Post-Proxy] Response: {context.Response.StatusCode} {context.Response.ContentType} {context.Response.Body}");
                });

             app.UseProxyMiddleware();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                // register health check endpoint
                // endpoints.MapHealthChecks("/health", new HealthCheckOptions
                // {
                //     ResponseWriter = HealthCheckReports.WriteJson,
                //     AllowCachingResponses = false
                // });
            });

            
        }
    }
}


// using Microsoft.AspNetCore.Builder;
// using Microsoft.AspNetCore.Diagnostics.HealthChecks;
// using Microsoft.AspNetCore.Hosting;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;

// using Nbg.AspNetCore.Http.Extensions;
// using Nbg.AspNetCore.Identity.iBank.OAuth2;

// using Nbg.AspNetCore.ProxyMiddleware;
// using Nbg.AspNetCore.ProxyMiddleware.Transform.Modules;
// using Nbg.NetCore.Healthchecks.Checks;
// using Nbg.NetCore.Healthchecks.Reports;
// using Nbg.NetCore.Proxy.Utilities;

// namespace Nbg.NetCore.DevPortal.Proxy
// {
//     public class Startup
//     {
//         public IConfiguration Configuration { get; }

//         public Startup(IConfiguration configuration)
//         {
//             Configuration = configuration;
//             configuration.ConfigureServicePointManager();
//         }

//         // This method gets called by the runtime. Use this method to add services to the container.
//         // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
//         public void ConfigureServices(IServiceCollection services)
//         {
            

//             services.AddDistributedMemoryCache();
//             // Add IIdentityService implementation to dependency injection
//             // services.AddintranetIdentityOAuth2();
//             //add ibank services OAuth2
//             services.AddiBankIdentityOAuth2();

//             services.AddHttpClient("DevPortalProxyHandler")
//                 .ConfigureHttpClient(client =>
//                 {
//                     client.DefaultRequestHeaders.Add("Accept", "application/json");
//                     client.DefaultRequestHeaders.Add("Content-Type", "application/json");
//                 });

//             // services.AddHealthChecks()
//             //     .AddAllocatedMemoryCheck()
//             //     .AddUriCheck();

//             // services.AddProxyMiddleware();

//             var authSection = Configuration.GetSection("Authentication");

//             services.AddAuthentication("Bearer")
//                 .AddIdentityServerAuthentication("Bearer", options =>
//                 {
//                     options.Authority = authSection["Authority"];
//                     options.ApiName = authSection["ApiName"];
//                     options.ApiSecret = authSection["ApiSecret"];
//                     options.SupportedTokens = IdentityServer4.AccessTokenValidation.SupportedTokens.Both;
//                     options.RequireHttpsMetadata = false;
//                 });

            
//             services.AddControllers();
//             services.AddLogging(loggingBuilder =>
//             {
//                 loggingBuilder.AddConsole();
//                 loggingBuilder.AddDebug();
//             });

   

//         }

//         // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
//         public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
//         {

           

            
//             if (env.IsDevelopment())
//             {
//                 app.UseDeveloperExceptionPage();
//             }

//             app.UseDefaultFiles();
//             app.UseStaticFiles();

//             app.UseRouting();

//             // IMPORTANT, enable JWT authentication
//             app.UseAuthentication();
//             app.UseAuthorization();

//             app.Use(async (context, next) =>
//                 {
//                     Console.WriteLine($"[Pre-Proxy] Full request URL: {context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}");
//                     var destinationUrl = $"http://localhost:7032{context.Request.Path}{context.Request.QueryString}";
//                     Console.WriteLine($"[Proxy] Actually forwarding to: {destinationUrl}");

//                     await next();
//                     Console.WriteLine($"[Post-Proxy] Response: {context.Response.StatusCode} {context.Response.ContentType}");
//                 });

//             // app.UseProxyMiddleware();
//              app.UseMiddleware<ProxyMiddleware>(
//                     new HttpClient(), "http://localhost:7032"
//                 );

//             app.UseEndpoints(endpoints =>
//             {
//                 endpoints.MapControllers();

//                 // register health check endpoint
//                 // endpoints.MapHealthChecks("/health", new HealthCheckOptions
//                 // {
//                 //     ResponseWriter = HealthCheckReports.WriteJson,
//                 //     AllowCachingResponses = false
//                 // });
//             });

            
//         }
//     }
// }