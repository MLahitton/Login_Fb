# Guia Taller Completa - `Login_Fb`

Esta guia reconstruye el proyecto desde cero en una carpeta `Login_Fb`, hasta el estado funcional actual:

- Backend con `ASP.NET Core`, `Identity`, `JWT`, `EF Core`, `PostgreSQL`, `MailKit`.
- Frontend con `Blazor WebAssembly`.
- Flujo completo: registro, confirmacion, login, refresh token, logout, recuperar password, dashboard de perfil.
- Persistencia definitiva de foto de perfil y portada por cuenta.

## Bloque 0 - Requisitos previos

```powershell
# .NET SDK
dotnet --list-sdks

# Recomendado: herramienta de migraciones
dotnet tool update --global dotnet-ef

# Verifica PostgreSQL local (servicio activo en 5432)
# (si usas pgAdmin, valida conexion al servidor localhost)
```

Lineas relevantes y funcion:

```text
dotnet tool update --global dotnet-ef
```

Instala/actualiza `dotnet-ef` para crear y aplicar migraciones.

Funcion del bloque completo:

Asegura que tu entorno de trabajo este realmente preparado para desarrollar sin bloqueos: SDK instalado, comandos de EF disponibles y motor de base de datos operativo. Si este bloque queda correcto, el resto del taller fluye porque ya puedes crear migraciones, compilar proyectos y ejecutar la solucion sin depender de configuraciones pendientes.

Extensiones de VS Code recomendadas:

```text
- C#
- C# Dev Kit
- .NET Install Tool for Extension Authors
- PostgreSQL (opcional, para inspeccionar datos)
```

## Bloque 1 - Crear solucion y proyectos

```powershell
mkdir Login_Fb
cd Login_Fb

# Solucion
dotnet new sln -n login_Fb

# Proyectos
dotnet new webapi -n Api --use-controllers
dotnet new classlib -n Application
dotnet new classlib -n Domain
dotnet new classlib -n Infrastructure
dotnet new blazorwasm -n Frontend
```

Lineas relevantes y funcion:

```powershell
dotnet new webapi -n Api --use-controllers
```

Crea el host HTTP del backend.

```powershell
dotnet new blazorwasm -n Frontend
```

Crea el frontend standalone (separado del backend).

Funcion del bloque completo:

Genera la estructura inicial del sistema separando responsabilidades desde el primer minuto. Este bloque evita mezclar backend, logica de negocio y frontend en un solo proyecto, lo que facilita mantenimiento, pruebas y escalabilidad a medida que el taller crece.

Archivos generados automaticamente por este bloque:

```text
- Api/WeatherForecast.cs
- Api/Api.http
- Frontend/Layout/* (base)
- Frontend/wwwroot/* (base)
- Archivos de proyecto .csproj
```

## Bloque 2 - Agregar proyectos a la solucion y referencias

```powershell
# Agregar proyectos a la solucion
dotnet sln add Api/Api.csproj
dotnet sln add Application/Application.csproj
dotnet sln add Domain/Domain.csproj
dotnet sln add Infrastructure/Infrastructure.csproj
dotnet sln add Frontend/Frontend.csproj

# Referencias entre capas
dotnet add Api/Api.csproj reference Application/Application.csproj
dotnet add Api/Api.csproj reference Infrastructure/Infrastructure.csproj

dotnet add Application/Application.csproj reference Domain/Domain.csproj

dotnet add Infrastructure/Infrastructure.csproj reference Application/Application.csproj
dotnet add Infrastructure/Infrastructure.csproj reference Domain/Domain.csproj
```

Lineas relevantes y funcion:

```powershell
dotnet add Api/Api.csproj reference Infrastructure/Infrastructure.csproj
```

Permite que `Api` use registro de servicios, DbContext y providers reales.

Funcion del bloque completo:

