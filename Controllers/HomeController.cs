using System.Diagnostics;
using GraficasMixing.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GraficasMixing.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly GaficadoreTestContext _context;

        // Constructor con inyección de dependencias
        public HomeController(ILogger<HomeController> logger, GaficadoreTestContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {

            return View();
        }

        public IActionResult Privacy()
        {

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}