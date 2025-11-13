
using System;                                             
using Microsoft.AspNetCore.Authorization;                
using Microsoft.AspNetCore.Mvc;                          
using System.Threading.Tasks;                            
using Microsoft.Extensions.Logging;                      
using Microsoft.Extensions.Configuration;                
using webapicsharp.Servicios.Abstracciones;              
using Microsoft.Data.SqlClient;                     
using System.Text.Json;

namespace webapicsharp.Controllers
{

    [Route("api/{tabla}")]                                
    [ApiController]                                       
    public class EntidadesController : ControllerBase
    {

        private readonly IServicioCrud _servicioCrud;           
        private readonly ILogger<EntidadesController> _logger;  
        private readonly IConfiguration _configuration;         

        public EntidadesController(
            IServicioCrud servicioCrud,           
            ILogger<EntidadesController> logger,  
            IConfiguration configuration         
        )
        {

            _servicioCrud = servicioCrud ?? throw new ArgumentNullException(
                nameof(servicioCrud),
                "IServicioCrud no fue inyectado correctamente. Verificar registro de servicios en Program.cs"
            );

            _logger = logger ?? throw new ArgumentNullException(
                nameof(logger),
                "ILogger no fue inyectado correctamente. Problema en configuración de logging de ASP.NET Core"
            );

            _configuration = configuration ?? throw new ArgumentNullException(
                nameof(configuration),
                "IConfiguration no fue inyectado correctamente. Problema en configuración base de ASP.NET Core"
            );
        }

        [AllowAnonymous]                                  
        [HttpGet]                                        
        public async Task<IActionResult> ListarAsync(
            string tabla,                                 
            [FromQuery] string? esquema,                  
            [FromQuery] int? limite                       
        )
        {
            try
            {

                _logger.LogInformation(
                    "INICIO consulta - Tabla: {Tabla}, Esquema: {Esquema}, Límite: {Limite}",
                    tabla,                                
                    esquema ?? "por defecto",            
                    limite?.ToString() ?? "por defecto"  
                );

                var filas = await _servicioCrud.ListarAsync(tabla, esquema, limite);

                _logger.LogInformation(
                    "RESULTADO exitoso - Registros obtenidos: {Cantidad} de tabla {Tabla}",
                    filas.Count,    
                    tabla          
                );

                if (filas.Count == 0)
                {

                    _logger.LogInformation(
                        "SIN DATOS - Tabla {Tabla} consultada exitosamente pero no contiene registros",
                        tabla
                    );

                    return NoContent();
                }

                return Ok(new
                {

                    tabla = tabla,                              
                    esquema = esquema ?? "por defecto",         
                    limite = limite,                            
                    total = filas.Count,                        

                    datos = filas                               

                });
            }

            catch (ArgumentException excepcionArgumento)
            {

                _logger.LogWarning(
                    "ERROR DE VALIDACIÓN - Petición rechazada - Tabla: {Tabla}, Error: {Mensaje}",
                    tabla,                          
                    excepcionArgumento.Message      
                );

                return BadRequest(new
                {
                    estado = 400,                                    
                    mensaje = "Parámetros de entrada inválidos.",    
                    detalle = excepcionArgumento.Message,            
                    tabla = tabla                                    
                });
            }
            catch (InvalidOperationException excepcionOperacion)
            {

                _logger.LogError(excepcionOperacion,
                    "ERROR DE OPERACIÓN - Fallo en consulta - Tabla: {Tabla}, Error: {Mensaje}",
                    tabla,                              
                    excepcionOperacion.Message          
                );

                return NotFound(new
                {
                    estado = 404,                                      
                    mensaje = "El recurso solicitado no fue encontrado.", 
                    detalle = excepcionOperacion.Message,              
                    tabla = tabla,                                     
                    sugerencia = "Verifique que la tabla y el esquema existan en la base de datos" 
                });
            }
            catch (UnauthorizedAccessException excepcionAcceso)
            {
                _logger.LogWarning(
                    "ACCESO DENEGADO - Tabla restringida: {Tabla}, Error: {Mensaje}",
                    tabla,
                    excepcionAcceso.Message
                );

                return StatusCode(403, new
                {
                    estado = 403,
                    mensaje = "Acceso denegado.",
                    detalle = excepcionAcceso.Message,
                    tabla = tabla
                });
            }
            catch (Exception excepcionGeneral)
            {

                _logger.LogError(excepcionGeneral,
                    "ERROR CRÍTICO - Falla inesperada en consulta - Tabla: {Tabla}",
                    tabla              
                );

                return StatusCode(500, new
                {
                    estado = 500,                                        
                    mensaje = "Error interno del servidor.",             
                    tabla = tabla,                                       
                    detalle = "Contacte al administrador del sistema.", 
                    timestamp = DateTime.UtcNow                          
                });
            }
        }

