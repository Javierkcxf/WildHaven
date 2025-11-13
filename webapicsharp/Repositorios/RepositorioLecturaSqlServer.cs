
using System;                                          
using System.Collections.Generic;                      
using System.Threading.Tasks;                          
using Microsoft.Data.SqlClient;                       
using webapicsharp.Repositorios.Abstracciones;        
using webapicsharp.Servicios.Abstracciones;           
using webapicsharp.Servicios.Utilidades;

namespace webapicsharp.Repositorios
{

    public class RepositorioLecturaSqlServer : IRepositorioLecturaTabla
    {

        private readonly IProveedorConexion _proveedorConexion;

        public RepositorioLecturaSqlServer(IProveedorConexion proveedorConexion)
        {

            _proveedorConexion = proveedorConexion ?? throw new ArgumentNullException(
                nameof(proveedorConexion),
                "IProveedorConexion no puede ser null. Verificar registro de servicios en Program.cs."
            );
        }

        public async Task<IReadOnlyList<Dictionary<string, object?>>> ObtenerFilasAsync(
            string nombreTabla,    
            string? esquema,       
            int? limite           
        )
        {

            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException(
                    "El nombre de la tabla no puede estar vacío.",
                    nameof(nombreTabla)
                );

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "dbo" : esquema.Trim();

            int limiteFinal = limite ?? 1000;

            string consultaSql = $"SELECT TOP ({limiteFinal}) * FROM [{esquemaFinal}].[{nombreTabla}]";

            var resultados = new List<Dictionary<string, object?>>();

            try
            {

                string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();

                using var conexion = new SqlConnection(cadenaConexion);

                await conexion.OpenAsync();

                using var comando = new SqlCommand(consultaSql, conexion);

                using var lector = await comando.ExecuteReaderAsync();

                while (await lector.ReadAsync())
                {

                    var fila = new Dictionary<string, object?>();

                    for (int indiceColumna = 0; indiceColumna < lector.FieldCount; indiceColumna++)
                    {

                        string nombreColumna = lector.GetName(indiceColumna);

                        object? valorColumna = lector.IsDBNull(indiceColumna)
                            ? null                          
                            : lector.GetValue(indiceColumna); 

                        fila[nombreColumna] = valorColumna;
                    }

                    resultados.Add(fila);
                }

            }
            catch (SqlException excepcionSql)
            {

                if (excepcionSql.Number == 208 && !string.IsNullOrWhiteSpace(esquema) && !esquema.Equals("dbo", StringComparison.OrdinalIgnoreCase))
                {

                    try
                    {

                        return await ObtenerFilasAsync(nombreTabla, "dbo", limite);
                    }
                    catch
                    {

                        throw new InvalidOperationException(
                            $"Error SQL: La tabla '{esquema}.{nombreTabla}' no existe en el esquema especificado. " +
                            $"Se intentó automáticamente con esquema por defecto 'dbo.{nombreTabla}' pero tampoco existe. " +
                            $"Verificar que la tabla '{nombreTabla}' existe en la base de datos y en qué esquema está ubicada.",
                            excepcionSql  
                        );
                    }
                }

                throw new InvalidOperationException(
                    $"Error de SQL Server al consultar la tabla '{esquemaFinal}.{nombreTabla}': {excepcionSql.Message}. " +
                    $"Código de error SQL Server: {excepcionSql.Number}. " +
                    $"Verificar que la tabla existe y se tienen permisos de lectura.",
                    excepcionSql  
                );
            }
            catch (Exception excepcionGeneral)
            {

                throw new InvalidOperationException(
                    $"Error inesperado al acceder a SQL Server para tabla '{esquemaFinal}.{nombreTabla}': {excepcionGeneral.Message}. " +
                    $"Verificar conectividad y configuración del servidor.",
                    excepcionGeneral  
                );
            }

            return resultados;
        }

        public async Task<IReadOnlyList<Dictionary<string, object?>>> ObtenerPorClaveAsync(
            string nombreTabla,     
            string? esquema,        
            string nombreClave,     
            string valor           
        )
        {

            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));

            if (string.IsNullOrWhiteSpace(nombreClave))
                throw new ArgumentException("El nombre de la clave no puede estar vacío.", nameof(nombreClave));

            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException("El valor no puede estar vacío.", nameof(valor));

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "dbo" : esquema.Trim();

            string consultaSql = $"SELECT * FROM [{esquemaFinal}].[{nombreTabla}] WHERE [{nombreClave}] = @valor";

            var resultados = new List<Dictionary<string, object?>>();

