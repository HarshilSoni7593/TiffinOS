using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TiffinOS.API.Data;
using TiffinOS.API.Extensions;
using TiffinOS.API.Middleware;
using TiffinOS.API.Services;
using TiffinOS.API.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<CurrentUserContext>();
builder.Services.AddScoped<IAuthService, AuthService>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler
    .DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero  // no grace period on expiry
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "TiffinOS API", Version = "v1" });

    // Add JWT input to Swagger UI
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    c.OperationFilter<TenantHeaderOperationFilter>();
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthentication();

app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userContext = context.RequestServices
            .GetRequiredService<CurrentUserContext>();

        var claims = context.User.Claims.ToList();

        // .NET remaps 'sub' to this URI — use FirstOrDefault safely
        var userId = claims
            .FirstOrDefault(c =>
                c.Type == "sub" ||
                c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)
            ?.Value;

        var tenantId = claims
            .FirstOrDefault(c => c.Type == "tenant_id")
            ?.Value;

        var email = claims
            .FirstOrDefault(c =>
                c.Type == "email" ||
                c.Type == System.Security.Claims.ClaimTypes.Email)
            ?.Value;

        if (userId != null && tenantId != null && email != null)
        {
            userContext.UserId = Guid.Parse(userId);
            userContext.TenantId = Guid.Parse(tenantId);
            userContext.Email = email;
            userContext.Roles = claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToArray();
            userContext.Permissions = claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToArray();
            userContext.IsAuthenticated = true;
        }
    }

    await next();
});


app.UseAuthorization();
app.MapControllers();

app.Run();
