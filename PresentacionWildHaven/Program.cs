using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PresentacionWildHaven;
using PresentacionWildHaven.Services;
using PresentacionWildHaven.Services.Auth;
using PresentacionWildHaven.Services.Notifications;
using Microsoft.JSInterop;
var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


var apiUrl = "http://localhost:5031/";  // ← CAMBIA ESTO A TU URL

builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(apiUrl)
});

builder.Services.AddHttpClient("ApiGenerica", client =>
{
    client.BaseAddress = new Uri(apiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<SesionService>();
builder.Services.AddScoped<ServicioApiGenerico>();
builder.Services.AddScoped<ServicioAutenticacion>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddSingleton<INotificable, NotificacionService>();
var app = builder.Build();
try
{
    var sesion = app.Services.GetRequiredService<SesionService>();
    await sesion.RestaurarSesionAsync();
}
catch { }
await builder.Build().RunAsync();