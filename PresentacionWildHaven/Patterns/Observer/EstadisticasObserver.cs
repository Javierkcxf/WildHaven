using PresentacionWildHaven.Models;

namespace PresentacionWildHaven.Patterns.Observer
{
    public class EstadisticasObserver : IObserver
    {
        // Estadísticas en memoria (en producción podrías usar StateContainer)
        private Dictionary<string, int> _contadorPorEstado = new();

        public void Actualizar(Reporte reporte, string? estadoAnterior = null)
        {
            var estadoNuevo = reporte.Estado?.Nombre ?? "Desconocido";

            // Decrementar contador del estado anterior
            if (!string.IsNullOrEmpty(estadoAnterior))
            {
                if (_contadorPorEstado.ContainsKey(estadoAnterior))
                {
                    _contadorPorEstado[estadoAnterior]--;
                }
            }

            // Incrementar contador del estado nuevo
            if (!_contadorPorEstado.ContainsKey(estadoNuevo))
            {
                _contadorPorEstado[estadoNuevo] = 0;
            }
            _contadorPorEstado[estadoNuevo]++;

            Console.WriteLine($"📈 [Estadísticas actualizadas]");
            foreach (var kvp in _contadorPorEstado)
            {
                Console.WriteLine($"   {kvp.Key}: {kvp.Value}");
            }
        }

        public Dictionary<string, int> ObtenerEstadisticas()
        {
            return new Dictionary<string, int>(_contadorPorEstado);
        }
    }
}