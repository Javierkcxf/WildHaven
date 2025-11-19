using PresentacionWildHaven.Models;

namespace PresentacionWildHaven.Patterns.Observer
{
    public interface IObserver
    {
        void Actualizar(Reporte reporte, string? estadoAnterior = null);
    }
}