            try
            {

                string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();

                using var conexion = new SqlConnection(cadenaConexion);
                await conexion.OpenAsync();

                using var comando = new SqlCommand(consultaSql, conexion);

                comando.Parameters.AddWithValue("@valor", valor);

                using var lector = await comando.ExecuteReaderAsync();

                while (await lector.ReadAsync())
                {

                    var fila = new Dictionary<string, object?>();

                    for (int indiceColumna = 0; indiceColumna < lector.FieldCount; indiceColumna++)
                    {

                        string nombreColumna = lector.GetName(indiceColumna);

                        object? valorColumna = lector.IsDBNull(indiceColumna)
                            ? null
                            : lector.GetValue(indiceColumna);

                        fila[nombreColumna] = valorColumna;
                    }

                    resultados.Add(fila);
                }

            }
            catch (SqlException excepcionSql)
            {

                throw new InvalidOperationException(
                    $"Error SQL al filtrar tabla '{esquemaFinal}.{nombreTabla}' por columna '{nombreClave}' con valor '{valor}': {excepcionSql.Message}. " +
                    $"Verificar que la columna existe y el tipo de dato es compatible.",
                    excepcionSql
                );
            }
            catch (Exception excepcionGeneral)
            {

                throw new InvalidOperationException(
                    $"Error inesperado al filtrar tabla '{esquemaFinal}.{nombreTabla}' por {nombreClave}='{valor}': {excepcionGeneral.Message}",
                    excepcionGeneral
                );
            }

