using PresentacionWildHaven.Interface;
using PresentacionWildHaven.Models;

namespace PresentacionWildHaven.Implementacion
{
    public class ReporteFactorySelector
    {
        private readonly IReporteFactory _reporteUsuarioFactory;
        private readonly IReporteFactory _reporteAnonimoFactory;

        public ReporteFactorySelector()
        {
            _reporteUsuarioFactory = new ReporteUsuarioFactory();
            _reporteAnonimoFactory = new ReporteAnonimoFactory();
        }

        public IReporteFactory SeleccionarFactory(Reporte data)
        {
            if (data.UsuarioID.HasValue && data.UsuarioID > 0)
            {
                return _reporteUsuarioFactory;
            }

            return _reporteAnonimoFactory;
        }
    }

}