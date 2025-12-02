using GraficasMixing.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

public class KneaderController : Controller
{
    private readonly GaficadoreTestContext _context;

    public KneaderController(GaficadoreTestContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var hoy = DateTime.Today;

        // Solo datos de hoy y Kneader1 para inicializar
        var deHoyKneader1 = _context.KneaderM
                                    .Where(x => x.Date.Date == hoy && x.Kneader == "Kneader1")
                                    .OrderBy(x => x.Time)
                                    .ToList();

        return View(deHoyKneader1);
    }

    // Endpoint para AJAX: devuelve datos filtrados por rango y kneader
    [HttpGet]
    public JsonResult GetData(DateTime fechaInicio, DateTime fechaFin, string kneader)
    {
        var datos = _context.KneaderM
                            .Where(x => x.Date.Date >= fechaInicio.Date &&
                                        x.Date.Date <= fechaFin.Date &&
                                        x.Kneader == kneader)
                            .OrderBy(x => x.Date)
                            .ThenBy(x => x.Time)
                            .ToList();

        return Json(datos);
    }
}