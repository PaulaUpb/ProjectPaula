using System.Collections.Generic;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ProjectPaula.DAL;
using Microsoft.Data.Entity;
using ProjectPaula.Model.CalendarExport;
using Microsoft.AspNet.Http;

namespace ProjectPaula
{
    public class Startup
    {
        private static readonly StringValues AllowedOrigins = new StringValues(new[] { "https://ajax.aspnetcdn.com" });

        public Startup(IHostingEnvironment env)
        {
            // Setup configuration sources.
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services to the services container.
            services.AddMvc();

            // Add customized JSON serializer that serializes all enums as strings
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new StringEnumConverter());
            services.AddSingleton(s => JsonSerializer.Create(settings));

            // Add SignalR services
            services.AddSignalR();

            // Uncomment the following line to add Web API services which makes it easier to port Web API 2 controllers.
            // You will also need to add the Microsoft.AspNet.Mvc.WebApiCompatShim package to the 'dependencies' section of project.json.
            // services.AddWebApiConventions();
        }

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
#if !DEBUG
            loggerFactory.MinimumLevel = LogLevel.Error;
#endif
            loggerFactory.AddConsole();
            loggerFactory.AddDebug(LogLevel.Verbose);

            // Configure the HTTP request pipeline.
            app.UseDeveloperExceptionPage();

            // Add the following to the request pipeline only in development environment.
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();

            }
            else
            {
                // Add Error handling middleware which catches all application specific errors and
                // send the request to the following path or controller action.
                //app.UseExceptionHandler("/Home/Error");
            }

            HttpHelper.Configure(app.ApplicationServices.GetRequiredService<IHttpContextAccessor>());

            app.Use(next => async context =>
            {
                context.Response.Headers.Add(new KeyValuePair<string, StringValues>("Access-Control-Allow-Origin", AllowedOrigins));

                await next.Invoke(context); // call the next guy

                // do some more stuff here as the call is unwinding
            });

            // Add the platform handler to the request pipeline.
            app.UseIISPlatformHandler();

            // Add static files to the request pipeline.
            app.UseStaticFiles();

            // Add MVC to the request pipeline.
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                // Uncomment the following line to add a route for porting Web API 2 controllers.
                // routes.MapWebApiRoute("DefaultApi", "api/{controller}/{id?}");
            });

            // Add SignalR to the request pipeline.
            app.UseSignalR();

            var db = new DatabaseContext(PaulRepository.Filename);
            db.Database.Migrate();
            db.Database.EnsureCreated();
            //Uncomment to see SQL queries
            //db.LogToConsole();
            PaulRepository.Initialize();
        }
    }
}
