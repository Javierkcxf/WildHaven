using PresentacionWildHaven.Interface;
using PresentacionWildHaven.Models;

namespace PresentacionWildHaven.Implementacion
{
    public class ReporteAnonimoFactory : IReporteFactory
    {
        public Reporte Crear(Reporte request)
        {
            var data = request as Reporte;

            if (data == null)
                throw new ArgumentException("Los datos enviados no corresponden a un reporte.");

            return new Reporte
            {
                UsuarioID = null, // anonimo

                EspecieID = data.EspecieID,
                DescripcionEspecie = data.DescripcionEspecie,
                EstadoAnimal = data.EstadoAnimal,
                DireccionTexto = data.DireccionTexto,

                // Datos del ciudadano
                NombreReportante = data.NombreReportante,
                TelefonoReportante = data.TelefonoReportante,
                TipoMascota = data.TipoMascota,
                InformacionAdicional = data.InformacionAdicional,

                EstadoID = data.EstadoID,
                FechaCreacion = DateTime.Now,
                FechaActualizacion = DateTime.Now
            };
        }
    }

}