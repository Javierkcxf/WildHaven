
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Npgsql;                                        
using NpgsqlTypes;                                   
using webapicsharp.Repositorios.Abstracciones;      
using webapicsharp.Servicios.Abstracciones;         
using webapicsharp.Servicios.Utilidades;            

namespace webapicsharp.Repositorios
{

    public sealed class RepositorioLecturaPostgreSQL : IRepositorioLecturaTabla
    {

        private readonly IProveedorConexion _proveedorConexion;

        public RepositorioLecturaPostgreSQL(IProveedorConexion proveedorConexion)
        {
            _proveedorConexion = proveedorConexion ?? throw new ArgumentNullException(nameof(proveedorConexion));
        }

        private async Task<NpgsqlDbType?> DetectarTipoColumnaAsync(string nombreTabla, string esquema, string nombreColumna)
        {

            string sql = @"
                SELECT data_type, udt_name 
                FROM information_schema.columns 
                WHERE table_schema = @esquema 
                AND table_name = @tabla 
                AND column_name = @columna";

            try
            {
                string cadena = _proveedorConexion.ObtenerCadenaConexion();
                await using var conexion = new NpgsqlConnection(cadena);
                await conexion.OpenAsync();

                await using var comando = new NpgsqlCommand(sql, conexion);

                comando.Parameters.AddWithValue("esquema", esquema);
                comando.Parameters.AddWithValue("tabla", nombreTabla);
                comando.Parameters.AddWithValue("columna", nombreColumna);

                await using var lector = await comando.ExecuteReaderAsync();
                if (await lector.ReadAsync())
                {

                    string dataType = lector.GetString("data_type");    
                    string udtName = lector.GetString("udt_name");      

                    return MapearTipoPostgreSQL(dataType, udtName);
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Advertencia: No se pudo detectar tipo de columna {nombreColumna} en {esquema}.{nombreTabla}: {ex.Message}");
            }

            return null; 
        }

        private NpgsqlDbType? MapearTipoPostgreSQL(string dataType, string udtName)
        {

            return dataType.ToLower() switch
            {

                "integer" or "int4" => NpgsqlDbType.Integer,           
                "bigint" or "int8" => NpgsqlDbType.Bigint,             
                "smallint" or "int2" => NpgsqlDbType.Smallint,         

                "numeric" or "decimal" => NpgsqlDbType.Numeric,        
                "real" or "float4" => NpgsqlDbType.Real,               
                "double precision" or "float8" => NpgsqlDbType.Double, 

                "character varying" or "varchar" => NpgsqlDbType.Varchar, 
                "character" or "char" => NpgsqlDbType.Char,               
                "text" => NpgsqlDbType.Text,                              

                "boolean" or "bool" => NpgsqlDbType.Boolean,              
                "uuid" => NpgsqlDbType.Uuid,                              

                "timestamp without time zone" => NpgsqlDbType.Timestamp,    
                "timestamp with time zone" => NpgsqlDbType.TimestampTz,     
                "date" => NpgsqlDbType.Date,                                 
                "time" => NpgsqlDbType.Time,                                 

                "json" => NpgsqlDbType.Json,                                 
                "jsonb" => NpgsqlDbType.Jsonb,                               

                _ => null 
            };
        }

            private object ConvertirValor(string valor, NpgsqlDbType? tipoDestino)
            {

                if (tipoDestino == null) return valor;

                try
                {

                    return tipoDestino switch
                    {

                        NpgsqlDbType.Integer => int.Parse(valor),           
                        NpgsqlDbType.Bigint => long.Parse(valor),           
                        NpgsqlDbType.Smallint => short.Parse(valor),        
                        NpgsqlDbType.Numeric => decimal.Parse(valor),       
                        NpgsqlDbType.Real => float.Parse(valor),            
                        NpgsqlDbType.Double => double.Parse(valor),         

                        NpgsqlDbType.Boolean => bool.Parse(valor),          

                        NpgsqlDbType.Uuid => Guid.Parse(valor),             

                        NpgsqlDbType.Timestamp => ConvertirTimestamp(valor),
                        NpgsqlDbType.TimestampTz => ConvertirTimestampTz(valor),
                        NpgsqlDbType.Date => ConvertirFecha(valor),
                        NpgsqlDbType.Time => ConvertirHora(valor),          

                        NpgsqlDbType.Varchar => valor,
                        NpgsqlDbType.Char => valor,
                        NpgsqlDbType.Text => valor,
                        NpgsqlDbType.Json => valor,
                        NpgsqlDbType.Jsonb => valor,

                        _ => valor
                    };
                }
                catch
                {

                    return valor;
                }
            }
            private DateTime ConvertirTimestamp(string valor)
    {
        if (DateTime.TryParse(valor, out DateTime resultado))
            return resultado;

        throw new FormatException($"No se pudo convertir '{valor}' a DateTime");
    }

        private DateTime ConvertirTimestampTz(string valor)
        {
            if (DateTime.TryParse(valor, out DateTime resultado))
                return DateTime.SpecifyKind(resultado, DateTimeKind.Utc);

            throw new FormatException($"No se pudo convertir '{valor}' a DateTime con timezone");
        }

        private DateOnly ConvertirFecha(string valor)
        {
            if (DateOnly.TryParse(valor, out DateOnly resultado))
                return resultado;

            throw new FormatException($"No se pudo convertir '{valor}' a DateOnly");
        }

        private TimeOnly ConvertirHora(string valor)
        {
            if (TimeOnly.TryParse(valor, out TimeOnly resultado))
                return resultado;

            throw new FormatException($"No se pudo convertir '{valor}' a TimeOnly");
        }

        public async Task<IReadOnlyList<Dictionary<string, object?>>> ObtenerFilasAsync(
            string nombreTabla,
            string? esquema,
            int? limite
        )
        {

            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "public" : esquema.Trim();
            int limiteFinal = limite ?? 1000;

            string sql = $"SELECT * FROM \"{esquemaFinal}\".\"{nombreTabla}\" LIMIT @limite";
            var filas = new List<Dictionary<string, object?>>();

            try
            {

                string cadena = _proveedorConexion.ObtenerCadenaConexion();

                await using var conexion = new NpgsqlConnection(cadena);
                await conexion.OpenAsync();

                await using var comando = new NpgsqlCommand(sql, conexion);
                comando.Parameters.AddWithValue("limite", limiteFinal); 

                await using var lector = await comando.ExecuteReaderAsync();
                while (await lector.ReadAsync())
                {
                    var fila = new Dictionary<string, object?>();
                    for (int i = 0; i < lector.FieldCount; i++)
                    {
                        string nombreColumna = lector.GetName(i);

                        object? valor = lector.IsDBNull(i) ? null : lector.GetValue(i);
                        fila[nombreColumna] = valor;
                    }
                    filas.Add(fila);
                }
            }
            catch (NpgsqlException ex)
            {

                throw new InvalidOperationException(
                    $"Error PostgreSQL al consultar tabla '{esquemaFinal}.{nombreTabla}': {ex.Message}",
                    ex);
            }

            return filas;
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

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "public" : esquema.Trim();
            var filas = new List<Dictionary<string, object?>>();

            try
            {

                var tipoColumna = await DetectarTipoColumnaAsync(nombreTabla, esquemaFinal, nombreClave);

                object valorConvertido = ConvertirValor(valor, tipoColumna);

                string sql = $"SELECT * FROM \"{esquemaFinal}\".\"{nombreTabla}\" WHERE \"{nombreClave}\" = @valor";
                string cadena = _proveedorConexion.ObtenerCadenaConexion();

                await using var conexion = new NpgsqlConnection(cadena);
                await conexion.OpenAsync();

                await using var comando = new NpgsqlCommand(sql, conexion);

                if (tipoColumna.HasValue)
                {

                    var parametro = new NpgsqlParameter("valor", tipoColumna.Value) { Value = valorConvertido };
                    comando.Parameters.Add(parametro);
                }
                else
                {

                    comando.Parameters.AddWithValue("valor", valor);
                }

                await using var lector = await comando.ExecuteReaderAsync();
                while (await lector.ReadAsync())
                {
                    var fila = new Dictionary<string, object?>();
                    for (int i = 0; i < lector.FieldCount; i++)
                    {
                        string nombreColumna = lector.GetName(i);
                        object? valorColumna = lector.IsDBNull(i) ? null : lector.GetValue(i);
                        fila[nombreColumna] = valorColumna;
                    }
                    filas.Add(fila);
                }
            }
            catch (NpgsqlException ex)
            {
                throw new InvalidOperationException(
                    $"Error PostgreSQL al filtrar tabla '{esquemaFinal}.{nombreTabla}' por {nombreClave}='{valor}': {ex.Message}",
                    ex);
            }

            return filas;
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

                string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "public" : esquema.Trim();

                try
                {
                    string cadena = _proveedorConexion.ObtenerCadenaConexion();
                    await using var conexion = new NpgsqlConnection(cadena);
                    await conexion.OpenAsync();

                    var columnasAutoIncrement = await ObtenerColumnasAutoIncrementalesAsync(
                        conexion, 
                        esquemaFinal, 
                        nombreTabla
                    );

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
                                datosFinales[campo] = EncriptacionBCrypt.Encriptar(valorOriginal);
                            }
                        }
                    }

