using System;
using System.Text.Json.Serialization;

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
    }


}
