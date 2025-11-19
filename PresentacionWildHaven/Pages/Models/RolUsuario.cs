using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PresentacionWildHaven.Models
{
    /// <summary>
    /// Modelo que representa la relación entre Usuarios y Roles.
    /// Corresponde a la tabla RolUsuario en la base de datos.
    /// </summary>
    public class RolUsuario
    {
        /// <summary>
        /// Email del usuario.
        /// </summary>
        [Required(ErrorMessage = "El email es obligatorio")]
        [StringLength(100)]
        [EmailAddress]
        [JsonPropertyName("email")]
        public string EmailUsuario { get; set; } = string.Empty;

        /// <summary>
        /// ID del rol asociado.
        /// </summary>
        [Required(ErrorMessage = "El ID del rol es obligatorio")]
        [JsonPropertyName("rolid")]
        public int IdRol { get; set; }
    }
}