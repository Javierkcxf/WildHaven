using PresentacionWildHaven.Models;

namespace PresentacionWildHaven.Patterns.Observer
{
    public class ActualizadorDashboard : IObserver
    {
        public event Action? OnDashboardActualizado;

        public void Actualizar(Reporte reporte, string? estadoAnterior = null)
        {
            Console.WriteLine($"📊 [Dashboard] Reporte #{reporte.ReporteID} actualizado");
            Console.WriteLine($"   Estado anterior: {estadoAnterior}");
            Console.WriteLine($"   Estado nuevo: {reporte.Estado?.Nombre}");

            // ✅ Notificar al dashboard que debe refrescarse
            OnDashboardActualizado?.Invoke();
        }
    }
}