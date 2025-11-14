using PresentacionWildHaven.Interface;
using PresentacionWildHaven.Models;

namespace PresentacionWildHaven.Implementacion
{

    public class ReporteUsuarioFactory : IReporteFactory
{
    public Reporte Crear(Reporte request)
    {
        var data = request as Reporte;

        if (data == null)
            throw new ArgumentException("Los datos enviados no corresponden a un reporte.");

        return new Reporte
        {
            UsuarioID = data.UsuarioID, // viene del token o del request
            EspecieID = data.EspecieID,
            DescripcionEspecie = data.DescripcionEspecie,
            EstadoAnimal = data.EstadoAnimal,
            DireccionTexto = data.DireccionTexto,
            
            // Datos NO usados en reportes con usuario
            NombreReportante = null,
            TelefonoReportante = null,
            TipoMascota = null,
            InformacionAdicional = null,

            EstadoID = data.EstadoID,
            FechaCreacion = DateTime.Now,
            FechaActualizacion = DateTime.Now
        };
    }
}
}
