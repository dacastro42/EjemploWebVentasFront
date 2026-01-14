using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EjemploWebVentasFront.Pages.Admin.Ventas
{
    public class AdminVentasIndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AdminVentasIndexModel(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
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
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = HttpContext.Session.GetString("ACCESS_TOKEN");
            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToPage("/Account/Login");
            }
           // var token = GetToken();
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToPage("/Account/Login");

            // 2️⃣ Validar rol
            var rol = HttpContext.Session.GetString("ROL");
            if (rol != "ADMIN")
                return RedirectToPage("/Ventas/Create");

            // 3️⃣ Consumir API
            var client = _httpClientFactory.CreateClient("VentasApi");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/Ventas");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = $"Error API {(int)response.StatusCode}: {body}";
                return Page();
            }

            Ventas = JsonSerializer.Deserialize<List<VentaDto>>(
                body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new();

            return Page();
        }

        private string? GetToken()
        {
            var key = _configuration["Jwt:SessionKey"] ?? "ACCESS_TOKEN";
            return HttpContext.Session.GetString(key);
        }
    }
}