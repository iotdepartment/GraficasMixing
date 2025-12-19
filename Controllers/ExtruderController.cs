using GraficasMixing.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GraficasMixing.Controllers
{
    public class ExtruderController : Controller
    {
        private readonly MasterMcontext _context;

        public ExtruderController(MasterMcontext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var datos = _context.MasterM
                .OrderBy(x => x.FechaAjustada)
                .ToList();

            var modelo = new
            {
                E1 = datos.Where(x => x.ExtruderID == 1)
                          .Select(x => new { Fecha = x.FechaAjustada, Estado = x.VelocidadRPM == 0 ? 0 : 1 })
                          .ToList(),

                E3 = datos.Where(x => x.ExtruderID == 3)
                          .Select(x => new { Fecha = x.FechaAjustada, Estado = x.VelocidadRPM == 0 ? 0 : 1 })
                          .ToList(),

                E4 = datos.Where(x => x.ExtruderID == 4)
                          .Select(x => new { Fecha = x.FechaAjustada, Estado = x.VelocidadRPM == 0 ? 0 : 1 })
                          .ToList(),

                E5 = datos.Where(x => x.ExtruderID == 5)
                          .Select(x => new { Fecha = x.FechaAjustada, Estado = x.VelocidadRPM == 0 ? 0 : 1 })
                          .ToList()
            };

            return View(modelo);
        }
    }
}