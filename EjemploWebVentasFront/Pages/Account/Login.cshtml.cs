using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EjemploWebVentasFront.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;



        public LoginModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnPostAsync()
        {


            var client = _httpClientFactory.CreateClient("VentasApi");

            // Ajusta el endpoint según tu API
            var loginRequest = new
            {
                email = Username,
                password = Password
            };

            var content = new StringContent(
                JsonSerializer.Serialize(loginRequest),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync("/api/auth/login", content);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = "Credenciales inválidas";
                return Page();
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();

            // Se asume que el API devuelve: { "token": "xxxxx" }
            using var document = JsonDocument.Parse(jsonResponse);
            var token = document.RootElement.GetProperty("token").GetString();

            if (string.IsNullOrEmpty(token))
            {
                ErrorMessage = "No se recibió token";
                return Page();
            }

            // Guardar VendedorId desde el token (claim)
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            // Intenta varios nombres comunes de claim
            string? vendedorIdStr =
                jwt.Claims.FirstOrDefault(c => c.Type == "vendedorId")?.Value ??
                jwt.Claims.FirstOrDefault(c => c.Type == "id")?.Value ??
                jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ??
                jwt.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value;

            if (int.TryParse(vendedorIdStr, out var vendedorId))
            {
                HttpContext.Session.SetInt32("VENDEDOR_ID", vendedorId);
            }

            // Guardar token en Session
            HttpContext.Session.SetString(
                _configuration["Jwt:SessionKey"]!,
                token
            );

            return RedirectToPage("/Ventas/Create");
        }
    }
}