Define la direccion de dependencias de forma explicita para mantener la arquitectura limpia. Gracias a esto, cada capa sabe exactamente que puede consumir y se evita acoplamiento innecesario entre proyectos, algo clave cuando empieces a evolucionar funcionalidades o refactorizar.

## Bloque 3 - Instalar paquetes NuGet

### 3.1 Api

```powershell
dotnet add Api/Api.csproj package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.8
dotnet add Api/Api.csproj package Microsoft.AspNetCore.OpenApi --version 10.0.6
dotnet add Api/Api.csproj package Microsoft.EntityFrameworkCore.Design --version 10.0.8
dotnet add Api/Api.csproj package Swashbuckle.AspNetCore --version 10.1.7
```

### 3.2 Application

```powershell
dotnet add Application/Application.csproj package Microsoft.EntityFrameworkCore --version 10.0.8
dotnet add Application/Application.csproj package Microsoft.Extensions.DependencyInjection.Abstractions --version 10.0.8
dotnet add Application/Application.csproj package Microsoft.Extensions.Identity.Core --version 10.0.8
```

### 3.3 Domain

```powershell
dotnet add Domain/Domain.csproj package Microsoft.Extensions.Identity.Stores --version 10.0.8
```

### 3.4 Infrastructure

```powershell
dotnet add Infrastructure/Infrastructure.csproj package MailKit --version 4.16.0
dotnet add Infrastructure/Infrastructure.csproj package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 10.0.8
dotnet add Infrastructure/Infrastructure.csproj package Microsoft.EntityFrameworkCore --version 10.0.8
dotnet add Infrastructure/Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design --version 10.0.8
dotnet add Infrastructure/Infrastructure.csproj package Microsoft.EntityFrameworkCore.Sqlite --version 10.0.8
dotnet add Infrastructure/Infrastructure.csproj package Microsoft.Extensions.Configuration.Abstractions --version 10.0.8
dotnet add Infrastructure/Infrastructure.csproj package Microsoft.Extensions.DependencyInjection.Abstractions --version 10.0.8
dotnet add Infrastructure/Infrastructure.csproj package Microsoft.Extensions.Options.ConfigurationExtensions --version 10.0.8
dotnet add Infrastructure/Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.1
dotnet add Infrastructure/Infrastructure.csproj package System.IdentityModel.Tokens.Jwt --version 8.18.0
```

Lineas relevantes y funcion:

```text
Microsoft.AspNetCore.Authentication.JwtBearer
```

Valida tokens en endpoints protegidos.

```text
Npgsql.EntityFrameworkCore.PostgreSQL
```

Conecta EF Core con PostgreSQL.

```text
MailKit
```

Permite enviar codigos por SMTP.

Funcion del bloque completo:

Instala todas las librerias necesarias para cubrir el flujo completo del producto: seguridad con JWT, persistencia con EF Core + PostgreSQL, gestion de identidad con Identity y envio de correos con MailKit. En otras palabras, este bloque activa las capacidades tecnicas que hacen posible el taller de punta a punta.

## Bloque 4 - Estructura de carpetas de trabajo

```powershell
mkdir Domain/Entities

mkdir Application/DTOs
mkdir Application/Interfaces
mkdir Application/Services
mkdir Application/DependencyInjection

mkdir Infrastructure/Data
mkdir Infrastructure/Settings
mkdir Infrastructure/DependencyInjection

mkdir Api/Controllers
mkdir Api/wwwroot

mkdir Frontend/Services/Auth
mkdir Frontend/Models/Auth
mkdir Frontend/Pages/Auth/register
mkdir Frontend/Pages/Auth/login
mkdir Frontend/Pages/Auth/VerifyEmail
mkdir Frontend/Pages/Auth/Forgotpassword
mkdir Frontend/Pages/Auth/Resetpassword
mkdir Frontend/Pages/Auth/Dashboard
```

Funcion del bloque completo:

