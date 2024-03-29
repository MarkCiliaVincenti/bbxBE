﻿using bbxBE.Common.Consts;
using IdentityModel;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace bbxBE.WebApi.Extensions
{
    public static class ServiceExtensions
    {

        public static void AddSwaggerExtension(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.IncludeXmlComments(XmlCommentsFilePath);
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Clean Architecture - bbxBE",
                    Description = "This Api will be responsible for overall data distribution and authorization.",
                    Contact = new OpenApiContact
                    {
                        Name = "Jane Doe",
                        Email = "jdoe@janedoe.com",
                        Url = new Uri("https://janedoe.com/contact"),
                    }
                });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    Description = "Input your Bearer token in this format - Bearer {your token here} to access this API",
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer",
                            },
                            Scheme = "Bearer",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                        }, new List<string>()
                    },
                });
                c.CustomSchemaIds(x => x.FullName); //https://stackoverflow.com/questions/65196695/swagger-swashbuckle-does-not-support-nested-class-as-action-method-parameter

            });
        }

        public static void AddControllersExtension(this IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                })
                ;
        }

        //Configure CORS to allow any origin, header and method. 
        //Change the CORS policy based on your requirements.
        //More info see: https://docs.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-3.0

        public static void AddCorsExtension(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                });
            });
        }


        public static void AddVersionedApiExplorerExtension(this IServiceCollection services)
        {
            services.AddVersionedApiExplorer(o =>
            {
                o.GroupNameFormat = "'v'VVV";
                o.SubstituteApiVersionInUrl = true;
            });
        }
        public static void AddApiVersioningExtension(this IServiceCollection services)
        {
            services.AddApiVersioning(config =>
            {
                // Specify the default API Version as 1.0
                config.DefaultApiVersion = new ApiVersion(1, 0);
                // If the client hasn't specified the API version in the request, use the default API version number 
                config.AssumeDefaultVersionWhenUnspecified = true;
                // Advertise the API versions supported for the particular endpoint
                config.ReportApiVersions = true;
            });
        }
        public static void AddJWTAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var JWTKey = configuration["JWTSettings:Key"];
            var JWTIssuer = configuration["JWTSettings:Issuer"];
            var JWTAudience = configuration["JWTSettings:Audience"];

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = JWTIssuer,
                    ValidAudience = JWTAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWTKey)),
                    TokenDecryptionKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWTKey)),
                    ClockSkew = TimeSpan.Zero   //https://stackoverflow.com/questions/39728519/jwtsecuritytoken-doesnt-expire-when-it-should
                        };
                    });
            /* ori
            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
            .AddIdentityServerAuthentication(options =>
            {
                options.Authority = configuration["Sts:ServerUrl"];
                options.RequireHttpsMetadata = false;
            });
            */
        }


        public static void AddAuthorizationPolicies_HAVETOCHANGE(this IServiceCollection services, IConfiguration configuration)
        {
            string hradmin = configuration["ApiRoles:HRAdminRole"],
                    manager = configuration["ApiRoles:ManagerRole"], employee = configuration["ApiRoles:EmployeeRole"];

            services.AddAuthorization(options =>
            {
                options.AddPolicy(AuthorizationConsts.HrAdminPolicy, policy => policy.RequireAssertion(context => HasRole_ÁTALAKÍTANI(context.User, hradmin)));
                options.AddPolicy(AuthorizationConsts.ManagerPolicy, policy => policy.RequireAssertion(context => HasRole_ÁTALAKÍTANI(context.User, manager) || HasRole_ÁTALAKÍTANI(context.User, hradmin)));
                options.AddPolicy(AuthorizationConsts.EmployeePolicy, policy => policy.RequireAssertion(context => HasRole_ÁTALAKÍTANI(context.User, employee) || HasRole_ÁTALAKÍTANI(context.User, manager) || HasRole_ÁTALAKÍTANI(context.User, hradmin)));
            });


        }
        public static bool HasRole_ÁTALAKÍTANI(ClaimsPrincipal user, string role)
        {
            if (string.IsNullOrEmpty(role))
                return false;

            return user.HasClaim(c =>
                                (c.Type == JwtClaimTypes.Role || c.Type == $"client_{JwtClaimTypes.Role}") &&
                                System.Array.Exists(c.Value.Split(','), e => e == role)
                            );
        }

        static string XmlCommentsFilePath
        {
            get
            {
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                var fileName = typeof(Startup).GetTypeInfo().Assembly.GetName().Name + ".xml";
                return Path.Combine(basePath, fileName);
            }
        }

    }

}
