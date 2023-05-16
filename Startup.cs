using MCPhase3.CodeRepository;
using MCPhase3.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Weather.UI.Utilties;

namespace MCPhase3
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
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(10);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
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

            services.AddSingleton<IRedisCache, RedisCache>();
            services.AddDistributedMemoryCache();
            // Add Redis services to the container.
            services.AddStackExchangeRedisCache(options => {
                options.Configuration = Configuration.GetConnectionString("RedisCacheUrl");
                //    options.InstanceName = builder.Configuration.GetValue<string>("RedisInstance");
            });


            //remittance protector classes Dependency injection
            //services.AddSingleton<UniqueCode>();
            //services.AddSingleton<CustomDataProtection>();

            //add following compatibility for tempdata cache memory.
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
            .AddSessionStateTempDataProvider();
            services.AddSession();
            services.AddControllersWithViews();           
            services.AddMvc();           
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
             
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //call error view directly -- this works
            //app.UseStatusCodePagesWithReExecute("/Home/Error", "?code={0}");

            // this will return nice 404 error or 500 error page
            //app.UseStatusCodePages();

            app.UseStatusCodePagesWithRedirects("/ErrorHandlerMiddleware/HttpStatusCodeHandler/{0}");
            //app.UseStatusCodePagesWithReExecute("/ErrorHandlerMiddleware/HttpStatusCodeHandler", "?statusCode={0}");

            //app.UseExceptionHandler("/ErrorHandlerMiddleware/PageNotFound/{0}");


            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseSession();
            app.UseRouting();

            app.UseMiddleware<UserSessionHandlerMiddleWare>("test");

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