Crea un mapa de carpetas coherente para que cada archivo tenga un lugar claro desde el inicio. Esto reduce perdida de tiempo buscando codigo, mejora la lectura del proyecto y te permite escalar sin convertir la solucion en un monolito desordenado.

## Bloque 5 - Configuracion del backend (`Api`)

Archivo: `Api/Program.cs`

```csharp
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(apiWebRootPath)
});
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

Lineas relevantes y funcion:

```csharp
app.UseStaticFiles(new StaticFileOptions { ... })
```

Sirve imagenes subidas en `Api/wwwroot/uploads`.

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

Habilita seguridad JWT en controladores.

Funcion del bloque completo:

Configura el backend para operar en escenarios reales: registro de servicios, seguridad por token, politica CORS para frontend separado y pipeline HTTP con middleware en orden correcto. Este bloque es el corazon de arranque del API, porque define como se autentica, como expone rutas y como se conecta con el cliente.

Archivo: `Api/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=login_fb;Username=postgres;Password=1234"
  },
  "Jwt": {
    "Key": "ESTA_ES_UNA_CLAVE_SUPER_SECRETA_CAMBIAME_DESPUES_123456789",
    "Issuer": "login_Fb_api",
    "Audience": "login_Fb_frontend",
    "ExpirationMinutes": 15
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "SenderName": "Login Facebook Taller",
    "SenderEmail": "tu-correo@gmail.com",
    "Password": "tu-app-password"
  }
}
```

Funcion del bloque completo:

Centraliza la configuracion critica del sistema en un solo punto: conexion a base de datos, parametros de JWT y datos SMTP para correo. Con esto, el comportamiento del backend queda parametrizado por ambiente y no hardcodeado en clases, lo que facilita despliegue, mantenimiento y seguridad operativa.

## Bloque 6 - Dominio y persistencia (`Domain` + `Infrastructure`)

Archivo: `Domain/Entities/ApplicationUser.cs`

```csharp
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string Bio { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = "Activo";
    public string ProfilePhotoUrl { get; set; } = string.Empty;
    public string CoverPhotoUrl { get; set; } = string.Empty;
}
```

Funcion del bloque completo:

Define la entidad principal del sistema combinando los campos base de Identity con propiedades de perfil que usa la aplicacion. Este bloque permite pasar de un usuario tecnico minimo a un modelo funcional para una experiencia tipo red social.

Archivo: `Infrastructure/Data/ApplicationDbContext.cs`

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetCode> PasswordResetCodes => Set<PasswordResetCode>();
    public DbSet<EmailVerificationCode> EmailVerificationCodes => Set<EmailVerificationCode>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(x => x.TokenHash).IsUnique();
        });
    }
}
```

Funcion del bloque completo:

Modela la persistencia completa de autenticacion: tablas de Identity, refresh tokens, codigos de verificacion y recuperacion, incluyendo indices y relaciones. Esto garantiza integridad de datos y soporte para flujos sensibles como renovacion de sesion y reseteo de contrasena.

Archivo: `Infrastructure/DependencyInjection/InfrastructureServiceRegistration.cs`

```csharp
services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
});

services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
    options.Password.RequiredLength = 8;
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

services.AddScoped<IJwtService, JwtService>();
services.AddScoped<IEmailService, EmailService>();
```

Funcion del bloque completo:

Registra en DI toda la infraestructura que la aplicacion necesita para funcionar en runtime: contexto de datos, Identity, politicas de password/lockout y servicios de JWT/correo. El resultado es un backend configurable, desacoplado y listo para consumirse desde controladores.

## Bloque 7 - Capa Application (DTOs, interfaces y servicio)

