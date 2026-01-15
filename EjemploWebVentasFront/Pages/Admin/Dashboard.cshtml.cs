using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EjemploWebVentasFront.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        [BindProperty(SupportsGet = true)]
        public int? Mes { get; set; }
        [BindProperty(SupportsGet = true)]
        public int? Anio { get; set; }
        public decimal? TotalMesValor { get; set; }

        public DashboardModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public string ErrorMessage { get; set; } = string.Empty;

        // Los reportes pueden devolver objetos distintos; los guardamos como string/JsonElement
        public JsonElement? MayorVenta { get; set; }
        public JsonElement? VendedorTop { get; set; }
        public JsonElement? TotalMes { get; set; }
        public JsonElement? CarroMasVendido { get; set; }

        public async Task<IActionResult> OnGetAsync()
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

            try
            {
                MayorVenta = await GetJson(client, "/api/Ventas/reportes/mayor-venta");
                VendedorTop = await GetJson(client, "/api/Ventas/reportes/vendedor-top");
                //TotalMes = await GetJson(client, "/api/Ventas/reportes/total-mes");
                // Solo consultar total-mes si hay mes válido (1..12)
                if (Anio is null) Anio = DateTime.Now.Year; // por defecto
                if (Mes is >= 1 and <= 12 && Anio is >= 2000 and <= 2100)
                {
                    // Ajusta según tu backend: querystring o ruta
                    // Opción 1: query => /total-mes?mes=1
                    TotalMes = await GetJson(client, $"/api/Ventas/reportes/total-mes?mes={Mes}&anio={Anio}");

                    // Si tu backend fuera por ruta sería:
                    // TotalMes = await GetJson(client, $"/api/Ventas/reportes/total-mes/{Mes}");
                }
                else
                {
                    TotalMes = null;
                }
                CarroMasVendido = await GetJson(client, "/api/Ventas/reportes/carro-mas-vendido");
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return Page();
        }

        private async Task<JsonElement?> GetJson(HttpClient client, string url)
        {
            var resp = await client.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Error API {url}: {(int)resp.StatusCode} - {body}");

            var doc = JsonDocument.Parse(body);
            return doc.RootElement.Clone();
        }

        private string? GetToken()
        {
            var key = _configuration["Jwt:SessionKey"] ?? "ACCESS_TOKEN";
            return HttpContext.Session.GetString(key);
        }
    }
}
