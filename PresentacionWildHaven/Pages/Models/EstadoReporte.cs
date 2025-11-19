namespace PresentacionWildHaven.Models
{
    public class EstadoReporte
    {
        public int EstadoID { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int Orden { get; set; }
    }
}