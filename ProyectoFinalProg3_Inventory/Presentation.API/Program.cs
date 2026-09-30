using Application.Interfaces;
using Application.Services;
using Core.Application.Configurations;
using Core.Application.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
// 1. Registro obligatorio para Swagger clásico
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa el token JWT así: Bearer {tu token}"
    });

    opt.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

//cargar y registrar la configuración de JwtSettings desde appsettings.json
var jwtSettingsSection = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    // Si no se encuentra la configuración, lanzar una excepción
    ?? throw new InvalidOperationException($"No se encontró la configuración de JwtSettings.");
//Aplicamos la configuración de JwtSettings al contenedor de servicios
builder.Services.AddSingleton(jwtSettingsSection);


//Configurar Autenticación Bearer
builder.Services.AddAuthentication(options =>
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
        ValidIssuer = jwtSettingsSection.Issuer,
        ValidAudience = jwtSettingsSection.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSettingsSection.SecretKey))
    };

    //Validacion del RF-CA-18: Rechazar tokens revocados (si el token está en la lista de revocados, no es válido)
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var jti = context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;

            if (!string.IsNullOrEmpty(jti))
            {
                var estaRevocado = await dbContext.TokensRevocados.AnyAsync(t => t.TokenJti == jti);
                if (estaRevocado)
                {
                    context.Fail("La credencial de sesión ha sido cerrada y ya no es válida.");
                }
            }
        }
    };
});

// Registrar DbContext usando "DefaultConnection"
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddHostedService<ProcesadorCorreosWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();   // <-- Habilita el middleware de Swagger
    app.UseSwaggerUI(); // <-- Habilita la interfaz gráfica
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication(); // <-- Habilita la autenticación antes de la autorización
app.UseAuthorization();

app.MapControllers();

app.Run();