                    foreach (var columnaAuto in columnasAutoIncrement)
                    {
                        if (datosFinales.ContainsKey(columnaAuto))
                        {
                            datosFinales.Remove(columnaAuto);
                        }
                    }

                    if (!datosFinales.Any())
                    {
                        throw new InvalidOperationException(
                            "No hay columnas válidas para insertar después de excluir columnas auto-incrementales."
                        );
                    }

                    var tiposColumnas = new Dictionary<string, NpgsqlDbType?>();
                    foreach (var columna in datosFinales.Keys)
                    {
                        var tipo = await DetectarTipoColumnaAsync(nombreTabla, esquemaFinal, columna);
                        tiposColumnas[columna] = tipo;
                    }

                    var columnas = string.Join(", ", datosFinales.Keys.Select(k => $"\"{k}\""));
                    var parametros = string.Join(", ", datosFinales.Keys.Select(k => $"@{k}"));
                    string sql = $"INSERT INTO \"{esquemaFinal}\".\"{nombreTabla}\" ({columnas}) VALUES ({parametros})";

                    await using var comando = new NpgsqlCommand(sql, conexion);

                    foreach (var kvp in datosFinales)
                    {
                        string nombreColumna = kvp.Key;
                        object? valor = kvp.Value;

                        if (tiposColumnas.TryGetValue(nombreColumna, out var tipoColumna) && tipoColumna.HasValue)
                        {

                            object valorConvertido = valor!;
                            if (valor is string valorString)
                            {
                                valorConvertido = ConvertirValor(valorString, tipoColumna);
                            }

                            var parametro = new NpgsqlParameter(nombreColumna, tipoColumna.Value) 
                            { 
                                Value = valorConvertido ?? DBNull.Value 
                            };
                            comando.Parameters.Add(parametro);
                        }
                        else
                        {

                            comando.Parameters.AddWithValue(nombreColumna, valor ?? DBNull.Value);
                        }
                    }

