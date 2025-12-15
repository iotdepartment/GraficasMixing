using GraficasMixing.Models;
using Microsoft.AspNetCore.Mvc;
using System;
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

    [HttpGet]
    public JsonResult GetData(DateTime fechaInicio, DateTime fechaFin, string kneader, string turno)
    {
        // Base query por fechas y kneader
        var query = _context.KneaderM
                            .Where(x => x.Date.Date >= fechaInicio.Date &&
                                        x.Date.Date <= fechaFin.Date &&
                                        x.Kneader == kneader);

        // Filtrar por turno si se especifica
        if (!string.IsNullOrEmpty(turno))
        {
            switch (turno)
            {
                case "Turno1": // 06:00 - 14:00
                    query = query.Where(x => x.Time >= new TimeSpan(7, 0, 0) &&
                                             x.Time < new TimeSpan(14, 0, 0));
                    break;

                case "Turno2": // 14:00 - 22:00
                    query = query.Where(x => x.Time >= new TimeSpan(14, 0, 0) &&
                                             x.Time < new TimeSpan(24, 0, 0));
                    break;

                case "Turno3": // 22:00 - 06:00 (abarca medianoche)
                    query = query.Where(x => (x.Time >= new TimeSpan(24, 0, 0)) ||
                                             (x.Time < new TimeSpan(7, 0, 0)));
                    break;
            }
        }

        var datos = query.OrderBy(x => x.Date)
                         .ThenBy(x => x.Time)
                         .ToList();

        return Json(datos);
    }
}