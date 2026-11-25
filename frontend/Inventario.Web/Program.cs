using Inventario.Web.Components;
using Inventario.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Authorization;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<ProtectedSessionStorage>();
builder.Services.AddScoped<AuthenticationStateProvider, SesionAuthStateProvider>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:8080";

builder.Services.AddHttpClient<AuthService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});

builder.Services.AddHttpClient<AdminUsuariosServices>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});

// RF-GP-01..07 y RF-GP-05: clientes de permisos y del endpoint protegido por permiso.
builder.Services.AddHttpClient<SolicitudesPermisosServices>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});

builder.Services.AddHttpClient<InventarioServices>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});

// RF-NEG-02..06: cliente de órdenes de compra (módulo de negocio).
builder.Services.AddHttpClient<OrdenesCompraServices>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});

// Datos maestros del módulo de negocio: proveedores y productos.
builder.Services.AddHttpClient<CatalogoServices>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});

// Práctica 4: cliente del manejador de documentos (RF-DOC-01..04).
builder.Services.AddHttpClient<DocumentosServices>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});

// Práctica 5: notificaciones, cola de correos y reportes.
builder.Services.AddHttpClient<NotificacionesServices>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});

builder.Services.AddHttpClient<CorreosServices>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});

builder.Services.AddHttpClient<ReportesServices>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});

// Asignación 4: consulta de auditoría (RF-AUD-07), solo Administrador.
builder.Services.AddHttpClient<AuditoriaServices>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});

builder.Services.AddScoped<SesionActual>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    // La sesión vive en el circuito (AuthorizeRouteView), no en el pipeline HTTP:
    // evitamos que el middleware de autorización intente un challenge con cookies.
    .WithMetadata(new AllowAnonymousAttribute());

app.Run();
