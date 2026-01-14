using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;


namespace EjemploWebVentasFront.Pages.Ventas
{
    using System.Text;
    using System.Text.Json;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    namespace EjemploWebVentasFront.Pages.Ventas
    {
        public class CreateModel : PageModel
        {
            private readonly IHttpClientFactory _httpClientFactory;
            private readonly IConfiguration _configuration;

            public CreateModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
            {
                _httpClientFactory = httpClientFactory;
                _configuration = configuration;
            }

            // Carros para pintar tarjetas
            public List<CarroDto> Carros { get; set; } = new();

            // Este hidden trae el "carrito" en JSON (carroId + cantidad)
            [BindProperty]
            public string DetallesJson { get; set; } = "[]";

            public string ErrorMessage { get; set; } = string.Empty;
            public string SuccessMessage { get; set; } = string.Empty;

            public class CarroDto
            {
                // Tu tabla muestra: idC, marca, modelo, anio, precioC
                public int idC { get; set; }
                public string marca { get; set; } = "";
                public string modelo { get; set; } = "";
                public int anio { get; set; }
                public decimal precioC { get; set; }
            }

            public class DetalleDto
            {
                public int carroId { get; set; }
                public int cantidad { get; set; }
            }

            public async Task<IActionResult> OnGetAsync()
            {
                var token = GetToken();
                if (string.IsNullOrWhiteSpace(token))
                    return RedirectToPage("/Account/Login");

                // Cargar carros desde API
                var client = _httpClientFactory.CreateClient("VentasApi");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                //  AJUSTA AQUÍ si tu endpoint no es /api/Carros
                var resp = await client.GetAsync("/api/Carros");
                var body = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    ErrorMessage = $"No se pudieron cargar carros: {(int)resp.StatusCode} {resp.StatusCode} - {body}";
                    return Page();
                }

                Carros = JsonSerializer.Deserialize<List<CarroDto>>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

                return Page();
            }
            //public IActionResult OnGet()
            //{
            //    var token = HttpContext.Session.GetString("ACCESS_TOKEN");
            //    if (string.IsNullOrWhiteSpace(token))
            //    {
            //        return RedirectToPage("/Account/Login");
            //    }

            //    return Page();
            //}
            public async Task<IActionResult> OnPostAsync()
            {
                var token = GetToken();
                if (string.IsNullOrWhiteSpace(token))
                    return RedirectToPage("/Account/Login");

                var vendedorId = HttpContext.Session.GetInt32("VENDEDOR_ID");
                if (vendedorId is null || vendedorId <= 0)
                {
                    ErrorMessage = "No se encontró el vendedorId en sesión. Reingresa al sistema.";
                    return Page();
                }

                List<DetalleDto>? detalles;
                try
                {
                    detalles = JsonSerializer.Deserialize<List<DetalleDto>>(DetallesJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    ErrorMessage = "Detalles inválidos (JSON).";
                    return Page();
                }

                if (detalles == null || detalles.Count == 0)
                {
                    ErrorMessage = "Debes seleccionar al menos un carro y definir cantidad.";
                    await OnGetAsync(); // recargar carros
                    return Page();
                }

                if (detalles.Any(d => d.carroId <= 0 || d.cantidad <= 0))
                {
                    ErrorMessage = "Cada ítem debe tener carroId y cantidad > 0.";
                    await OnGetAsync();
                    return Page();
                }

                // Armar el request EXACTO del Swagger
                var ventaRequest = new
                {
                    vendedorId = vendedorId.Value,
                    detalles = detalles.Select(d => new { carroId = d.carroId, cantidad = d.cantidad }).ToList()
                };

                var client = _httpClientFactory.CreateClient("VentasApi");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var content = new StringContent(
                    JsonSerializer.Serialize(ventaRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync("/api/Ventas", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    ErrorMessage = $"Error al crear venta: {(int)response.StatusCode} {response.StatusCode} - {responseBody}";
                    await OnGetAsync();
                    return Page();
                }

                SuccessMessage = " Venta creada correctamente.";
                // opcional: aquí puedes mostrar responseBody si te devuelve total y quieres pintarlo
                // SuccessMessage = $" Venta creada: {responseBody}";

                await OnGetAsync();
                DetallesJson = "[]";
                return Page();
            }

            private string? GetToken()
            {
                var key = _configuration["Jwt:SessionKey"] ?? "ACCESS_TOKEN";
                return HttpContext.Session.GetString(key);
            }
        }
    }
}
