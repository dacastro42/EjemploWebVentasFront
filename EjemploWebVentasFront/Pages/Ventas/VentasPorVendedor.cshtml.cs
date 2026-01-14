using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EjemploWebVentasFront.Pages.Ventas
{
    public class VentasPorVendedorModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public VentasPorVendedorModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public List<VentaDto> Ventas { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;

        public class VentaDto
        {
            public int IdVentas { get; set; }
            public int VendedorId { get; set; }
            public DateTime FechaVenta { get; set; }
            public decimal TotalVenta { get; set; }
            public decimal Iva { get; set; }
            public List<DetalleDto> Detalles { get; set; } = new();
        }

        public class DetalleDto
        {
            public int CarroId { get; set; }
            public int Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal Subtotal { get; set; }

            public CarroDto? Carro { get; set; }
        }

        public class CarroDto
        {
            public int IdC { get; set; }
            public string Marca { get; set; } = "";
            public string Modelo { get; set; } = "";
            public int Anio { get; set; }
            public decimal PrecioC { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = HttpContext.Session.GetString("ACCESS_TOKEN");
            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToPage("/Account/Login");
            }

            //var token = GetToken();
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToPage("/Account/Login");

            var vendedorId = HttpContext.Session.GetInt32("VENDEDOR_ID");
            if (vendedorId is null || vendedorId <= 0)
            {
                ErrorMessage = "No se encontró el vendedorId en sesión. Reingresa al sistema.";
                return Page();
            }

            var client = _httpClientFactory.CreateClient("VentasApi");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Ajusta si tu ruta real es distinta:
            var resp = await client.GetAsync($"/api/Ventas/Vendedor/{vendedorId.Value}");
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                ErrorMessage = $"Error API {(int)resp.StatusCode} {resp.StatusCode}: {body}";
                return Page();
            }

            Ventas = JsonSerializer.Deserialize<List<VentaDto>>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            return Page();
        }

        private string? GetToken()
        {
            var key = _configuration["Jwt:SessionKey"] ?? "ACCESS_TOKEN";
            return HttpContext.Session.GetString(key);
        }
    }
}
