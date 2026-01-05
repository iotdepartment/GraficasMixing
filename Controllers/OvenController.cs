using Microsoft.AspNetCore.Mvc;

namespace GraficasMixing.Controllers
{
    public class OvenController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