Archivo: `Application/DTOs/ApiResponse.cs`

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T? data, string message = "Operacion realizada correctamente.") { ... }
    public static ApiResponse<T> Fail(string message) { ... }
}
```

Funcion del bloque completo:

Unifica el formato de salida de la API para que el frontend siempre reciba la misma estructura (`success`, `message`, `data`). Esto simplifica manejo de errores, mejora trazabilidad y evita condicionales inconsistentes en cada pantalla.

Archivo: `Application/Interfaces/IAuthService.cs`

```csharp
Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordRequest request);
Task<ApiResponse<ProfileResponse>> GetProfileAsync(string userId);
Task<ApiResponse<ProfileResponse>> UpdateProfileAsync(string userId, UpdateProfileRequest request);
Task<ApiResponse<ProfileResponse>> UpdateProfileImagesAsync(string userId, string? profilePhotoUrl, string? coverPhotoUrl);
```

Funcion del bloque completo:

Define la frontera de la capa de aplicacion: que operaciones existen y que devuelve cada una. Con este contrato, controladores y servicios se conectan de forma predecible, facilitando pruebas, refactor y evolucion del dominio.

Archivo: `Application/Services/AuthService.cs`

```csharp
public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
{
    var user = await _userManager.FindByEmailAsync(email);
    var passwordIsValid = await _userManager.CheckPasswordAsync(user, request.Password);

    var refreshToken = _jwtService.GenerateRefreshToken();
    _context.RefreshTokens.Add(new RefreshToken
    {
        UserId = user.Id,
        TokenHash = _jwtService.HashToken(refreshToken),
        ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
    });

    var authResponse = await BuildAuthResponseAsync(user, refreshToken);
    return ApiResponse<AuthResponse>.Ok(authResponse, "Login exitoso.");
}

public async Task<ApiResponse<ProfileResponse>> UpdateProfileImagesAsync(
    string userId,
    string? profilePhotoUrl,
    string? coverPhotoUrl)
{
    var user = await _userManager.FindByIdAsync(userId);
    if (!string.IsNullOrWhiteSpace(profilePhotoUrl)) user.ProfilePhotoUrl = profilePhotoUrl;
    if (!string.IsNullOrWhiteSpace(coverPhotoUrl)) user.CoverPhotoUrl = coverPhotoUrl;
    await _userManager.UpdateAsync(user);
    return ApiResponse<ProfileResponse>.Ok(ProfileResponse.FromUser(user), "Imagenes de perfil actualizadas.");
}
```

Lineas relevantes y funcion:

```csharp
TokenHash = _jwtService.HashToken(refreshToken)
```

Nunca se guarda el refresh token en texto plano.

```csharp
return ApiResponse<ProfileResponse>.Ok(ProfileResponse.FromUser(user), ...)
```

Devuelve el estado actualizado del perfil al frontend.

Funcion del bloque completo:

Implementa la logica central del negocio: registro, confirmacion, login, refresh token, logout, recuperacion de contrasena y actualizacion de perfil. Este bloque concentra reglas, validaciones y persistencia para que el controlador sea delgado y el comportamiento sea consistente.

## Bloque 8 - Controlador API (`Api/Controllers/AuthControllers.cs`)

```csharp
[HttpPost("register")]
[HttpPost("confirm-email")]
[HttpPost("login")]
[HttpPost("forgot-password")]
[HttpPost("reset-password")]
[HttpPost("refresh-token")]
[Authorize]
[HttpPost("logout")]
[Authorize]
[HttpGet("me")]
[Authorize]
[HttpGet("profile")]
[Authorize]
[HttpPut("profile")]
[Authorize]
[HttpPost("profile/photo")]
[Authorize]
[HttpPost("profile/cover")]
```

Bloque de validacion de upload:

```csharp
if (file.Length > MaxImageSizeBytes)
{
    return BadRequest(ApiResponse<string>.Fail("La imagen supera el maximo de 5MB."));
}

