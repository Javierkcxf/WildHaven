using System;
using System.Text.Json.Serialization;
using PresentacionWildHaven.Patterns.Observer;

namespace PresentacionWildHaven.Models
{
    public class Reporte
    {
        [JsonIgnore]
        public int ReporteID { get; set; }
        public int? UsuarioID { get; set; }

        public int? EspecieID { get; set; }
        public string? DescripcionEspecie { get; set; }
        public string? EstadoAnimal { get; set; }
        public string? DireccionTexto { get; set; }

        public string? NombreReportante { get; set; }
        public string? TelefonoReportante { get; set; }
        public string? TipoMascota { get; set; }
        public string? InformacionAdicional { get; set; }

        public int? EstadoID { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
        [JsonIgnore]
        public EstadoReporte? Estado { get; set; }

        // ✅ PATRÓN OBSERVER
        private List<IObserver> _observadores = new();

        public void SuscribirObservador(IObserver observador)
        {
            if (!_observadores.Contains(observador))
            {
                _observadores.Add(observador);
            }
        }

        public void DesuscribirObservador(IObserver observador)
        {
            _observadores.Remove(observador);
        }

        public void CambiarEstado(int nuevoEstadoID, EstadoReporte nuevoEstado)
        {
            var estadoAnterior = Estado?.Nombre;
            
            EstadoID = nuevoEstadoID;
            Estado = nuevoEstado;
            FechaActualizacion = DateTime.Now;

            // ✅ Notificar a todos los observadores
            NotificarObservadores(estadoAnterior);
        }

        private void NotificarObservadores(string? estadoAnterior)
        {
            foreach (var observador in _observadores)
            {
                observador.Actualizar(this, estadoAnterior);
            }
        }
    }
}
