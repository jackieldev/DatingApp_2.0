using System;
using System.Net;
using System.Text;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        /* ConfigureServices UseNpgsql
                [Obsolete]
                public void ConfigureServices(IServiceCollection services)
                {
                    services.AddDbContext<DataContext>(x => x
                            .UseNpgsql(this.Configuration.GetConnectionString("DefaultConnection"))
                                .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.IncludeIgnoredWarning)));

                    services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                            .AddJsonOptions(opt => opt.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

                    services.AddCors();
                    services.Configure<CloudinarySettings>(this.Configuration.GetSection("CloudinarySettings"));
                    services.AddAutoMapper();
                    services.AddTransient<Seed>();
                    services.AddScoped<IAuthRepository, AuthRepository>();
                    services.AddScoped<IDatingRepository, DatingRepository>();
                    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(options =>
                        {
                            options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuerSigningKey = true,
                                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII
                                    .GetBytes(Configuration
                                    .GetSection("AppSettings:Token").Value)),
                                ValidateIssuer = false,
                                ValidateAudience = false
                            };
                        });
                    services.AddScoped<LogUserActivity>();
                }
        */

        [Obsolete]
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<DataContext>(x => x
                .UseSqlite(this.Configuration.GetConnectionString("DefaultConnection")));

            IdentityBuilder builder = services.AddIdentityCore<User>(opt =>
            {
                opt.Password.RequireDigit = false;
                opt.Password.RequiredLength = 4;
                opt.Password.RequireNonAlphanumeric = false;
                opt.Password.RequireUppercase = false;
            });

            builder = new IdentityBuilder(builder.UserType, typeof(Role), builder.Services);
            builder.AddEntityFrameworkStores<DataContext>();
            builder.AddRoleValidator<RoleValidator<Role>>();
            builder.AddRoleManager<RoleManager<Role>>();
            builder.AddSignInManager<SignInManager<User>>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII
                            .GetBytes(Configuration.GetSection("AppSettings:Token").Value)),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });

            services.AddAuthorization(op =>
            {
                op.AddPolicy("RequiredAdminRole", policy => policy.RequireRole("Admin"));
                op.AddPolicy("ModeratePhotoRole", policy => policy.RequireRole("Admin", "Moderator"));
                op.AddPolicy("VipOnle", policy => policy.RequireRole("VIP"));
            });

            services.AddMvc(options =>
            {
                var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
                options.Filters.Add(new AuthorizeFilter(policy));

            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
              .AddJsonOptions(opt => opt.SerializerSettings.ReferenceLoopHandling =
                                        Newtonsoft.Json.ReferenceLoopHandling.Ignore);

            services.AddCors();
            services.Configure<CloudinarySettings>(this.Configuration.GetSection("CloudinarySettings"));
            services.AddAutoMapper();
            services.AddTransient<Seed>();
            services.AddScoped<IDatingRepository, DatingRepository>();
            services.AddScoped<LogUserActivity>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, Seed seeder)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(builder =>
                {
                    builder.Run(async context =>
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                        var error = context.Features.Get<IExceptionHandlerFeature>();
                        if (error != null)
                        {
                            context.Response.AddApplicationError(error.Error.Message);
                            await context.Response.WriteAsync(error.Error.Message);
                        }
                    });
                });

                //app.UseHsts();
            }

            //app.UseHttpsRedirection();
            seeder.SeedUsers();
            app.UseCors(x => x.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            app.UseAuthentication();

            //wwwroot
            //app.UseDefaultFiles();
            //app.UseStaticFiles();

            app.UseMvc();
            //     routes => {
            //     routes.MapSpaFallbackRoute(
            //         name: "spa-fallback",
            //         defaults: new { controller = "Fallback", action = "Index" }
            //     );
            // });
        }
    }
}
