using System;
using System.ComponentModel.DataAnnotations;

namespace PresentacionWildHaven.Models
{
    /// <summary>
    /// Modelo que representa un rol de usuario en el sistema.
    /// Corresponde a la tabla Roles en la base de datos.
    /// </summary>
    public class Roles
    {
        /// <summary>
        /// Identificador único del rol.
        /// </summary>
        [Key]
        public int RolID { get; set; }

        /// <summary>
        /// Nombre del rol (ej: "Administrador", "Usuario", "Veterinario").
        /// </summary>
        [Required(ErrorMessage = "El nombre del rol es obligatorio")]
        [StringLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Descripción detallada del rol y sus permisos.
        /// </summary>
        [StringLength(255, ErrorMessage = "La descripción no puede exceder 255 caracteres")]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Fecha de creación del rol.
        /// </summary>
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}