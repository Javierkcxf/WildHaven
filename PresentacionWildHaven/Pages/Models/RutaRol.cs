using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PresentacionWildHaven.Models
{
    /// <summary>
    /// Modelo que representa las rutas permitidas por rol.
    /// Corresponde a la tabla RutasRol en la base de datos.
    /// </summary>
    public class RutaRol
    {
        /// <summary>
        /// ID de la ruta (si existe en tu BD).
        /// </summary>
        [Key]
        public int? RutaRolID { get; set; }

        /// <summary>
        /// Ruta o URL permitida (ej: "/admin/dashboard", "/mis-reportes").
        /// </summary>
        [Required]
        [StringLength(255)]
        [JsonPropertyName("ruta")]
        public string NombreRuta { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del rol asociado (ej: "Administrador", "Usuario").
        /// </summary>
        [Required]
        [StringLength(50)]
        [JsonPropertyName("rol")]
        public string NombreRol { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de creación del registro.
        /// </summary>
        public DateTime? FechaCreacion { get; set; }
    }
}