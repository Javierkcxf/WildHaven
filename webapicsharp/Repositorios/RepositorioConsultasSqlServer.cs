
using System;                                          
using System.Collections.Generic;                      
using System.Threading.Tasks;                          
using System.Data;                                     
using Microsoft.Data.SqlClient;                       
using webapicsharp.Repositorios.Abstracciones;        
using webapicsharp.Servicios.Abstracciones;           

namespace webapicsharp.Repositorios
{

    public class RepositorioConsultasSqlServer : IRepositorioConsultas
    {

        private readonly IProveedorConexion _proveedorConexion;

        public RepositorioConsultasSqlServer(IProveedorConexion proveedorConexion)
        {
            _proveedorConexion = proveedorConexion ?? throw new ArgumentNullException(
                nameof(proveedorConexion),
                "IProveedorConexion no puede ser null. Verificar registro en Program.cs."
            );
        }

        public async Task<DataTable> EjecutarConsultaParametrizadaConDictionaryAsync(
            string consultaSQL,
            Dictionary<string, object?> parametros,
            int maximoRegistros = 10000,
            string? esquema = null
        )
        {

            if (string.IsNullOrWhiteSpace(consultaSQL))
                throw new ArgumentException(
                    "La consulta SQL no puede estar vacía.",
                    nameof(consultaSQL)
                );

            if (maximoRegistros <= 0)
                throw new ArgumentException(
                    "El máximo de registros debe ser mayor a cero.",
                    nameof(maximoRegistros)
                );

            var dataTable = new DataTable();

            try
            {

                string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();

                using var conexion = new SqlConnection(cadenaConexion);
                await conexion.OpenAsync();

                using var comando = new SqlCommand(consultaSQL, conexion);
                comando.CommandTimeout = 30; 

                AgregarParametrosDictionary(comando, parametros ?? new Dictionary<string, object?>());

                using var lector = await comando.ExecuteReaderAsync();
                dataTable.Load(lector); 

                if (dataTable.Rows.Count > maximoRegistros)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Advertencia SQL Server: Consulta retornó {dataTable.Rows.Count} registros, límite esperado era {maximoRegistros}. " +
                        $"Consulta: {TruncarConsultaParaLog(consultaSQL)}"
                    );
                }

