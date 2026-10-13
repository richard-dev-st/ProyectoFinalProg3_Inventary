using Application.Interfaces;
using Core.Application.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

//La raíz de configuración se fija en la carpeta del ejecutable para que
//appsettings.json se encuentre sin importar desde dónde se lance el worker.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

//Lee ConnectionStrings:DefaultConnection desde appsettings.json y lo inyecta en el DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("No se encontró 'DefaultConnection'.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHostedService<ProcesadorCorreosWorker>();

await builder.Build().RunAsync();
