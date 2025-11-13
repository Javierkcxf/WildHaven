
using System; 

namespace webapicsharp.Servicios.Abstracciones
{

    public interface IProveedorConexion
    {

        string ProveedorActual { get; }

        string ObtenerCadenaConexion();
    }
}
