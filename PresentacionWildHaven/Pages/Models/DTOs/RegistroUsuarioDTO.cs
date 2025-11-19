using System.Text.Json.Serialization;

namespace PresentacionWildHaven.Models.DTOs
{
    public class RegistroUsuarioDTO
    {
        [JsonIgnore]
        [JsonPropertyName("usuarioID")]
        public int UsuarioID { get; set; }
        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = "";

        [JsonPropertyName("email")]
        public string Email { get; set; } = "";

        [JsonPropertyName("passwordHash")]
        public string PasswordHash { get; set; } = "";

        [JsonPropertyName("rolID")]
        public int RolID { get; set; } = 1; // Rol de usuario normal por defecto

        [JsonPropertyName("telefono")]
        public string? Telefono { get; set; }

        [JsonPropertyName("activo")]
        public bool Activo { get; set; } = true;
    }
}