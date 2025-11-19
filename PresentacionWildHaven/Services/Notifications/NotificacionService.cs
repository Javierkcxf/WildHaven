using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace PresentacionWildHaven.Services.Notifications
{
    public class NotificacionService : INotificable
    {
        private readonly List<NotificacionModelo> _notificaciones = new();
        private readonly System.Timers.Timer _timer;

        public event Action? OnChange;

        public IEnumerable<NotificacionModelo> Notificaciones => _notificaciones.Where(n => n.Visible);

        public NotificacionService()
        {
            // Timer para auto-ocultar notificaciones cada 5 segundos
            _timer = new System.Timers.Timer(5000);
            _timer.Elapsed += LimpiarNotificacionesAntiguas;
            _timer.Start();
        }

        public void MostrarNotificacion(string mensaje, TipoNotificacion tipo = TipoNotificacion.Info)
        {
            var notificacion = new NotificacionModelo
            {
                Mensaje = mensaje,
                Tipo = tipo
            };

            _notificaciones.Add(notificacion);
            NotificarCambio();

            // Auto-ocultar después de 5 segundos
            _ = Task.Run(async () =>
            {
                await Task.Delay(5000);
                OcultarNotificacion(notificacion.Id);
            });
        }

        public void MostrarExito(string mensaje)
        {
            MostrarNotificacion(mensaje, TipoNotificacion.Exito);
        }

        public void MostrarError(string mensaje)
        {
            MostrarNotificacion(mensaje, TipoNotificacion.Error);
        }

        public void MostrarAdvertencia(string mensaje)
        {
            MostrarNotificacion(mensaje, TipoNotificacion.Advertencia);
        }

        public void MostrarInfo(string mensaje)
        {
            MostrarNotificacion(mensaje, TipoNotificacion.Info);
        }

        public void OcultarNotificacion(Guid id)
        {
            var notificacion = _notificaciones.FirstOrDefault(n => n.Id == id);
            if (notificacion != null)
            {
                notificacion.Visible = false;
                NotificarCambio();
            }
        }

        private void LimpiarNotificacionesAntiguas(object? sender, ElapsedEventArgs e)
        {
            var notificacionesAEliminar = _notificaciones
                .Where(n => !n.Visible && (DateTime.Now - n.FechaCreacion).TotalSeconds > 10)
                .ToList();

            foreach (var notificacion in notificacionesAEliminar)
            {
                _notificaciones.Remove(notificacion);
            }

            if (notificacionesAEliminar.Any())
            {
                NotificarCambio();
            }
        }

        private void NotificarCambio()
        {
            OnChange?.Invoke();
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}