var extension = Path.GetExtension(file.FileName);
if (!AllowedImageExtensions.Contains(extension))
{
    return BadRequest(ApiResponse<string>.Fail("Formato no permitido. Usa JPG, PNG o WEBP."));
}
```

Funcion del bloque completo:

Traduce la logica de aplicacion a endpoints REST consumibles por el frontend, incluyendo validaciones de autenticacion y de carga de archivos. Aqui se controla que solo entren datos validos, se protege el sistema y se devuelve una respuesta uniforme.

## Bloque 9 - Servicios JWT y email (`Infrastructure/Settings`)

Archivo: `Infrastructure/Settings/JwtService.cs`

```csharp
var token = new JwtSecurityToken(
    issuer: _settings.Issuer,
    audience: _settings.Audience,
    claims: claims,
    expires: expiresAtUtc,
    signingCredentials: credentials);

return (handler.WriteToken(token), expiresAtUtc);
```

Funcion del bloque completo:

Encapsula la creacion de tokens con criterios de seguridad: firmado, expiracion controlada y refresh token no predecible. Este bloque es clave para mantener sesiones seguras sin forzar login continuo del usuario.

Archivo: `Infrastructure/Settings/EmailService.cs`

```csharp
await smtpClient.ConnectAsync(_settings.SmtpServer, _settings.Port, SecureSocketOptions.StartTls);
await smtpClient.AuthenticateAsync(_settings.SenderEmail, _settings.Password);
await smtpClient.SendAsync(message);
```

Funcion del bloque completo:

Implementa la comunicacion por correo para procesos criticos de cuenta (verificacion y recuperacion). Gracias a este bloque, el flujo de autenticacion deja de ser solo local y pasa a tener validaciones reales sobre propiedad del email.

## Bloque 10 - Frontend: configuracion y cliente HTTP

Archivo: `Frontend/Program.cs`

```csharp
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:5001/api/auth/";

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});

builder.Services.AddScoped<AuthApiService>();
```

Archivo: `Frontend/wwwroot/appsettings.json`

```json
{
  "Api": {
    "BaseUrl": "http://localhost:5000/api/auth/"
  }
}
```

Funcion del bloque completo:

Conecta el frontend con el backend mediante una base URL configurable y un servicio HTTP inyectado. Esto evita acoplar rutas en componentes y facilita cambios de ambiente sin tocar cada pagina manualmente.

Archivo: `Frontend/Services/Auth/AuthApiService.cs`

```csharp
await _js.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, accessToken);
await _js.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, refreshToken);

