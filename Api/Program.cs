using System.Text;
using Application.DependencyInjection;
using Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// 1. Controllers
// ======================================================
builder.Services.AddControllers();

// ======================================================
// 2. Application layer
// ======================================================
builder.Services.AddApplicationServices();

// ======================================================
// 3. Infrastructure layer
// ======================================================
builder.Services.AddInfrastructureServices(builder.Configuration);

// ======================================================
// 4. CORS
// ======================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(
                "https://localhost:5002",
                "http://localhost:5002",
                "https://localhost:7002",
                "http://localhost:7002",
                "https://localhost:7065",
                "http://localhost:5065"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ======================================================
// 5. JWT Authentication
// ======================================================
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key no está configurado.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer no está configurado.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience no está configurado.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            ),

            ClockSkew = TimeSpan.Zero
        };
    });

// ======================================================
// 6. Authorization
// ======================================================
builder.Services.AddAuthorization();

// ======================================================
// 7. Swagger / OpenAPI
// ======================================================
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Login Facebook Auth API",
        Version = "v1",
        Description = "API de autenticación con Identity, JWT, PostgreSQL y recuperación por correo."
    });

    // Configuración compatible con Swashbuckle.AspNetCore 10.x + Microsoft.OpenApi.
    // En Swagger, debes pegar el token así:
    // Bearer TU_ACCESS_TOKEN
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Escribe: Bearer TU_ACCESS_TOKEN"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });
});

// ======================================================
// 8. Build app
// ======================================================
var app = builder.Build();

// ======================================================
// 9. Swagger middleware
// ======================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ======================================================
// 10. HTTPS
// ======================================================
app.UseHttpsRedirection();

// ======================================================
// 11. CORS middleware
// ======================================================
app.UseCors("FrontendPolicy");

// ======================================================
// 12. Authentication middleware
// ======================================================
app.UseAuthentication();

// ======================================================
// 13. Authorization middleware
// ======================================================
app.UseAuthorization();

// ======================================================
// 14. Controllers
// ======================================================
app.MapControllers();

// ======================================================
// 15. Run
// ======================================================
app.Run();