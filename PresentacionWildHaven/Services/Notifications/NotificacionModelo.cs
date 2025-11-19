namespace PresentacionWildHaven.Services.Notifications
{
    public class NotificacionModelo
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Mensaje { get; set; } = string.Empty;
        public TipoNotificacion Tipo { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public bool Visible { get; set; } = true;
        
        public string ObtenerClaseCSS()
        {
            return Tipo switch
            {
                TipoNotificacion.Exito => "alert-success",
                TipoNotificacion.Error => "alert-danger",
                TipoNotificacion.Advertencia => "alert-warning",
                TipoNotificacion.Info => "alert-info",
                _ => "alert-secondary"
            };
        }

        public string ObtenerIcono()
        {
            return Tipo switch
            {
                TipoNotificacion.Exito => "✅",
                TipoNotificacion.Error => "❌",
                TipoNotificacion.Advertencia => "⚠️",
                TipoNotificacion.Info => "ℹ️",
                _ => "🔔"
            };
        }
    }
}