if (allowRefreshRetry && response.StatusCode == HttpStatusCode.Unauthorized)
{
    if (await TryRefreshTokenAsync())
    {
        await AttachAccessTokenAsync();
        response = await requestFactory();
    }
}
```

Bloque upload de imagen:

```csharp
using var content = new MultipartFormDataContent();
content.Add(streamContent, "file", file.Name);
using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
{
    Content = content
};
return await _http.SendAsync(request);
```

Bloque de normalizacion de URLs de imagen:

```csharp
public string ResolveAssetUrl(string? rawUrl)
{
    var apiOrigin = apiBase.GetLeftPart(UriPartial.Authority);
    return apiOrigin + normalized;
}
```

Funcion del bloque completo:

Concentra en una sola clase toda la comunicacion de auth/perfil: guardado de tokens, reintento por refresh, parseo uniforme de respuestas, errores de red y upload multipart. Este diseño mantiene los componentes Razor limpios y hace mas simple depurar incidencias.

## Bloque 11 - Frontend: pantallas del flujo

### Registro (`Frontend/Pages/Auth/register/Register.razor`)

```razor
<EditForm Model="registerModel" OnValidSubmit="HandleRegister">
...
var response = await AuthService.Register(registerModel);
if (response.Success)
{
    Navigation.NavigateTo($"/verify-code?email={email}");
}
```

Funcion del bloque completo:

Implementa la primera interaccion del usuario con el sistema: capturar datos, validar formulario, enviar solicitud al backend y encadenar el flujo hacia confirmacion de correo. Es la puerta de entrada al resto del proceso autenticado.

### Verificacion (`Frontend/Pages/Auth/VerifyEmail/VerifyCode.razor`)

```razor
var response = await AuthService.VerifyCode(model);
if (response.Success)
{
    Navigation.NavigateTo("/login");
}
```

Funcion del bloque completo:

Cierra el ciclo de activacion de cuenta validando que el codigo recibido por correo coincide y no esta vencido. Este paso evita cuentas no verificadas y refuerza seguridad antes de permitir login.

### Login (`Frontend/Pages/Auth/login/Login.razor`)

```razor
var response = await AuthService.Login(model);
if (response.Success)
{
    Navigation.NavigateTo("/dashboard");
}
```

Funcion del bloque completo:

Conecta credenciales con sesion real: autentica, persiste access/refresh token y habilita navegacion a pantallas protegidas. Este bloque marca la transicion entre usuario anonimo y usuario autenticado.

### Forgot Password (`Frontend/Pages/Auth/Forgotpassword/ForgotPassword.razor`)

```razor
var response = await AuthService.ForgotPassword(model);
if (response.Success)
{
    Navigation.NavigateTo($"/reset-password?email={email}");
}
```

Funcion del bloque completo:

Dispara el flujo de soporte de credenciales cuando el usuario olvida su contrasena. El backend responde de forma segura y el frontend encamina al formulario de cambio sin exponer informacion sensible.

### Reset Password (`Frontend/Pages/Auth/Resetpassword/ResetPassword.razor`)

```razor
var response = await AuthService.ResetPassword(model);
if (response.Success)
{
    Navigation.NavigateTo("/login");
}
```

Funcion del bloque completo:

Completa la recuperacion de acceso validando codigo temporal y aplicando la nueva contrasena. Al finalizar, el usuario puede volver a autenticarse con la nueva clave y los tokens anteriores quedan invalidados segun la logica del servicio.

### Dashboard (`Frontend/Pages/Auth/Dashboard/Dashboard.razor`)

```razor
var response = await AuthService.GetProfile();
ApplyProfileData(response.Data);

