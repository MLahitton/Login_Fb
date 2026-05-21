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
// Permite que ASP.NET Core detecte y use controladores,
// por ejemplo: Api/Controllers/AuthController.cs
builder.Services.AddControllers();

// ======================================================
// 2. Application layer
// ======================================================
// Registra servicios de la capa Application.
// Ejemplo: IAuthService -> AuthService
builder.Services.AddApplicationServices();

// ======================================================
// 3. Infrastructure layer
// ======================================================
// Registra PostgreSQL, Identity, JwtService, EmailService
// y las configuraciones de JwtSettings / EmailSettings.
builder.Services.AddInfrastructureServices(builder.Configuration);

// ======================================================
// 4. CORS
// ======================================================
// Permite que el frontend pueda consumir esta API aunque
// esté corriendo en otro puerto.
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
// Lee la configuración JWT desde Api/appsettings.json.
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key no está configurado.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer no está configurado.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience no está configurado.");

// Configura autenticación con JWT Bearer.
// Esto permite validar tokens enviados así:
// Authorization: Bearer TU_ACCESS_TOKEN
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // En desarrollo se deja en false para evitar problemas
        // con certificados HTTPS locales.
        options.RequireHttpsMetadata = false;

        // Guarda el token dentro del contexto de autenticación.
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Valida que el token haya sido emitido por el issuer esperado.
            ValidateIssuer = true,

            // Valida que el token esté dirigido a la audience esperada.
            ValidateAudience = true,

            // Valida que la firma del token sea correcta.
            ValidateIssuerSigningKey = true,

            // Valida que el token no esté vencido.
            ValidateLifetime = true,

            // Valores esperados desde appsettings.json.
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,

            // Clave secreta usada para validar la firma del JWT.
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            ),

            // Evita tolerancia extra en la expiración del token.
            ClockSkew = TimeSpan.Zero
        };
    });

// ======================================================
// 6. Authorization
// ======================================================
// Permite usar [Authorize] en controladores o endpoints.
builder.Services.AddAuthorization();

// ======================================================
// 7. Swagger / OpenAPI
// ======================================================
// Habilita la documentación interactiva de la API.
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Login Facebook Auth API",
        Version = "v1",
        Description = "API de autenticación con Identity, JWT, PostgreSQL y recuperación por correo."
    });

    // Define el esquema Bearer para que Swagger muestre el botón Authorize.
    // Esta configuración no fuerza el requisito global para evitar conflictos
    // con versiones recientes de Microsoft.OpenApi.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa el JWT en el botón Authorize. Ejemplo: eyJhbGciOiJIUzI1NiIs..."
    });
});

// ======================================================
// 8. Build app
// ======================================================
// Construye la aplicación con todos los servicios registrados.
var app = builder.Build();

// ======================================================
// 9. Swagger middleware
// ======================================================
// Swagger se activa en entorno de desarrollo.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ======================================================
// 10. HTTPS
// ======================================================
// Redirige HTTP a HTTPS.
app.UseHttpsRedirection();

// ======================================================
// 11. CORS middleware
// ======================================================
// Debe ejecutarse antes de Authentication y Authorization.
app.UseCors("FrontendPolicy");

// ======================================================
// 12. Authentication middleware
// ======================================================
// Lee y valida el JWT enviado en el header Authorization.
app.UseAuthentication();

// ======================================================
// 13. Authorization middleware
// ======================================================
// Aplica reglas de autorización y atributos [Authorize].
app.UseAuthorization();

// ======================================================
// 14. Controllers
// ======================================================
// Activa los endpoints definidos en los controladores.
app.MapControllers();

// ======================================================
// 15. Run
// ======================================================
// Inicia la API.
app.Run();