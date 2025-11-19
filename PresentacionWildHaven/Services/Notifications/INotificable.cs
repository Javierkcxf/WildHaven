namespace PresentacionWildHaven.Services.Notifications
{
    public interface INotificable
    {
        void MostrarNotificacion(string mensaje, TipoNotificacion tipo = TipoNotificacion.Info);
        void MostrarExito(string mensaje);
        void MostrarError(string mensaje);
        void MostrarAdvertencia(string mensaje);
        void MostrarInfo(string mensaje);
    }
}