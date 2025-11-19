using PresentacionWildHaven.Models;
using PresentacionWildHaven.Services.Notifications;

namespace PresentacionWildHaven.Patterns.Observer
{
    public class NotificadorUsuario : IObserver
    {
        private readonly INotificable _notificacionService;

        public NotificadorUsuario(INotificable notificacionService)
        {
            _notificacionService = notificacionService;
        }

        public void Actualizar(Reporte reporte, string? estadoAnterior = null)
        {
            var estadoActual = reporte.Estado?.Nombre ?? "Desconocido";
            string mensaje = $"Reporte #{reporte.ReporteID}: {estadoAnterior} → {estadoActual}"     ;

            switch (estadoActual)
            {
                case "En Atención":
                    _notificacionService.MostrarInfo(mensaje);
                    break;
                case "Resuelto":
                    _notificacionService.MostrarExito(mensaje);
                    break;
                case "Cancelado":
                    _notificacionService.MostrarAdvertencia(mensaje);
                    break;
                case "Requiere Rescate":
                    _notificacionService.MostrarError($"🚨 URGENTE - {mensaje}");
                    break;
                default:
                    _notificacionService.MostrarInfo(mensaje);
                    break;
            }
        }
    }
}