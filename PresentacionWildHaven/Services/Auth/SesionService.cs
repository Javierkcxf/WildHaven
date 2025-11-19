using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using PresentacionWildHaven.Models;

namespace PresentacionWildHaven.Services.Auth
{

    public class SesionService
    {
        private static SesionService? _instancia;
        private static readonly object _lock = new object();

        private readonly IJSRuntime _js;
        private readonly IHttpClientFactory _httpClientFactory;

        // Datos de sesión en memoria
        private string? _usuarioId;
        private string? _nombreUsuario;
        private string? _email;
        private string? _rol;
        private string? _token;
        private List<RutaRol>? _rutasRol;

        // Propiedades públicas
        public string? UsuarioId => _usuarioId;
        public string? NombreUsuario => _nombreUsuario;
        public string? Email => _email;
        public string? Rol => _rol;
        public string? Token => _token;
        public bool EstaAutenticado => !string.IsNullOrEmpty(_token);
        public bool EsAdministrador => _rol == "Administrador" || _rol == "Admin";
        public bool EsUsuario => _rol == "Usuario";

        public event Action? OnCambioSesion;

        // Constructor privado (Singleton)
        public SesionService(IJSRuntime js, IHttpClientFactory httpClientFactory)
        {
            _js = js;
            _httpClientFactory = httpClientFactory;
        }

        // ✅ PATRÓN SINGLETON con inyección de dependencias
        public static SesionService Instancia { get; private set; } = null!;

        /// <summary>
        /// Inicializa el Singleton. Debe llamarse en Program.cs ANTES de usar la instancia.
        /// </summary>
        public static void Inicializar(IJSRuntime js, IHttpClientFactory httpClientFactory)
        {
            if (_instancia == null)
            {
                lock (_lock)
                {
                    if (_instancia == null)
                    {
                        _instancia = new SesionService(js, httpClientFactory);
                        Instancia = _instancia;
                    }
                }
            }
        }

       public async Task IniciarSesionAsync(string usuarioId, string nombreUsuario, string email, string rol, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("El token no puede estar vacío");

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("El email no puede estar vacío");

            _usuarioId = usuarioId;
            _nombreUsuario = nombreUsuario;
            _email = email;
            _rol = rol;
            _token = token;

            await _js.InvokeVoidAsync("sessionStorage.setItem", "usuarioId", usuarioId);
            await _js.InvokeVoidAsync("sessionStorage.setItem", "nombreUsuario", nombreUsuario);
            await _js.InvokeVoidAsync("sessionStorage.setItem", "email", email);
            await _js.InvokeVoidAsync("sessionStorage.setItem", "rol", rol);
            await _js.InvokeVoidAsync("sessionStorage.setItem", "token", token);

            OnCambioSesion?.Invoke();
        }

        public async Task CerrarSesionAsync()
        {
            _usuarioId = null;
            _nombreUsuario = null;
            _email = null;
            _rol = null;
            _token = null;
            _rutasRol = null;

            await _js.InvokeVoidAsync("sessionStorage.clear");

            OnCambioSesion?.Invoke();
        }

        public async Task<bool> RestaurarSesionAsync()
        {
            try
            {
                var token = await _js.InvokeAsync<string>("sessionStorage.getItem", "token");

                if (string.IsNullOrWhiteSpace(token))
                    return false;

                _usuarioId = await _js.InvokeAsync<string>("sessionStorage.getItem", "usuarioId");
                _nombreUsuario = await _js.InvokeAsync<string>("sessionStorage.getItem", "nombreUsuario");
                _email = await _js.InvokeAsync<string>("sessionStorage.getItem", "email");
                _rol = await _js.InvokeAsync<string>("sessionStorage.getItem", "rol");
                _token = token;

                OnCambioSesion?.Invoke();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public HttpClient ObtenerClienteAutenticado()
        {
            var cliente = _httpClientFactory.CreateClient("ApiGenerica");

            if (!string.IsNullOrWhiteSpace(_token))
            {
                cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            }

            return cliente;
        }

        public IEnumerable<Claim> ObtenerClaims()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_token))
                    return Enumerable.Empty<Claim>();

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(_token);
                return jwtToken.Claims;
            }
            catch
            {
                return Enumerable.Empty<Claim>();
            }
        }

        public async Task GuardarRutasRolAsync(List<RutaRol> rutasRol)
        {
            if (rutasRol == null)
                throw new ArgumentNullException(nameof(rutasRol));

            _rutasRol = rutasRol;

            var json = JsonSerializer.Serialize(rutasRol);
            await _js.InvokeVoidAsync("sessionStorage.setItem", "rutasRol", json);
        }

        public async Task<List<RutaRol>> ObtenerRutasRolAsync()
        {
            if (_rutasRol != null)
                return _rutasRol;

            try
            {
                var json = await _js.InvokeAsync<string>("sessionStorage.getItem", "rutasRol");

                if (!string.IsNullOrWhiteSpace(json))
                {
                    _rutasRol = JsonSerializer.Deserialize<List<RutaRol>>(json);
                    return _rutasRol ?? new List<RutaRol>();
                }
            }
            catch
            {
                // Ignorar errores
            }

            return new List<RutaRol>();
        }

        public async Task<bool> TienePermisoParaRutaAsync(string ruta)
        {
            try
            {
                var rutasRol = await ObtenerRutasRolAsync();

                if (rutasRol == null || rutasRol.Count == 0)
                    return false;

                return rutasRol.Any(r =>
                    r.NombreRuta?.Equals(ruta, StringComparison.OrdinalIgnoreCase) == true);
            }
            catch
            {
                return false;
            }
        }
    }
}