        [AllowAnonymous]
        [HttpGet("{nombreClave}/{valor}")]
        public async Task<IActionResult> ObtenerPorClaveAsync(
        string tabla,           
        string nombreClave,     
        string valor,           
        [FromQuery] string? esquema = null  
        )
        {
            try
            {

                _logger.LogInformation(
                    "INICIO filtrado - Tabla: {Tabla}, Esquema: {Esquema}, Clave: {Clave}, Valor: {Valor}",
                    tabla, esquema ?? "por defecto", nombreClave, valor
                );

                var filas = await _servicioCrud.ObtenerPorClaveAsync(tabla, esquema, nombreClave, valor);

                _logger.LogInformation(
                    "RESULTADO filtrado - {Cantidad} registros encontrados para {Clave}={Valor} en {Tabla}",
                    filas.Count, nombreClave, valor, tabla
                );

                if (filas.Count == 0)
                {
                    return NotFound(new
                    {
                        estado = 404,
                        mensaje = "No se encontraron registros",
                        detalle = $"No se encontró ningún registro con {nombreClave} = {valor} en la tabla {tabla}",
                        tabla = tabla,
                        esquema = esquema ?? "por defecto",
                        filtro = $"{nombreClave} = {valor}"
                    });
                }

                return Ok(new
                {
                    tabla = tabla,
                    esquema = esquema ?? "por defecto",
                    filtro = $"{nombreClave} = {valor}",
                    total = filas.Count,
                    datos = filas
                });
            }
            catch (UnauthorizedAccessException excepcionAcceso)
            {
                return StatusCode(403, new
                {
                    estado = 403,
                    mensaje = "Acceso denegado.",
                    detalle = excepcionAcceso.Message,
                    tabla = tabla
                });
            }
            catch (ArgumentException excepcionArgumento)
            {
                return BadRequest(new
                {
                    estado = 400,
                    mensaje = "Parámetros inválidos.",
                    detalle = excepcionArgumento.Message,
                    tabla = tabla
                });
            }
            catch (InvalidOperationException excepcionOperacion)
            {
                return NotFound(new
                {
                    estado = 404,
                    mensaje = "Recurso no encontrado.",
                    detalle = excepcionOperacion.Message,
                    tabla = tabla
                });
            }
            catch (Exception excepcionGeneral)
            {
                _logger.LogError(excepcionGeneral,
                    "ERROR CRÍTICO - Falla en filtrado - Tabla: {Tabla}, Clave: {Clave}, Valor: {Valor}",
                    tabla, nombreClave, valor
                );

                return StatusCode(500, new
                {
                    estado = 500,
                    mensaje = "Error interno del servidor.",
                    tabla = tabla,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CrearAsync(
            string tabla,                                           
            [FromBody] Dictionary<string, object?> datosEntidad,   
            [FromQuery] string? esquema = null,                    
            [FromQuery] string? camposEncriptar = null             
        )
        {
            try
            {

                _logger.LogInformation(
                    "INICIO creación - Tabla: {Tabla}, Esquema: {Esquema}, Campos a encriptar: {CamposEncriptar}",
                    tabla, esquema ?? "por defecto", camposEncriptar ?? "ninguno"
                );

                if (datosEntidad == null || !datosEntidad.Any())
                {
                    return BadRequest(new
                    {
                        estado = 400,
                        mensaje = "Los datos de la entidad no pueden estar vacíos.",
                        tabla = tabla
                    });
                }

                var datosConvertidos = new Dictionary<string, object?>();
                foreach (var kvp in datosEntidad)
                {
                    if (kvp.Value is JsonElement elemento)
                    {
                        datosConvertidos[kvp.Key] = ConvertirJsonElement(elemento);
                    }
                    else
                    {
                        datosConvertidos[kvp.Key] = kvp.Value;
                    }
                }

                bool creado = await _servicioCrud.CrearAsync(tabla, esquema, datosConvertidos, camposEncriptar);

                if (creado)
                {
                    _logger.LogInformation(
                        "ÉXITO creación - Registro creado en tabla {Tabla}",
                        tabla
                    );

                    return Ok(new
                    {
                        estado = 200,
                        mensaje = "Registro creado exitosamente.",
                        tabla = tabla,
                        esquema = esquema ?? "por defecto"
                    });
                }
                else
                {
                    return StatusCode(500, new
                    {
                        estado = 500,
                        mensaje = "No se pudo crear el registro.",
                        tabla = tabla
                    });
                }
            }
            catch (UnauthorizedAccessException excepcionAcceso)
            {
                return StatusCode(403, new
                {
                    estado = 403,
                    mensaje = "Acceso denegado.",
                    detalle = excepcionAcceso.Message,
                    tabla = tabla
                });
            }
            catch (ArgumentException excepcionArgumento)
            {
                return BadRequest(new
                {
                    estado = 400,
                    mensaje = "Datos inválidos.",
                    detalle = excepcionArgumento.Message,
                    tabla = tabla
                });
            }
            catch (InvalidOperationException excepcionOperacion)
            {
                return StatusCode(500, new
                {
                    estado = 500,
                    mensaje = "Error en la operación.",
                    detalle = excepcionOperacion.Message,
                    tabla = tabla
                });
            }
            catch (Exception excepcionGeneral)
            {
                _logger.LogError(excepcionGeneral,
                    "ERROR CRÍTICO - Falla en creación - Tabla: {Tabla}",
                    tabla
                );

                return StatusCode(500, new
                {
                    estado = 500,
                    mensaje = "Error interno del servidor.",
                    tabla = tabla,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [AllowAnonymous]
        [HttpPut("{nombreClave}/{valorClave}")]
        public async Task<IActionResult> ActualizarAsync(
            string tabla,                                           
            string nombreClave,                                     
            string valorClave,                                      
            [FromBody] Dictionary<string, object?> datosEntidad,   
            [FromQuery] string? esquema = null,                    
            [FromQuery] string? camposEncriptar = null             
        )
        {
            try
            {

                _logger.LogInformation(
                    "INICIO actualización - Tabla: {Tabla}, Clave: {Clave}={Valor}, Esquema: {Esquema}, Campos a encriptar: {CamposEncriptar}",
                    tabla, nombreClave, valorClave, esquema ?? "por defecto", camposEncriptar ?? "ninguno"
                );

                if (datosEntidad == null || !datosEntidad.Any())
                {
                    return BadRequest(new
                    {
                        estado = 400,
                        mensaje = "Los datos de actualización no pueden estar vacíos.",
                        tabla = tabla,
                        filtro = $"{nombreClave} = {valorClave}"
                    });
                }

                var datosConvertidos = new Dictionary<string, object?>();
                foreach (var kvp in datosEntidad)
                {
                    if (kvp.Value is JsonElement elemento)
                    {
                        datosConvertidos[kvp.Key] = ConvertirJsonElement(elemento);
                    }
                    else
                    {
                        datosConvertidos[kvp.Key] = kvp.Value;
                    }
                }

                int filasAfectadas = await _servicioCrud.ActualizarAsync(
                    tabla, esquema, nombreClave, valorClave, datosConvertidos, camposEncriptar
                );

                if (filasAfectadas > 0)
                {
                    _logger.LogInformation(
                        "ÉXITO actualización - {FilasAfectadas} filas actualizadas en tabla {Tabla} WHERE {Clave}={Valor}",
                        filasAfectadas, tabla, nombreClave, valorClave
                    );

                    return Ok(new
                    {
                        estado = 200,
                        mensaje = "Registro actualizado exitosamente.",
                        tabla = tabla,
                        esquema = esquema ?? "por defecto",
                        filtro = $"{nombreClave} = {valorClave}",
                        filasAfectadas = filasAfectadas,
                        camposEncriptados = camposEncriptar ?? "ninguno"
                    });
                }
                else
                {

                    return NotFound(new
                    {
                        estado = 404,
                        mensaje = "No se encontró el registro a actualizar.",
                        detalle = $"No existe un registro con {nombreClave} = {valorClave} en la tabla {tabla}",
                        tabla = tabla,
                        filtro = $"{nombreClave} = {valorClave}"
                    });
                }
            }
            catch (UnauthorizedAccessException excepcionAcceso)
            {
                return StatusCode(403, new
                {
                    estado = 403,
                    mensaje = "Acceso denegado.",
                    detalle = excepcionAcceso.Message,
                    tabla = tabla
                });
            }
            catch (ArgumentException excepcionArgumento)
            {
                return BadRequest(new
                {
                    estado = 400,
                    mensaje = "Parámetros inválidos.",
                    detalle = excepcionArgumento.Message,
                    tabla = tabla,
                    filtro = $"{nombreClave} = {valorClave}"
                });
            }
            catch (InvalidOperationException excepcionOperacion)
            {
                return StatusCode(500, new
                {
                    estado = 500,
                    mensaje = "Error en la operación de actualización.",
                    detalle = excepcionOperacion.Message,
                    tabla = tabla,
                    filtro = $"{nombreClave} = {valorClave}"
                });
            }
            catch (Exception excepcionGeneral)
            {
                _logger.LogError(excepcionGeneral,
                    "ERROR CRÍTICO - Falla en actualización - Tabla: {Tabla}, Clave: {Clave}={Valor}",
                    tabla, nombreClave, valorClave
                );

                return StatusCode(500, new
                {
                    estado = 500,
                    mensaje = "Error interno del servidor.",
                    tabla = tabla,
                    filtro = $"{nombreClave} = {valorClave}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [AllowAnonymous]
        [HttpDelete("{nombreClave}/{valorClave}")]
        public async Task<IActionResult> EliminarAsync(
            string tabla,                                          
            string nombreClave,                                    
            string valorClave,                                     
            [FromQuery] string? esquema = null                     
        )
        {
            try
            {

                _logger.LogInformation(
                    "INICIO eliminación - Tabla: {Tabla}, Clave: {Clave}={Valor}, Esquema: {Esquema}",
                    tabla, nombreClave, valorClave, esquema ?? "por defecto"
                );

                int filasEliminadas = await _servicioCrud.EliminarAsync(
                    tabla, esquema, nombreClave, valorClave
                );

                if (filasEliminadas > 0)
                {
                    _logger.LogInformation(
                        "ÉXITO eliminación - {FilasEliminadas} filas eliminadas de tabla {Tabla} WHERE {Clave}={Valor}",
                        filasEliminadas, tabla, nombreClave, valorClave
                    );

                    return Ok(new
                    {
                        estado = 200,
                        mensaje = "Registro eliminado exitosamente.",
                        tabla = tabla,
                        esquema = esquema ?? "por defecto",
                        filtro = $"{nombreClave} = {valorClave}",
                        filasEliminadas = filasEliminadas
                    });
                }
                else
                {

                    return NotFound(new
                    {
                        estado = 404,
                        mensaje = "No se encontró el registro a eliminar.",
                        detalle = $"No existe un registro con {nombreClave} = {valorClave} en la tabla {tabla}",
                        tabla = tabla,
                        filtro = $"{nombreClave} = {valorClave}"
                    });
                }
            }
            catch (UnauthorizedAccessException excepcionAcceso)
            {
                return StatusCode(403, new
                {
                    estado = 403,
                    mensaje = "Acceso denegado.",
                    detalle = excepcionAcceso.Message,
                    tabla = tabla
                });
            }
            catch (ArgumentException excepcionArgumento)
            {
                return BadRequest(new
                {
                    estado = 400,
                    mensaje = "Parámetros inválidos.",
                    detalle = excepcionArgumento.Message,
                    tabla = tabla,
                    filtro = $"{nombreClave} = {valorClave}"
                });
            }
            catch (InvalidOperationException excepcionOperacion)
            {

                if (excepcionOperacion.InnerException is SqlException sqlEx &&
                    sqlEx.Number == 547)
                {
                    return Conflict(new
                    {
                        estado = 409,
                        mensaje = "No se puede eliminar el registro.",
                        detalle = "El registro está siendo referenciado por otros datos (restricción de clave foránea).",
                        tabla = tabla,
                        filtro = $"{nombreClave} = {valorClave}"
                    });
                }

                return StatusCode(500, new
                {
                    estado = 500,
                    mensaje = "Error en la operación de eliminación.",
                    detalle = excepcionOperacion.Message,
                    tabla = tabla,
                    filtro = $"{nombreClave} = {valorClave}"
                });
            }
            catch (Exception excepcionGeneral)
            {
                _logger.LogError(excepcionGeneral,
                    "ERROR CRÍTICO - Falla en eliminación - Tabla: {Tabla}, Clave: {Clave}={Valor}",
                    tabla, nombreClave, valorClave
                );

                return StatusCode(500, new
                {
                    estado = 500,
                    mensaje = "Error interno del servidor.",
                    tabla = tabla,
                    filtro = $"{nombreClave} = {valorClave}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [AllowAnonymous]                                  
        [HttpGet]                                         
        [Route("api/info")]                               
        public IActionResult ObtenerInformacion()
        {
            return Ok(new
            {

                controlador = "EntidadesController",
                version = "1.0",
                descripcion = "Controlador genérico para consultar tablas de base de datos",

                endpoints = new[]
                {
                   "GET /api/{tabla} - Lista registros de una tabla",
                   "GET /api/{tabla}?esquema={esquema} - Lista con esquema específico",
                   "GET /api/{tabla}?limite={numero} - Lista con límite de registros",
                   "GET /api/info - Muestra esta información"
               },

                ejemplos = new[]
                {
                   "GET /api/usuarios",
                   "GET /api/productos?esquema=ventas",
                   "GET /api/clientes?limite=50",
                   "GET /api/pedidos?esquema=ventas&limite=100"
               }
            });
        }

        [AllowAnonymous]                                  
        [HttpGet("/")]                                    
        public IActionResult Inicio()
        {
            return Ok(new
            {

                Mensaje = "Bienvenido a la API Genérica en C#",
                Version = "1.0",
                Descripcion = "API genérica para operaciones CRUD sobre cualquier tabla de base de datos",
                Documentacion = "Para más detalles, visita /swagger",
                FechaServidor = DateTime.UtcNow,          

                Enlaces = new
                {
                    Swagger = "/swagger",                 
                    Info = "/api/info",                   
                    EjemploTabla = "/api/MiTabla"        
                },

                Uso = new[]
                {
                   "GET /api/{tabla} - Lista registros de una tabla",
                   "GET /api/{tabla}?limite=50 - Lista con límite específico",
                   "GET /api/{tabla}?esquema=dbo - Lista con esquema específico"
               }
            });
        }

        private object? ConvertirJsonElement(JsonElement elemento)
        {

            return elemento.ValueKind switch
            {

                JsonValueKind.String => elemento.GetString(),

                JsonValueKind.Number => elemento.TryGetInt32(out int intValue)
                    ? intValue           
                    : elemento.GetDouble(),  

                JsonValueKind.True => true,
                JsonValueKind.False => false,

                JsonValueKind.Null => null,

                JsonValueKind.Object => elemento.GetRawText(),
                JsonValueKind.Array => elemento.GetRawText(),

                _ => elemento.ToString()
            };
        }

        [AllowAnonymous]
        [HttpPost("verificar-contrasena")]
        public async Task<IActionResult> VerificarContrasenaAsync(
            string tabla,                                                    
            [FromBody] Dictionary<string, object?> datos,                    
            [FromQuery] string? esquema = null                               
        )
        {
            try
            {

                _logger.LogInformation(
                    "INICIO verificación credenciales - Tabla: {Tabla}, Esquema: {Esquema}",
                    tabla, esquema ?? "por defecto"
                );

                if (datos == null || !datos.Any())
                {
                    return BadRequest(new
                    {
                        estado = 400,
                        mensaje = "Los parámetros de verificación no pueden estar vacíos.",
                        tabla = tabla
                    });
                }

                var datosConvertidos = new Dictionary<string, object?>();
                foreach (var kvp in datos)
                {
                    if (kvp.Value is JsonElement elemento)
                    {
                        datosConvertidos[kvp.Key] = ConvertirJsonElement(elemento);
                    }
                    else
                    {
                        datosConvertidos[kvp.Key] = kvp.Value;
                    }
                }

                var parametrosRequeridos = new[] { "campoUsuario", "campoContrasena", "valorUsuario", "valorContrasena" };
                foreach (var parametro in parametrosRequeridos)
                {
                    if (!datosConvertidos.ContainsKey(parametro) || 
                        string.IsNullOrWhiteSpace(datosConvertidos[parametro]?.ToString()))
                    {
                        return BadRequest(new
                        {
                            estado = 400,
                            mensaje = $"El parámetro '{parametro}' es requerido.",
                            tabla = tabla,
                            parametrosRequeridos = parametrosRequeridos
                        });
                    }
                }

                string campoUsuario = datosConvertidos["campoUsuario"]?.ToString() ?? "";
                string campoContrasena = datosConvertidos["campoContrasena"]?.ToString() ?? "";
                string valorUsuario = datosConvertidos["valorUsuario"]?.ToString() ?? "";
                string valorContrasena = datosConvertidos["valorContrasena"]?.ToString() ?? "";

                _logger.LogInformation(
                    "Verificando credenciales - Usuario: {Usuario}, Tabla: {Tabla}",
                    valorUsuario, tabla
                );

                var (codigo, mensaje) = await _servicioCrud.VerificarContrasenaAsync(
                    tabla, esquema, campoUsuario, campoContrasena, valorUsuario, valorContrasena
                );

                switch (codigo)
                {
                    case 200:
                        _logger.LogInformation(
                            "ÉXITO autenticación - Usuario {Usuario} autenticado correctamente en tabla {Tabla}",
                            valorUsuario, tabla
                        );

                        return Ok(new
                        {
                            estado = 200,
                            mensaje = "Credenciales verificadas exitosamente.",
                            tabla = tabla,
                            usuario = valorUsuario,
                            autenticado = true
                        });

                    case 404:
                        _logger.LogWarning(
                            "FALLO autenticación - Usuario {Usuario} no encontrado en tabla {Tabla}",
                            valorUsuario, tabla
                        );

                        return NotFound(new
                        {
                            estado = 404,
                            mensaje = "Usuario no encontrado.",
                            tabla = tabla,
                            usuario = valorUsuario,
                            autenticado = false
                        });

                    case 401:
                        _logger.LogWarning(
                            "FALLO autenticación - Contraseña incorrecta para usuario {Usuario} en tabla {Tabla}",
                            valorUsuario, tabla
                        );

                        return Unauthorized(new
                        {
                            estado = 401,
                            mensaje = "Contraseña incorrecta.",
                            tabla = tabla,
                            usuario = valorUsuario,
                            autenticado = false
                        });

                    default:
                        return StatusCode(500, new
                        {
                            estado = 500,
                            mensaje = "Error durante la verificación de credenciales.",
                            detalle = mensaje,
                            tabla = tabla
                        });
                }
            }
            catch (UnauthorizedAccessException excepcionAcceso)
            {
                return StatusCode(403, new
                {
                    estado = 403,
                    mensaje = "Acceso denegado.",
                    detalle = excepcionAcceso.Message,
                    tabla = tabla
                });
            }
            catch (ArgumentException excepcionArgumento)
            {
                return BadRequest(new
                {
                    estado = 400,
                    mensaje = "Parámetros inválidos.",
                    detalle = excepcionArgumento.Message,
                    tabla = tabla
                });
            }
            catch (Exception excepcionGeneral)
            {
                _logger.LogError(excepcionGeneral,
                    "ERROR CRÍTICO - Falla en verificación de credenciales - Tabla: {Tabla}",
                    tabla
                );

                return StatusCode(500, new
                {
                    estado = 500,
                    mensaje = "Error interno del servidor.",
                    tabla = tabla,
                    timestamp = DateTime.UtcNow
                });
            }
        }

    }
}
