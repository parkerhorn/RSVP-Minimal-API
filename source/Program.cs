using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WeddingAPI.Repository;
using WeddingAPI.Models;
using WeddingAPI.Services.Interfaces;
using WeddingAPI.Repository.Interfaces;
using WeddingAPI.Services;
using WeddingAPI.Helpers;
using Microsoft.OpenApi.Models;

namespace WeddingAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Wedding API", Version = "v1" });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
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

        var dbSettings = new DatabaseSettings();

        builder.Configuration.GetSection("ConnectionStrings").Bind(dbSettings);

        builder.Services.AddSingleton(dbSettings);

        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddScoped<IUnitOfWork<ApplicationDbContext>, UnitOfWork<ApplicationDbContext>>();

        builder.Services.AddScoped<IGenericAsyncDataService<RSVP, ApplicationDbContext>, GenericAsyncDataService<RSVP, ApplicationDbContext>>();

        var jwtSettings = new JwtSettings();
        builder.Configuration.GetSection("JWT").Bind(jwtSettings);

        // Validate JWT settings
        if (string.IsNullOrEmpty(jwtSettings.SecretKey))
        {
            throw new InvalidOperationException("JWT:SecretKey is required. Set it in environment variables or appsettings.Development.json");
        }

        builder.Services.AddSingleton(jwtSettings);

        var clientCredentials = builder.Configuration.GetSection("ApiClients").Get<List<ClientCredentials>>() ?? new List<ClientCredentials>();
        builder.Services.AddSingleton(clientCredentials);

        builder.Services.AddScoped<ITokenService, TokenService>();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.SecretKey)),
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            });

        builder.Services.AddAuthorization();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        EndpointMapper.MapEndpoints(app);

        app.Run();
    }
}