                    int filasAfectadas = await comando.ExecuteNonQueryAsync();
                    return filasAfectadas > 0;
                }
                catch (NpgsqlException ex)
                {
                    throw new InvalidOperationException(
                        $"Error PostgreSQL al insertar en tabla '{esquemaFinal}.{nombreTabla}': {ex.Message}",
                        ex);
                }
            }

            private async Task<HashSet<string>> ObtenerColumnasAutoIncrementalesAsync(
                NpgsqlConnection conexion,
                string esquema,
                string tabla
            )
            {
                var columnasAuto = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                string sqlDeteccion = @"
                    SELECT c.column_name
                    FROM information_schema.columns c
                    WHERE c.table_schema = @esquema
                    AND c.table_name = @tabla
                    AND (
                        -- Detectar columnas con SERIAL (tienen default nextval)
                        c.column_default LIKE 'nextval%'
                        -- Detectar columnas IDENTITY (PostgreSQL 10+)
                        OR c.is_identity = 'YES'
                    )";

                await using var cmd = new NpgsqlCommand(sqlDeteccion, conexion);
                cmd.Parameters.AddWithValue("esquema", esquema);
                cmd.Parameters.AddWithValue("tabla", tabla);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    columnasAuto.Add(reader.GetString(0));
                }

                return columnasAuto;
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

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "public" : esquema.Trim();

            try
            {
                string cadena = _proveedorConexion.ObtenerCadenaConexion();
                await using var conexion = new NpgsqlConnection(cadena);
                await conexion.OpenAsync();

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
                            datosFinales[campo] = EncriptacionBCrypt.Encriptar(valorOriginal);
                        }
                    }
                }

                var tiposColumnas = new Dictionary<string, NpgsqlDbType?>();
                foreach (var columna in datosFinales.Keys)
                {
                    var tipo = await DetectarTipoColumnaAsync(nombreTabla, esquemaFinal, columna);
                    tiposColumnas[columna] = tipo;
                }

                var tipoClave = await DetectarTipoColumnaAsync(nombreTabla, esquemaFinal, nombreClave);
                object valorClaveConvertido = ConvertirValor(valorClave, tipoClave);

                var clausulaSet = string.Join(", ", datosFinales.Keys.Select(k => $"\"{k}\" = @{k}"));
                string sql = $"UPDATE \"{esquemaFinal}\".\"{nombreTabla}\" SET {clausulaSet} WHERE \"{nombreClave}\" = @valorClave";

                await using var comando = new NpgsqlCommand(sql, conexion);

                foreach (var kvp in datosFinales)
                {
                    string nombreColumna = kvp.Key;
                    object? valor = kvp.Value;

                    if (tiposColumnas.TryGetValue(nombreColumna, out var tipoColumna) && tipoColumna.HasValue)
                    {

                        object valorConvertido = valor!;
                        if (valor is string valorString)
                        {
                            valorConvertido = ConvertirValor(valorString, tipoColumna);
                        }

                        var parametro = new NpgsqlParameter(nombreColumna, tipoColumna.Value) 
                        { 
                            Value = valorConvertido ?? DBNull.Value 
                        };
                        comando.Parameters.Add(parametro);
                    }
                    else
                    {

                        comando.Parameters.AddWithValue(nombreColumna, valor ?? DBNull.Value);
                    }
                }

                if (tipoClave.HasValue)
                {
                    var parametroClave = new NpgsqlParameter("valorClave", tipoClave.Value) 
                    { 
                        Value = valorClaveConvertido 
                    };
                    comando.Parameters.Add(parametroClave);
                }
                else
                {
                    comando.Parameters.AddWithValue("valorClave", valorClave);
                }

                int filasAfectadas = await comando.ExecuteNonQueryAsync();
                return filasAfectadas; 
            }
            catch (NpgsqlException ex)
            {
                throw new InvalidOperationException(
                    $"Error PostgreSQL al actualizar tabla '{esquemaFinal}.{nombreTabla}' WHERE {nombreClave}='{valorClave}': {ex.Message}",
                    ex);
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

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "public" : esquema.Trim();

            try
            {

                var tipoColumna = await DetectarTipoColumnaAsync(nombreTabla, esquemaFinal, nombreClave);
                object valorConvertido = ConvertirValor(valorClave, tipoColumna);

                string sql = $"DELETE FROM \"{esquemaFinal}\".\"{nombreTabla}\" WHERE \"{nombreClave}\" = @valorClave";

                string cadena = _proveedorConexion.ObtenerCadenaConexion();

                await using var conexion = new NpgsqlConnection(cadena);
                await conexion.OpenAsync();

                await using var comando = new NpgsqlCommand(sql, conexion);

                if (tipoColumna.HasValue)
                {
                    var parametro = new NpgsqlParameter("valorClave", tipoColumna.Value) { Value = valorConvertido };
                    comando.Parameters.Add(parametro);
                }
                else
                {
                    comando.Parameters.AddWithValue("valorClave", valorClave);
                }

                int filasEliminadas = await comando.ExecuteNonQueryAsync();
                return filasEliminadas; 
            }
            catch (NpgsqlException ex)
            {
                throw new InvalidOperationException(
                    $"Error PostgreSQL al eliminar de tabla '{esquemaFinal}.{nombreTabla}' WHERE {nombreClave}='{valorClave}': {ex.Message}",
                    ex);
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

            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "public" : esquema.Trim();

            try
            {

                var tipoColumna = await DetectarTipoColumnaAsync(nombreTabla, esquemaFinal, campoUsuario);
                object valorConvertido = ConvertirValor(valorUsuario, tipoColumna);

                string sql = $"SELECT \"{campoContrasena}\" FROM \"{esquemaFinal}\".\"{nombreTabla}\" WHERE \"{campoUsuario}\" = @valorUsuario";

                string cadena = _proveedorConexion.ObtenerCadenaConexion();

                await using var conexion = new NpgsqlConnection(cadena);
                await conexion.OpenAsync();

                await using var comando = new NpgsqlCommand(sql, conexion);

                if (tipoColumna.HasValue)
                {
                    var parametro = new NpgsqlParameter("valorUsuario", tipoColumna.Value) { Value = valorConvertido };
                    comando.Parameters.Add(parametro);
                }
                else
                {
                    comando.Parameters.AddWithValue("valorUsuario", valorUsuario);
                }

                var resultado = await comando.ExecuteScalarAsync();
                return resultado?.ToString(); 
            }
            catch (NpgsqlException ex)
            {
                throw new InvalidOperationException(
                    $"Error PostgreSQL al obtener hash de contraseña de tabla '{esquemaFinal}.{nombreTabla}': {ex.Message}",
                    ex);
            }
        }
    }
}
