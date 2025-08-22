using System.Reflection;
using System.Text;

using Application.Services;

using Domain.Abstractions.Database;
using Domain.Abstractions.Services;
using Domain.Entities.Users;

using Infrastructure.Database;
using Infrastructure.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using Shared.Constants;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddRepositories(Assembly.GetExecutingAssembly())
            .AddServices()
            .AddDatabase(configuration)
            .AddHealthChecks(configuration)
            .AddAuthenticationInternal(configuration)
            .AddAuthorizationInternal(configuration);

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IHttpContextService, HttpContextService>();
        services.AddScoped<IFileStoreService, FileStoreService>();
        services.AddScoped<IHtmlImageProcessorService, HtmlImageProcessorService>();

        services.AddScoped<ITodoService, TodoService>();

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services, Assembly assembly)
    {
        // Find all non-abstract classes that inherit from GenericRepository
        {
            // Find all non-abstract classes that inherit from GenericRepository
            var repositoryTypes = assembly.GetTypes()
                                          .Where(type => type is
                                                         {
                                                             IsClass               : true,
                                                             IsAbstract            : false,
                                                             BaseType              : not null,
                                                             BaseType.IsGenericType: true,
                                                         }
                                                      && type.BaseType.GetGenericTypeDefinition() == typeof(GenericRepository<,>));

            foreach (var repositoryType in repositoryTypes)
            {
                // Get the specific repository interface (like IHospitalRepository, ICountryRepository)
                var repositoryInterface = repositoryType.GetInterfaces()
                                                        .FirstOrDefault(i => !i.IsGenericType
                                                                          && i != typeof(IRepository<,>)
                                                                          && i.Name.EndsWith("Repository") // To Ensure naming convention
                                                                       );

                if (repositoryInterface != null)
                {
                    services.AddScoped(repositoryInterface, repositoryType);
                }
            }
        }

        // Find all non-abstract classes that inherit from RepositoryBase
        {
            // Find all non-abstract classes that inherit from RepositoryBase
            var repositoryTypes = assembly.GetTypes()
                                          .Where(type => type is
                                                         {
                                                             IsClass               : true,
                                                             IsAbstract            : false,
                                                             BaseType              : not null,
                                                             BaseType.IsGenericType: true,
                                                         }
                                                      && type.BaseType.GetGenericTypeDefinition() == typeof(RepositoryBase<,>));

            foreach (var repositoryType in repositoryTypes)
            {
                // Get the specific repository interface (like IHospitalRepository, ICountryRepository)
                var repositoryInterface = repositoryType.GetInterfaces()
                                                        .FirstOrDefault(i => !i.IsGenericType
                                                                          && i != typeof(IRepositoryBase<,>)
                                                                          && i.Name.EndsWith("Repository") // To Ensure naming convention
                                                                       );

                if (repositoryInterface != null)
                {
                    services.AddScoped(repositoryInterface, repositoryType);
                }
            }
        }

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        // "Environment" => "Development", "Live"
    string connectionString = configuration.GetConnectionString("Database") ?? throw new Exception("Connection string 'Database' not found");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new Exception($"Database connection string is empty. Environment: '{configuration["Environment"]}'");
        }

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options ) =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure();
            });
        });

        services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        return services;
    }

    private static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
    string connectionString = configuration.GetConnectionString("Database") ?? throw new Exception("Connection string 'Database' not found");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new Exception($"Database connection string is empty. Environment: '{configuration["Environment"]}'");
        }

        services
            .AddHealthChecks();

        return services;
    }

    private static IServiceCollection AddAuthenticationInternal(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        return services;
    }

    private static IServiceCollection AddAuthorizationInternal(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthorization(options =>
        {
            // options.AddPolicy(Auth.ApiKeyPolicy, policy =>
            // {
            //     policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
            //     // policy.AddRequirements(new ApiKeyRequirement());
            // });
        });

        services.Configure<IdentityOptions>(options =>
        {
            // Password settings.
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 6;
            options.Password.RequiredUniqueChars = 1;

            // Lockout settings.
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(Convert.ToInt32(configuration["JwtSettings:UserLockoutMinutes"]));
            options.Lockout.MaxFailedAccessAttempts = 4;
            options.Lockout.AllowedForNewUsers = true;

            // User settings.
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;
        });

        //Adding Authentication - JWT
        services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;

                    // options.DefaultScheme = "Cookies";
                    // options.DefaultChallengeScheme = "OAuth2";
                })
                .AddCookie("Cookies") // Cookie authentication for storing user sessions
                .AddJwtBearer(options =>
                {
                    options.SaveToken = false;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        RequireExpirationTime = true,
                        ClockSkew = TimeSpan.Zero,
                        ValidIssuer = configuration["JwtSettings:Issuer"],
                        ValidAudience = configuration["JwtSettings:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:Key"]!))
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                            {
                                context.Response.Headers.TryAdd("Token-Expired", "true");
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

        return services;
    }
}