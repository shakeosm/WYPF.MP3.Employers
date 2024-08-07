using MCPhase3.CodeRepository;
using MCPhase3.CodeRepository.ActionFilters;
using MCPhase3.CodeRepository.InsertDataProcess;
using MCPhase3.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace MCPhase3
{
    public class Startup
    {
        private readonly int EXPIRY_MINUTES;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            EXPIRY_MINUTES = Configuration.GetValue<int>("RedisExpiryMinutes");
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(EXPIRY_MINUTES);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // The Tempdata provider cookie is not essential. Make it essential
            // so Tempdata is functional when tracking is disabled.
            services.Configure<CookieTempDataProviderOptions>(options =>
            {
                options.Cookie.IsEssential = true;
                options.Cookie.MaxAge = TimeSpan.FromMinutes(EXPIRY_MINUTES);
            });

            //to increase a rest api wait to maximum.
            services.Configure<FormOptions>(options =>
            {
                options.ValueLengthLimit = int.MaxValue;
                options.MultipartBodyLengthLimit = long.MaxValue; // <-- ! long.MaxValue
                options.MultipartBoundaryLengthLimit = int.MaxValue;
                options.MultipartHeadersCountLimit = int.MaxValue;
                options.MultipartHeadersLengthLimit = int.MaxValue;
            });

            services.Configure<ApiEndpoints>(Configuration.GetSection("ApiEndpoints"));

            services.AddTransient<ICommonRepo, CommonRepo>();
            services.AddTransient<IValidateExcelFile, ValidateExcelFile>();
            services.AddTransient<IExcelData, ExcelData>();
            services.AddTransient<ICheckTotalsService, CheckTotalsService>();
            services.AddTransient<IFileCountService, FileCountService>();

            // Add Redis services to the container.
            services.AddSingleton<IRedisCache, RedisCache>();
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = Configuration.GetConnectionString("RedisCacheUrl");
                //    options.InstanceName = builder.Configuration.GetValue<string>("RedisInstance");
            });

            services.AddDataProtection().SetDefaultKeyLifetime(TimeSpan.FromDays(10)).SetApplicationName("MP3.Phase3");

            //## Setting the UserSession Check ActionFilter Globally-> for All Controller->Actions
            services.AddControllersWithViews((options) =>
            {
                options.Filters.Add<UserSessionCheckActionFilter>();
            });
            
            services.AddMvc();

            //## new fix following the PenTest report
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(366);
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                //app.UseStatusCodePagesWithRedirects("/ErrorHandlerMiddleware/HttpStatusCodeHandler/{0}");
                app.UseStatusCodePagesWithReExecute("/ErrorHandlerMiddleware/HttpStatusCodeHandler", "?statusCode={0}");

                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                //app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseSession();
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Login}/{action=Index}/{id?}");
            });
        }
    }
}
