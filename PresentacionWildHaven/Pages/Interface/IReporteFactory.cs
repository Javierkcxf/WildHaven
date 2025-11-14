using PresentacionWildHaven.Models;

namespace PresentacionWildHaven.Interface
{
    public interface IReporteFactory
    {
        Reporte Crear(Reporte request);
    }

}