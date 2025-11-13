
using System;

using Microsoft.AspNetCore.Builder;

using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    "tablasprohibidas.json",
    optional: true,
    reloadOnChange: true
);

builder.Services.AddControllers();

builder.Services.AddCors(opts =>
{
    opts.AddPolicy("PermitirTodo", politica => politica
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
    );
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opciones =>
{

    opciones.IdleTimeout = TimeSpan.FromMinutes(30);

    opciones.Cookie.HttpOnly = true;

    opciones.Cookie.IsEssential = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<webapicsharp.Servicios.Abstracciones.IServicioCrud,
                           webapicsharp.Servicios.ServicioCrud>();

builder.Services.AddSingleton<webapicsharp.Servicios.Abstracciones.IProveedorConexion,
                              webapicsharp.Servicios.Conexion.ProveedorConexion>();

var proveedorBD = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";

builder.Services.AddScoped<webapicsharp.Servicios.Abstracciones.IServicioConsultas,
    webapicsharp.Servicios.ServicioConsultas>();

switch (proveedorBD.ToLower())
{
    case "postgres":

                builder.Services.AddScoped<webapicsharp.Repositorios.Abstracciones.IRepositorioLecturaTabla,
                                           webapicsharp.Repositorios.RepositorioLecturaPostgreSQL>();

        builder.Services.AddScoped<
            webapicsharp.Repositorios.Abstracciones.IRepositorioConsultas,
            webapicsharp.Repositorios.RepositorioConsultasPostgreSQL
        >();                                           
        break;
    case "mariadb":
    case "mysql":

        builder.Services.AddScoped<
            webapicsharp.Repositorios.Abstracciones.IRepositorioLecturaTabla,
            webapicsharp.Repositorios.RepositorioLecturaMysqlMariaDB>();

        builder.Services.AddScoped<
            webapicsharp.Repositorios.Abstracciones.IRepositorioConsultas,
            webapicsharp.Repositorios.RepositorioConsultasMysqlMariaDB>();

        break;

    case "sqlserver":
    case "sqlserverexpress":
    case "localdb":
    default:

        builder.Services.AddScoped<webapicsharp.Repositorios.Abstracciones.IRepositorioLecturaTabla,
                                   webapicsharp.Repositorios.RepositorioLecturaSqlServer>();

        builder.Services.AddScoped<webapicsharp.Repositorios.Abstracciones.IRepositorioConsultas,
                               webapicsharp.Repositorios.RepositorioConsultasSqlServer>();

        break;
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{

    c.SwaggerEndpoint("/swagger/v1/swagger.json", "webapicsharp v1");

    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseCors("PermitirTodo");

app.UseSession();

app.UseAuthorization();

app.MapControllers();

app.Run();
