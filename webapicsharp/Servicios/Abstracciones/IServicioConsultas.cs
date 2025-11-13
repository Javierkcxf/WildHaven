
using System.Collections.Generic;   
using System.Threading.Tasks;       
using Microsoft.Data.SqlClient;     
using System.Data;                  

namespace webapicsharp.Servicios.Abstracciones
{

    public interface IServicioConsultas
    {

        (bool esValida, string? mensajeError) ValidarConsultaSQL(string consulta, string[] tablasProhibidas);

        Task<DataTable> EjecutarConsultaParametrizadaAsync(
            string consulta,
            List<SqlParameter> parametros,
            int maximoRegistros,
            string? esquema
        );

        Task<DataTable> EjecutarConsultaParametrizadaDesdeJsonAsync(
            string consulta,
            Dictionary<string, object?>? parametros
        );

        Task<DataTable> EjecutarProcedimientoAlmacenadoAsync(
            string nombreSP,
            Dictionary<string, object?>? parametros,
            List<string>? camposAEncriptar
        );

    }
}
