using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StockPortal.API.Middleware;
using StockPortal.API.Services;
using StockPortal.Core.Interfaces;
using StockPortal.Infrastructure.Data;
using StockPortal.Infrastructure.Repositories;
using StockPortal.Infrastructure.Services;

namespace StockPortal.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // CORS Policy for Desktop & Web clients
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // JWT Bearer Authentication
            var jwtSecret = Configuration["Jwt:SecretKey"] ?? "StockPortalApp_SecureSecretKey_ForEnterpriseProduction_2026!#@$";
            var issuer = Configuration["Jwt:Issuer"] ?? "StockPortalApi";
            var audience = Configuration["Jwt:Audience"] ?? "StockPortalClients";

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // set to true in strictly SSL production
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            });

            // Swagger with JWT Support
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Stock Portal API",
                    Version = "v1",
                    Description = "Production-grade backend REST API for Stock Portal ERP"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Dependency Injection
            services.AddSingleton<IDapperContext, DapperContext>();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IExcelService, ExcelService>();

            // Repositories
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IVendorRepository, VendorRepository>();
            services.AddScoped<IPurchaseRepository, PurchaseRepository>();
            services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
            services.AddScoped<IFixedAssetRepository, FixedAssetRepository>();
            services.AddScoped<IDamageRepository, DamageRepository>();
            services.AddScoped<IFoodPacketRepository, FoodPacketRepository>();
            services.AddScoped<IGoodsReturnRepository, GoodsReturnRepository>();
            services.AddScoped<IIssueRepository, IssueRepository>();
            services.AddScoped<IDistributionRepository, DistributionRepository>();
            services.AddScoped<IUtilityRepository, UtilityRepository>();
            services.AddScoped<IQuotationRepository, QuotationRepository>();
            services.AddScoped<IReportRepository, ReportRepository>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Global Exception Handler
            app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

            // Request Logging Middleware (Prints every API call clearly to Console & NLog)
            app.UseMiddleware<RequestLoggingMiddleware>();

            if (env.IsDevelopment() || true) // Enable swagger for initial testing
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Stock Portal API v1"));
            }

            app.UseRouting();

            app.UseCors("AllowAll");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