            return resultados;
        }

        public async Task<bool> CrearAsync(
            string nombreTabla,
            string? esquema,
            Dictionary<string, object?> datos,
            string? camposEncriptar = null
        )
        {

            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));

            if (datos == null || !datos.Any())
                throw new ArgumentException("Los datos no pueden estar vacíos.", nameof(datos));

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "dbo" : esquema.Trim();

            var datosFinales = new Dictionary<string, object?>(datos);

            if (!string.IsNullOrWhiteSpace(camposEncriptar))
            {

                var camposAEncriptar = camposEncriptar.Split(',')
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var campo in camposAEncriptar)
                {
                    if (datosFinales.ContainsKey(campo) && datosFinales[campo] != null)
                    {
                        string valorOriginal = datosFinales[campo]?.ToString() ?? "";

                        datosFinales[campo] = webapicsharp.Servicios.Utilidades.EncriptacionBCrypt.Encriptar(valorOriginal);
                    }
                }
            }

            var columnas = string.Join(", ", datosFinales.Keys.Select(k => $"[{k}]"));
            var parametros = string.Join(", ", datosFinales.Keys.Select(k => $"@{k}"));

            string consultaSql = $"INSERT INTO [{esquemaFinal}].[{nombreTabla}] ({columnas}) VALUES ({parametros})";

            try
            {

                string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();

                using var conexion = new SqlConnection(cadenaConexion);
                await conexion.OpenAsync();

                using var comando = new SqlCommand(consultaSql, conexion);

                foreach (var kvp in datosFinales)
                {

                    comando.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value ?? DBNull.Value);
                }

                int filasAfectadas = await comando.ExecuteNonQueryAsync();
                return filasAfectadas > 0;
            }
            catch (SqlException excepcionSql)
            {

                throw new InvalidOperationException(
                    $"Error SQL al insertar en tabla '{esquemaFinal}.{nombreTabla}': {excepcionSql.Message}. " +
                    $"Código de error: {excepcionSql.Number}. " +
                    $"Verificar que la tabla existe, las columnas son correctas y no hay violaciones de restricciones.",
                    excepcionSql
                );
            }
            catch (Exception excepcionGeneral)
            {
                throw new InvalidOperationException(
                    $"Error inesperado al insertar en '{esquemaFinal}.{nombreTabla}': {excepcionGeneral.Message}",
                    excepcionGeneral
                );
            }
        }

        public async Task<int> ActualizarAsync(
            string nombreTabla,
            string? esquema,
            string nombreClave,
            string valorClave,
            Dictionary<string, object?> datos,
            string? camposEncriptar = null
        )
        {

            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));

            if (string.IsNullOrWhiteSpace(nombreClave))
                throw new ArgumentException("El nombre de la clave no puede estar vacío.", nameof(nombreClave));

            if (string.IsNullOrWhiteSpace(valorClave))
                throw new ArgumentException("El valor de la clave no puede estar vacío.", nameof(valorClave));

            if (datos == null || !datos.Any())
                throw new ArgumentException("Los datos a actualizar no pueden estar vacíos.", nameof(datos));

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "dbo" : esquema.Trim();

            var datosFinales = new Dictionary<string, object?>(datos);

            if (!string.IsNullOrWhiteSpace(camposEncriptar))
            {

                var camposAEncriptar = camposEncriptar.Split(',')
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var campo in camposAEncriptar)
                {
                    if (datosFinales.ContainsKey(campo) && datosFinales[campo] != null)
                    {
                        string valorOriginal = datosFinales[campo]?.ToString() ?? "";

                        datosFinales[campo] = webapicsharp.Servicios.Utilidades.EncriptacionBCrypt.Encriptar(valorOriginal);
                    }
                }
            }

            var clausulaSet = string.Join(", ", datosFinales.Keys.Select(k => $"[{k}] = @{k}"));

            string consultaSql = $"UPDATE [{esquemaFinal}].[{nombreTabla}] SET {clausulaSet} WHERE [{nombreClave}] = @valorClave";

            try
            {

                string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();

                using var conexion = new SqlConnection(cadenaConexion);
                await conexion.OpenAsync();

                using var comando = new SqlCommand(consultaSql, conexion);

                foreach (var kvp in datosFinales)
                {
                    comando.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value ?? DBNull.Value);
                }

                comando.Parameters.AddWithValue("@valorClave", valorClave);

                int filasAfectadas = await comando.ExecuteNonQueryAsync();
                return filasAfectadas;
            }
            catch (SqlException excepcionSql)
            {

                throw new InvalidOperationException(
                    $"Error SQL al actualizar tabla '{esquemaFinal}.{nombreTabla}' WHERE {nombreClave}='{valorClave}': {excepcionSql.Message}. " +
                    $"Código de error: {excepcionSql.Number}. " +
                    $"Verificar que la tabla y columnas existen, y no hay violaciones de restricciones.",
                    excepcionSql
                );
            }
            catch (Exception excepcionGeneral)
            {
                throw new InvalidOperationException(
                    $"Error inesperado al actualizar '{esquemaFinal}.{nombreTabla}' WHERE {nombreClave}='{valorClave}': {excepcionGeneral.Message}",
                    excepcionGeneral
                );
            }
        }

        public async Task<int> EliminarAsync(
            string nombreTabla,
            string? esquema,
            string nombreClave,
            string valorClave
        )
        {

            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));

            if (string.IsNullOrWhiteSpace(nombreClave))
                throw new ArgumentException("El nombre de la clave no puede estar vacío.", nameof(nombreClave));

            if (string.IsNullOrWhiteSpace(valorClave))
                throw new ArgumentException("El valor de la clave no puede estar vacío.", nameof(valorClave));

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "dbo" : esquema.Trim();

            string consultaSql = $"DELETE FROM [{esquemaFinal}].[{nombreTabla}] WHERE [{nombreClave}] = @valorClave";

            try
            {

                string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();

                using var conexion = new SqlConnection(cadenaConexion);
                await conexion.OpenAsync();

                using var comando = new SqlCommand(consultaSql, conexion);

                comando.Parameters.AddWithValue("@valorClave", valorClave);

                int filasEliminadas = await comando.ExecuteNonQueryAsync();
                return filasEliminadas;
            }
            catch (SqlException excepcionSql)
            {

                throw new InvalidOperationException(
                    $"Error SQL al eliminar de tabla '{esquemaFinal}.{nombreTabla}' WHERE {nombreClave}='{valorClave}': {excepcionSql.Message}. " +
                    $"Código de error: {excepcionSql.Number}. " +
                    $"Verificar que la tabla existe y no hay restricciones de clave foránea.",
                    excepcionSql
                );
            }
            catch (Exception excepcionGeneral)
            {
                throw new InvalidOperationException(
                    $"Error inesperado al eliminar de '{esquemaFinal}.{nombreTabla}' WHERE {nombreClave}='{valorClave}': {excepcionGeneral.Message}",
                    excepcionGeneral
                );
            }
        }

        public async Task<string?> ObtenerHashContrasenaAsync(
            string nombreTabla,
            string? esquema,
            string campoUsuario,
            string campoContrasena,
            string valorUsuario
        )
        {

            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));

            if (string.IsNullOrWhiteSpace(campoUsuario))
                throw new ArgumentException("El campo de usuario no puede estar vacío.", nameof(campoUsuario));

            if (string.IsNullOrWhiteSpace(campoContrasena))
                throw new ArgumentException("El campo de contraseña no puede estar vacío.", nameof(campoContrasena));

            if (string.IsNullOrWhiteSpace(valorUsuario))
                throw new ArgumentException("El valor de usuario no puede estar vacío.", nameof(valorUsuario));

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "dbo" : esquema.Trim();

            string consultaSql = $"SELECT [{campoContrasena}] FROM [{esquemaFinal}].[{nombreTabla}] WHERE [{campoUsuario}] = @valorUsuario";

            try
            {

                string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();

                using var conexion = new SqlConnection(cadenaConexion);
                await conexion.OpenAsync();

                using var comando = new SqlCommand(consultaSql, conexion);

                comando.Parameters.AddWithValue("@valorUsuario", valorUsuario);

                var resultado = await comando.ExecuteScalarAsync();

                return resultado?.ToString();
            }
            catch (SqlException excepcionSql)
            {

                throw new InvalidOperationException(
                    $"Error SQL al obtener hash de contraseña de tabla '{esquemaFinal}.{nombreTabla}' WHERE {campoUsuario}='{valorUsuario}': {excepcionSql.Message}. " +
                    $"Código de error: {excepcionSql.Number}",
                    excepcionSql
                );
            }
            catch (Exception excepcionGeneral)
            {
                throw new InvalidOperationException(
                    $"Error inesperado al obtener hash de contraseña de '{esquemaFinal}.{nombreTabla}': {excepcionGeneral.Message}",
                    excepcionGeneral
                );
            }
        }

    }
}
