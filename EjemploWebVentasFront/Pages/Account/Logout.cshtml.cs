using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EjemploWebVentasFront.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet()
        {
            //  Limpiar toda la sesión
            HttpContext.Session.Clear();

            // Redirigir al login
            return RedirectToPage("/Account/Login");
        }
    }
}
