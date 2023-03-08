using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LunazVehiclePartsTracker.Models;
using LunazVehiclePartsTracker.Code;
using SAPB1Commons.ServiceLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Task = System.Threading.Tasks.Task;
using System.Net;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json.Serialization;
using LunazVehiclePartsTracker.Code;
using LunazVehiclePartsTracker.Data;
using Syncfusion.Blazor.Popups;
using Syncfusion.Blazor;

namespace LunazVehiclePartsTracker
{
    public class Startup
    {
        private Microsoft.Extensions.Logging.ILogger _logger;

        public Startup(WebApplicationBuilder builder)
        {
            _builder = builder;
        }

        WebApplicationBuilder _builder;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices()
        {
            var Configuration = _builder.Configuration;
            var services = _builder.Services;

            // Add services to the container.
            _builder.Services.AddRazorPages();
            _builder.Services.AddServerSideBlazor();
            _builder.Services.AddSingleton<WeatherForecastService>();
            _builder.Services.AddSyncfusionBlazor();
            _builder.Services.AddControllersWithViews();
            _builder.Services.AddScoped<SfDialogService>();
            _builder.Services.Configure<ConnectionDetails>(Configuration.GetSection("ConnectionDetails"));

            _builder.Services.AddTransient<Client>();
            _builder.Services.AddSingleton<ConnectionPool>();


            services.AddTransient<UserDBConfig>(s => new UserDBConfig()
            {
                RequiredTables = new List<UDTConfig>()
                {
                    new UDTConfig() {
                        table="AA1",
                        description="Cache of User Preferences",
                        autoinc=false
                    },

                },
                RequiredFields = new List<UDFConfig>() {
                    new UDFConfig() {
                        table="@AA1",
                        field="UserKey",
                        description="Combined {UserCode}.{Setting} for the ItemType",
                        type=UDFConfig.db_Alpha,
                        size=254
                    },
                    new UDFConfig() {
                        table="@AA1",
                        field="ItemType",
                        description="Combined {Product}/{Feature} key",
                        type=UDFConfig.db_Alpha,
                        size=50
                    }

                }
            });
            //services.AddSingleton<IHostedService, Settings.InitializeUDTsService>();
            //services.AddHostedService<InitializeUDTsService>();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(config =>
                {
                    config.ExpireTimeSpan = TimeSpan.FromHours(8);
                    config.SlidingExpiration = true;
                    config.Events = new CookieAuthenticationEvents
                    {
                        OnRedirectToLogin = ctx =>
                        {
                            if (ctx.Request.Path.StartsWithSegments("/api"))
                            {
                                ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                            }
                            else
                            {
                                ctx.Response.Redirect(ctx.RedirectUri);
                            }
                            return Task.FromResult(0);
                        },
                        OnSigningIn = ctx =>
                        {
                            return Task.FromResult(0);
                        }
                    };
                });

            services.AddDistributedMemoryCache();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(2);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public WebApplication Configure()
        {
            var app = _builder.Build();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(@"Mgo+DSMBaFt/QHRqVVhjVFpFdEBBXHxAd1p/VWJYdVt5flBPcDwsT3RfQF5jSH5WdEViWXpfcndVRQ==;Mgo+DSMBPh8sVXJ0S0J+XE9HflRDX3xKf0x/TGpQb19xflBPallYVBYiSV9jS31Td0VmW39fcHFURGRbUg==;Mgo+DSMBMAY9C3t2VVhkQlFadVdJXGFWfVJpTGpQdk5xdV9DaVZUTWY/P1ZhSXxQdkRjXH1ecXRQR2JaU0c=;ODg1NTA1QDMyMzAyZTM0MmUzMFkwcmcyQmxuUUt6NHJqVHIxei81NlJuQjgzZ2VmbFpyYkdsTVRQTnZCWlU9");

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles(new StaticFileOptions
            {
                ServeUnknownFileTypes = true,
                DefaultContentType = "text/plane"
            });
            app.UseStaticFiles();

            app.UseRouting();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapRazorPages();
            //    endpoints.MapControllers();
            //    endpoints.MapFallbackToFile("index.razor");
            //});

            return app;
        }
    }
}