                return dataTable;
            }
            catch (SqlException sqlEx)
            {

                string mensajeError = sqlEx.Number switch
                {
                    2 => "Timeout: La consulta tardó demasiado en ejecutarse",
                    207 => "Nombre de columna inválido en la consulta SQL",
                    208 => "Tabla o vista especificada no existe en la base de datos",
                    102 => "Error de sintaxis en la consulta SQL",
                    515 => "Valor null no permitido en columna que no acepta nulls",
                    547 => "Violación de restricción de clave foránea",
                    2812 => "Procedimiento almacenado no encontrado",
                    8152 => "String or binary data would be truncated (datos demasiado largos)",
                    2146 => "Error de conversión de tipos de datos",
                    _ => $"Error SQL Server (Código {sqlEx.Number}): {sqlEx.Message}"
                };

                throw new InvalidOperationException(
                    $"Error al ejecutar consulta SQL: {mensajeError}. Consulta: {TruncarConsultaParaLog(consultaSQL)}",
                    sqlEx
                );
            }
            catch (InvalidOperationException)
            {

                throw;
            }
            catch (Exception ex)
            {

                throw new InvalidOperationException(
                    $"Error inesperado al ejecutar consulta: {ex.Message}. " +
                    $"Consulta: {TruncarConsultaParaLog(consultaSQL)}. " +
                    $"Tipo excepción: {ex.GetType().Name}",
                    ex
                );
            }
        }

        public async Task<(bool esValida, string? mensajeError)> ValidarConsultaConDictionaryAsync(
            string consultaSQL,
            Dictionary<string, object?> parametros
        )
        {
            if (string.IsNullOrWhiteSpace(consultaSQL))
                return (false, "La consulta no puede estar vacía");

            try
            {
                string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();

                using var conexion = new SqlConnection(cadenaConexion);
                await conexion.OpenAsync();

                using var comandoParseOnly = new SqlCommand("SET PARSEONLY ON", conexion);
                await comandoParseOnly.ExecuteNonQueryAsync();

                using var comandoValidacion = new SqlCommand(consultaSQL, conexion);
                comandoValidacion.CommandTimeout = 5; 

                AgregarParametrosDictionary(comandoValidacion, parametros ?? new Dictionary<string, object?>());

                await comandoValidacion.ExecuteNonQueryAsync();

                using var comandoParseOff = new SqlCommand("SET PARSEONLY OFF", conexion);
                await comandoParseOff.ExecuteNonQueryAsync();

                return (true, null);
            }
            catch (SqlException sqlEx)
            {

                string mensajeError = sqlEx.Number switch
                {
                    102 => "Error de sintaxis SQL: revise la estructura de la consulta",
                    207 => "Nombre de columna inválido: verifique que las columnas existan",
                    208 => "Objeto no válido: tabla o vista no existe en la base de datos",
                    156 => "Palabra clave SQL incorrecta o en posición incorrecta",
                    170 => "Error de sintaxis cerca de palabra reservada",
                    _ => $"Error de validación SQL Server (Código {sqlEx.Number}): {sqlEx.Message}"
                };

                return (false, mensajeError);
            }
            catch (Exception ex)
            {
                return (false, $"Error inesperado en validación: {ex.Message}");
            }
        }

        public async Task<DataTable> EjecutarProcedimientoAlmacenadoConDictionaryAsync(
            string nombreSP,
            Dictionary<string, object?> parametros
        )
        {

            if (string.IsNullOrWhiteSpace(nombreSP))
                throw new ArgumentException(
                    "El nombre del procedimiento almacenado no puede estar vacío.",
                    nameof(nombreSP)
                );

            var dataTable = new DataTable();

            try
            {

                string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();

                using var conexion = new SqlConnection(cadenaConexion);
                await conexion.OpenAsync();

                using var comando = new SqlCommand(nombreSP, conexion);
                comando.CommandType = CommandType.StoredProcedure;  
                comando.CommandTimeout = 30; 

                AgregarParametrosDictionary(comando, parametros ?? new Dictionary<string, object?>());

                using var lector = await comando.ExecuteReaderAsync();
                dataTable.Load(lector); 

                return dataTable;
            }
            catch (SqlException sqlEx)
            {

                string mensajeError = sqlEx.Number switch
                {
                    2812 => "Procedimiento almacenado no encontrado: verifique nombre y esquema",
                    201 => "Error en parámetros del procedimiento almacenado: revise nombres y tipos",
                    2 => "Timeout: El procedimiento tardó demasiado en ejecutarse",
                    8144 => "Demasiados parámetros especificados para el procedimiento",
                    8145 => "Parámetro requerido no especificado para el procedimiento",
                    _ => $"Error SQL Server en procedimiento almacenado (Código {sqlEx.Number}): {sqlEx.Message}"
                };

                throw new InvalidOperationException(
                    $"Error al ejecutar procedimiento almacenado '{nombreSP}': {mensajeError}",
                    sqlEx
                );
            }
            catch (Exception ex)
            {

                throw new InvalidOperationException(
                    $"Error inesperado al ejecutar procedimiento almacenado '{nombreSP}': {ex.Message}. " +
                    $"Tipo excepción: {ex.GetType().Name}",
                    ex
                );
            }
        }

        public async Task<DataTable> EjecutarConsultaParametrizadaAsync(
            string consultaSQL,
            List<SqlParameter>? parametros
        )
        {

            if (string.IsNullOrWhiteSpace(consultaSQL))
                throw new ArgumentException(
                    "La consulta SQL no puede estar vacía.",
                    nameof(consultaSQL)
                );

            var dataTable = new DataTable();

            try
            {

                string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();

                using var conexion = new SqlConnection(cadenaConexion);
                await conexion.OpenAsync();

                using var comando = new SqlCommand(consultaSQL, conexion);
                comando.CommandTimeout = 30;

                if (parametros != null && parametros.Count > 0)
                {
                    foreach (var parametro in parametros)
                    {
                        if (string.IsNullOrWhiteSpace(parametro.ParameterName))
                            throw new ArgumentException("Parámetro con nombre vacío encontrado");

                        var parametroClonado = new SqlParameter
                        {
                            ParameterName = parametro.ParameterName,
                            Value = parametro.Value ?? DBNull.Value,
                            SqlDbType = parametro.SqlDbType,
                            Size = parametro.Size,
                            Precision = parametro.Precision,
                            Scale = parametro.Scale
                        };

                        comando.Parameters.Add(parametroClonado);
                    }
                }

                using var lector = await comando.ExecuteReaderAsync();
                dataTable.Load(lector);

                return dataTable;
            }
            catch (SqlException sqlEx)
            {

                string mensajeError = sqlEx.Number switch
                {
                    2 => "Timeout: La consulta tardó demasiado en ejecutarse",
                    207 => "Nombre de columna inválido en la consulta SQL",
                    208 => "Tabla o vista especificada no existe en la base de datos",
                    102 => "Error de sintaxis en la consulta SQL",
                    515 => "Valor null no permitido en columna que no acepta nulls",
                    547 => "Violación de restricción de clave foránea",
                    2812 => "Procedimiento almacenado no encontrado",
                    _ => $"Error SQL Server (Código {sqlEx.Number}): {sqlEx.Message}"
                };

                throw new InvalidOperationException(
                    $"Error al ejecutar consulta SQL: {mensajeError}",
                    sqlEx
                );
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error inesperado al ejecutar consulta: {ex.Message}",
                    ex
                );
            }
        }

        public async Task<(bool esValida, string? mensajeError)> ValidarConsultaAsync(
            string consultaSQL,
            List<SqlParameter>? parametros
        )
        {
            if (string.IsNullOrWhiteSpace(consultaSQL))
                return (false, "La consulta no puede estar vacía");

            try
            {
                string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();

                using var conexion = new SqlConnection(cadenaConexion);
                await conexion.OpenAsync();

                using var comandoParseOnly = new SqlCommand("SET PARSEONLY ON", conexion);
                await comandoParseOnly.ExecuteNonQueryAsync();

                using var comandoValidacion = new SqlCommand(consultaSQL, conexion);
                comandoValidacion.CommandTimeout = 5;

                if (parametros != null)
                {
                    foreach (var parametro in parametros)
                    {
                        comandoValidacion.Parameters.Add(new SqlParameter
                        {
                            ParameterName = parametro.ParameterName,
                            SqlDbType = parametro.SqlDbType,
                            Value = DBNull.Value
                        });
                    }
                }

                await comandoValidacion.ExecuteNonQueryAsync();

                using var comandoParseOff = new SqlCommand("SET PARSEONLY OFF", conexion);
                await comandoParseOff.ExecuteNonQueryAsync();

                return (true, null);
            }
            catch (SqlException sqlEx)
            {
                string mensajeError = sqlEx.Number switch
                {
                    102 => "Error de sintaxis SQL",
                    207 => "Nombre de columna inválido",
                    208 => "Objeto no válido (tabla/vista no existe)",
                    _ => $"Error de validación: {sqlEx.Message}"
                };

                return (false, mensajeError);
            }
            catch (Exception ex)
            {
                return (false, $"Error inesperado en validación: {ex.Message}");
            }
        }

        public async Task<DataTable> EjecutarProcedimientoAlmacenadoAsync(
            string nombreSP,
            List<SqlParameter>? parametros)
        {
            if (string.IsNullOrWhiteSpace(nombreSP))
                throw new ArgumentException(
                    "El nombre del procedimiento almacenado no puede estar vacío.",
                    nameof(nombreSP)
                );

            var dataTable = new DataTable();

            try
            {
                string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();

                using var conexion = new SqlConnection(cadenaConexion);
                await conexion.OpenAsync();

                using var comando = new SqlCommand(nombreSP, conexion);
                comando.CommandType = CommandType.StoredProcedure;
                comando.CommandTimeout = 30;

                if (parametros != null && parametros.Count > 0)
                {
                    foreach (var parametro in parametros)
                    {
                        if (string.IsNullOrWhiteSpace(parametro.ParameterName))
                            throw new ArgumentException("Parámetro con nombre vacío encontrado");

                        var parametroClonado = new SqlParameter
                        {
                            ParameterName = parametro.ParameterName,
                            Value = parametro.Value ?? DBNull.Value,
                            SqlDbType = parametro.SqlDbType,
                            Size = parametro.Size,
                            Precision = parametro.Precision,
                            Scale = parametro.Scale
                        };

                        comando.Parameters.Add(parametroClonado);
                    }
                }

                using var lector = await comando.ExecuteReaderAsync();
                dataTable.Load(lector);

                return dataTable;
            }
            catch (SqlException sqlEx)
            {
                string mensajeError = sqlEx.Number switch
                {
                    2812 => "Procedimiento almacenado no encontrado",
                    201 => "Error en parámetros del procedimiento almacenado",
                    2 => "Timeout: El procedimiento tardó demasiado en ejecutarse",
                    _ => $"Error SQL Server en procedimiento almacenado (Código {sqlEx.Number}): {sqlEx.Message}"
                };

                throw new InvalidOperationException(
                    $"Error al ejecutar procedimiento almacenado '{nombreSP}': {mensajeError}",
                    sqlEx
                );
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error inesperado al ejecutar procedimiento almacenado '{nombreSP}': {ex.Message}",
                    ex
                );
            }
        }

        public async Task<string?> ObtenerEsquemaTablaAsync(string nombreTabla, string esquemaPredeterminado)
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));

            try
            {
                string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();

                using var conexion = new SqlConnection(cadenaConexion);
                await conexion.OpenAsync();

                string consultaSql = @"
                    SELECT TOP 1 TABLE_SCHEMA 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = @nombreTabla 
                    ORDER BY 
                        CASE WHEN TABLE_SCHEMA = @esquema THEN 0 ELSE 1 END, 
                        TABLE_SCHEMA";

                using var comando = new SqlCommand(consultaSql, conexion);
                comando.Parameters.Add(new SqlParameter("@nombreTabla", nombreTabla));
                comando.Parameters.Add(new SqlParameter("@esquema", esquemaPredeterminado));

                var resultado = await comando.ExecuteScalarAsync();
                return resultado?.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error al buscar esquema para tabla '{nombreTabla}': {ex.Message}",
                    ex
                );
            }
        }

        public async Task<DataTable> ObtenerEstructuraTablaAsync(string nombreTabla, string esquema)
        {
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));

            var dataTable = new DataTable();

            try
            {
                string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();

                using var conexion = new SqlConnection(cadenaConexion);
                await conexion.OpenAsync();

                string consultaSql = @"
                    SELECT c.COLUMN_NAME AS Nombre, c.DATA_TYPE AS TipoSql, c.CHARACTER_MAXIMUM_LENGTH AS Longitud,
                        c.IS_NULLABLE AS Nullable, c.COLUMN_DEFAULT AS ValorDefecto,
                        COLUMNPROPERTY(OBJECT_ID(QUOTENAME(c.TABLE_SCHEMA) + '.' + QUOTENAME(c.TABLE_NAME)), c.COLUMN_NAME, 'IsIdentity') AS EsIdentidad,
                        CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS EsPrimaria
                    FROM INFORMATION_SCHEMA.COLUMNS c
                    LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE pk
                        ON pk.TABLE_SCHEMA = c.TABLE_SCHEMA AND pk.TABLE_NAME = c.TABLE_NAME
                        AND pk.COLUMN_NAME = c.COLUMN_NAME
                        AND OBJECTPROPERTY(OBJECT_ID(pk.CONSTRAINT_NAME), 'IsPrimaryKey') = 1
                    WHERE c.TABLE_NAME = @nombreTabla AND c.TABLE_SCHEMA = @esquema
                    ORDER BY c.ORDINAL_POSITION";

                using var comando = new SqlCommand(consultaSql, conexion);
                comando.Parameters.Add(new SqlParameter("@nombreTabla", nombreTabla));
                comando.Parameters.Add(new SqlParameter("@esquema", esquema));

                using var lector = await comando.ExecuteReaderAsync();
                dataTable.Load(lector);

                return dataTable;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error al obtener estructura de tabla '{esquema}.{nombreTabla}': {ex.Message}",
                    ex
                );
            }
        }

        public async Task<DataTable> ObtenerEstructuraBaseDatosAsync(string? nombreBD)
        {
            var dataTable = new DataTable();

            try
            {
                string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();

                using var conexion = new SqlConnection(cadenaConexion);
                await conexion.OpenAsync();

                string consultaSql = @"
                    SELECT 
                        t.TABLE_SCHEMA AS Esquema,
                        t.TABLE_NAME AS Tabla,
                        c.COLUMN_NAME AS Columna,
                        c.DATA_TYPE AS TipoDato,
                        c.CHARACTER_MAXIMUM_LENGTH AS LongitudMaxima,
                        c.IS_NULLABLE AS Nullable,
                        CASE WHEN COLUMNPROPERTY(OBJECT_ID(t.TABLE_SCHEMA + '.' + t.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') = 1 THEN 'SI' ELSE 'NO' END AS Identidad,
                        c.ORDINAL_POSITION AS Posicion
                    FROM INFORMATION_SCHEMA.TABLES t
                    INNER JOIN INFORMATION_SCHEMA.COLUMNS c
                        ON t.TABLE_SCHEMA = c.TABLE_SCHEMA AND t.TABLE_NAME = c.TABLE_NAME
                    WHERE t.TABLE_TYPE = 'BASE TABLE'
                    ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME, c.ORDINAL_POSITION";

                using var comando = new SqlCommand(consultaSql, conexion);
                using var lector = await comando.ExecuteReaderAsync();
                dataTable.Load(lector);

                return dataTable;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error al obtener estructura de base de datos: {ex.Message}",
                    ex
                );
            }
        }

        private static void AgregarParametrosDictionary(SqlCommand comando, Dictionary<string, object?> parametros)
        {
            foreach (var kvp in parametros)
            {

                string nombreParametro = NormalizarNombreParametro(kvp.Key);

                if (string.IsNullOrWhiteSpace(nombreParametro))
                    throw new ArgumentException($"Nombre de parámetro inválido: '{kvp.Key}'");

                var sqlParameter = CrearSqlParameterOptimizado(nombreParametro, kvp.Value);

                comando.Parameters.Add(sqlParameter);
            }
        }

        private static SqlParameter CrearSqlParameterOptimizado(string nombre, object? valor)
        {

            if (valor == null || valor == DBNull.Value)
            {
                return new SqlParameter(nombre, SqlDbType.NVarChar) { Value = DBNull.Value };
            }

            return valor switch
            {

                int intVal => new SqlParameter(nombre, SqlDbType.Int) { Value = intVal },
                long longVal => new SqlParameter(nombre, SqlDbType.BigInt) { Value = longVal },
                short shortVal => new SqlParameter(nombre, SqlDbType.SmallInt) { Value = shortVal },
                byte byteVal => new SqlParameter(nombre, SqlDbType.TinyInt) { Value = byteVal },

                decimal decVal => new SqlParameter(nombre, SqlDbType.Decimal) { Value = decVal },
                double doubleVal => new SqlParameter(nombre, SqlDbType.Float) { Value = doubleVal },
                float floatVal => new SqlParameter(nombre, SqlDbType.Real) { Value = floatVal },

                DateTime dtVal => new SqlParameter(nombre, SqlDbType.DateTime2) { Value = dtVal },
                DateOnly dateVal => new SqlParameter(nombre, SqlDbType.Date) 
                { 
                    Value = dateVal.ToDateTime(TimeOnly.MinValue) 
                },
                TimeOnly timeVal => new SqlParameter(nombre, SqlDbType.Time) 
                { 
                    Value = timeVal.ToTimeSpan() 
                },

                bool boolVal => new SqlParameter(nombre, SqlDbType.Bit) { Value = boolVal },
                Guid guidVal => new SqlParameter(nombre, SqlDbType.UniqueIdentifier) { Value = guidVal },
                byte[] bytesVal => new SqlParameter(nombre, SqlDbType.VarBinary) { Value = bytesVal },

                string strVal => CrearParametroTextoOptimizado(nombre, strVal),
                char charVal => new SqlParameter(nombre, SqlDbType.NChar, 1) { Value = charVal.ToString() },

                _ => new SqlParameter(nombre, SqlDbType.NVarChar) { Value = valor.ToString() ?? "" }
            };
        }

        private static SqlParameter CrearParametroTextoOptimizado(string nombre, string valor)
        {

            if (string.IsNullOrEmpty(valor))
            {
                return new SqlParameter(nombre, SqlDbType.NVarChar, 1) { Value = DBNull.Value };
            }

            return valor.Length switch
            {

                <= 50 => new SqlParameter(nombre, SqlDbType.NVarChar, 50) { Value = valor },
                <= 255 => new SqlParameter(nombre, SqlDbType.NVarChar, 255) { Value = valor },
                <= 4000 => new SqlParameter(nombre, SqlDbType.NVarChar, 4000) { Value = valor },

                _ => new SqlParameter(nombre, SqlDbType.NText) { Value = valor }
            };
        }

        private static string NormalizarNombreParametro(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return "";

            string nombreLimpio = nombre.Trim();

            if (!nombreLimpio.StartsWith("@"))
            {
                nombreLimpio = "@" + nombreLimpio;
            }

            if (nombreLimpio.Length == 1) 
            {
                return "";
            }

            return nombreLimpio;
        }

        private static string TruncarConsultaParaLog(string consulta, int maxLength = 200)
        {
            if (string.IsNullOrEmpty(consulta)) return "[consulta vacía]";

            return consulta.Length > maxLength 
                ? consulta.Substring(0, maxLength) + "..." 
                : consulta;
        }

    }
}
