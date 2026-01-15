using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EjemploWebVentasFront.Pages.Admin.Ventas
{
    public class DetailsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public DetailsModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public string ErrorMessage { get; set; } = string.Empty;
        public VentaDto? Venta { get; set; }

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

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var token = GetToken();
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToPage("/Account/Login");

            var rol = HttpContext.Session.GetString("ROL");
            if (rol != "ADMIN")
                return RedirectToPage("/Ventas/Create");

            var client = _httpClientFactory.CreateClient("VentasApi");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var resp = await client.GetAsync($"/api/Ventas/{id}");
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                ErrorMessage = $"Error API {(int)resp.StatusCode} {resp.StatusCode}: {body}";
                return Page();
            }

            Venta = JsonSerializer.Deserialize<VentaDto>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return Page();
        }

        private string? GetToken()
        {
            var key = _configuration["Jwt:SessionKey"] ?? "ACCESS_TOKEN";
            return HttpContext.Session.GetString(key);
        }
    }
}