var uploadResponse = await AuthService.UploadProfilePhoto(file);
ApplyProfileData(uploadResponse.Data);
```

Funcion del bloque completo:

Orquesta la experiencia principal del usuario autenticado: lectura de perfil, edicion de datos personales y carga de imagenes con persistencia real por cuenta. Este bloque demuestra la integracion completa frontend-backend-base de datos en un caso de uso visible.

### Estilos de autenticacion y perfil (`.razor.css`)

```text
Frontend/Pages/Auth/register/Register.razor.css
Frontend/Pages/Auth/login/Login.razor.css
Frontend/Pages/Auth/VerifyEmail/VerifyCode.razor.css
Frontend/Pages/Auth/Forgotpassword/ForgotPassword.razor.css
Frontend/Pages/Auth/Resetpassword/ResetPassword.razor.css
Frontend/Pages/Auth/Dashboard/Dashboard.razor.css
```

Lineas relevantes y funcion:

```css
.profile-page { min-height: 100vh; background: #f1f5f9; }
.cover-image { object-fit: cover; }
.profile-photo { border-radius: 50%; object-fit: cover; }
```

Estas lineas sostienen el layout del dashboard y garantizan visualizacion correcta de portada/foto.

Funcion del bloque completo:

Aisla estilos por componente para evitar colisiones visuales entre pantallas y mantener mantenimiento sencillo. Ademas, el dashboard incluye reglas responsive para que portada, avatar, acciones y tarjetas se adapten correctamente en desktop y movil.

## Bloque 12 - Migraciones y base de datos

Comandos base:

```powershell
# Crear migracion inicial
dotnet ef migrations add InitialCreate --project Infrastructure/Infrastructure.csproj --startup-project Api/Api.csproj --context ApplicationDbContext

# Aplicar cambios en DB
dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project Api/Api.csproj --context ApplicationDbContext
```

Evolucion real usada en este proyecto:

```powershell
dotnet ef migrations add AddProfileFieldsToApplicationUser --project Infrastructure/Infrastructure.csproj --startup-project Api/Api.csproj --context ApplicationDbContext
dotnet ef migrations add AddProfileFieldsV2 --project Infrastructure/Infrastructure.csproj --startup-project Api/Api.csproj --context ApplicationDbContext
dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project Api/Api.csproj --context ApplicationDbContext
```

Archivos generados automaticamente por EF Core:

```text
Infrastructure/Migrations/<timestamp>_<Nombre>.cs
Infrastructure/Migrations/<timestamp>_<Nombre>.Designer.cs
Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs
```

Funcion del bloque completo:

Convierte cambios de entidades y configuracion EF en cambios reales sobre la base de datos usando migraciones versionadas. Este bloque te da trazabilidad de esquema y permite recrear el estado de datos en cualquier entorno de forma controlada.

## Bloque 13 - Ejecutar backend y frontend

Terminal 1 (API):

```powershell
dotnet run --project Api/Api.csproj --launch-profile https
```

Terminal 2 (Frontend):

```powershell
dotnet watch run --project Frontend/Frontend.csproj --launch-profile http
```

Funcion del bloque completo:

Pone en marcha el entorno completo de desarrollo con API y cliente corriendo al mismo tiempo. Esto permite validar escenarios end-to-end, depurar flujo real y comprobar inmediatamente el impacto de cada cambio.

## Bloque 14 - Pruebas funcionales por pantallas

```text
1. Register -> crea cuenta
2. Verify Code -> confirma email
3. Login -> entra al dashboard
4. Editar perfil -> guarda nombre/bio/ciudad/estado
5. Subir foto de perfil y portada
6. Cerrar sesion e iniciar de nuevo con la misma cuenta
7. Validar que foto/portada persisten
8. Probar con otra cuenta y validar fotos distintas
```

Funcion del bloque completo:

Ejecuta una bateria funcional sobre pantallas reales para confirmar que la integracion no es solo teorica: datos guardan, login persiste y cada cuenta mantiene su propio estado. Es el punto donde verificas que el taller cumple objetivos de negocio.

## Bloque 15 - Solucion de problemas frecuentes

Puerto ocupado `5126`:

```powershell
netstat -ano | findstr :5126
Stop-Process -Id <PID> -Force
```

Si frontend cae con `An unhandled error has occurred. Reload`:

```text
- Verifica que API este arriba en http://localhost:5000
- Verifica Frontend/wwwroot/appsettings.json
- Revisa consola del navegador y logs de dotnet watch
```

Si una imagen vieja no carga:

```text
- Re-subela una vez para que se guarde con la URL relativa nueva
- Confirma existencia en Api/wwwroot/uploads/<userId>
```

## Bloque 16 - Archivos que NO debes subir a Git

```text
- **/bin
- **/obj
- .vs/
- *.log
```

Esto ya esta cubierto por `.gitignore`:

```text
bin/
obj/
.vs/
*.user
*.suo
*.log
```

## Bloque 17 - Archivos del taller (manuales)

### Api

```text
Api/Api.csproj
Api/Program.cs
Api/appsettings.json
Api/Properties/launchSettings.json
Api/Controllers/AuthControllers.cs
```

### Application

```text
Application/Application.csproj
Application/DependencyInjection/ApplicationServiceRegistration.cs
Application/Interfaces/IApplicationDbContext.cs
Application/Interfaces/IAuthService.cs
Application/Interfaces/IEmailService.cs
Application/Interfaces/IJwtService.cs
Application/Services/AuthService.cs
Application/DTOs/ApiResponse.cs
Application/DTOs/AuthResponse.cs
Application/DTOs/ConfirmEmailRequest.cs
Application/DTOs/ForgotPasswordRequest.cs
Application/DTOs/LoginRequest.cs
Application/DTOs/LogoutRequest.cs
Application/DTOs/ProfileResponse.cs
Application/DTOs/RefreshTokenRequest.cs
Application/DTOs/RegisterRequest.cs
Application/DTOs/ResendEmailConfirmationRequest.cs
Application/DTOs/ResetPasswordRequest.cs
Application/DTOs/UpdateProfileRequest.cs
Application/DTOs/UserResponse.cs
```

### Domain

```text
Domain/Domain.csproj
Domain/Entities/ApplicationUser.cs
Domain/Entities/EmailVerificationCode.cs
Domain/Entities/PasswordResetCode.cs
Domain/Entities/RefreshToken.cs
```

### Infrastructure

```text
Infrastructure/Infrastructure.csproj
Infrastructure/Data/ApplicationDbContext.cs
Infrastructure/DependencyInjection/InfrastructureServiceRegistration.cs
Infrastructure/Settings/EmailService.cs
Infrastructure/Settings/EmailSettings.cs
Infrastructure/Settings/JwtService.cs
Infrastructure/Settings/JwtSettings.cs
Infrastructure/Migrations/*
```

### Frontend

```text
Frontend/Frontend.csproj
Frontend/Program.cs
Frontend/App.razor
Frontend/_Imports.razor
Frontend/Pages/Index.razor
Frontend/Services/Auth/AuthApiService.cs
Frontend/Models/Auth/ApiResponse.cs
Frontend/Models/Auth/AuthResponse.cs
Frontend/Models/Auth/ForgotPasswordRequest.cs
Frontend/Models/Auth/LoginRequest.cs
Frontend/Models/Auth/ProfileResponse.cs
Frontend/Models/Auth/RegisterRequest.cs
Frontend/Models/Auth/ResetPasswordRequest.cs
Frontend/Models/Auth/TokenRequests.cs
Frontend/Models/Auth/UpdateProfileRequest.cs
Frontend/Models/Auth/UserResponse.cs
Frontend/Models/Auth/VerifyCodeRequest.cs
Frontend/Pages/Auth/register/Register.razor
Frontend/Pages/Auth/register/Register.razor.css
Frontend/Pages/Auth/login/Login.razor
Frontend/Pages/Auth/login/Login.razor.css
Frontend/Pages/Auth/VerifyEmail/VerifyCode.razor
Frontend/Pages/Auth/VerifyEmail/VerifyCode.razor.css
Frontend/Pages/Auth/Forgotpassword/ForgotPassword.razor
Frontend/Pages/Auth/Forgotpassword/ForgotPassword.razor.css
Frontend/Pages/Auth/Resetpassword/ResetPassword.razor
Frontend/Pages/Auth/Resetpassword/ResetPassword.razor.css
Frontend/Pages/Auth/Dashboard/Dashboard.razor
Frontend/Pages/Auth/Dashboard/Dashboard.razor.css
Frontend/wwwroot/appsettings.json
Frontend/wwwroot/css/app.css
```

Nota importante:

```text
Frontend/loginrequest.cs esta vacio y no participa del flujo.
Puedes eliminarlo para evitar confusion.
```

## Bloque 18 - Comando de verificacion final

```powershell
# Build general (si tienes API corriendo, detenla antes)
dotnet build Api/Api.csproj

# Migraciones aplicadas
dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project Api/Api.csproj --context ApplicationDbContext
```

Funcion del bloque completo:

Cierra el taller con una verificacion tecnica minima de estabilidad: compilacion del API y esquema de base sincronizado. Si este bloque pasa, tienes una base consistente para versionar, desplegar o continuar nuevas funcionalidades sin deuda inmediata.

---

## Resultado esperado

Si seguiste toda la guia, debes tener un proyecto funcional en el que:

- El usuario se registra y confirma correo.
- Puede iniciar y cerrar sesion con JWT y refresh token.
- Puede recuperar contrasena por codigo.
- Puede editar perfil.
- Puede subir foto de perfil y portada.
- Las imagenes se mantienen al volver a entrar y cambian por cuenta.


