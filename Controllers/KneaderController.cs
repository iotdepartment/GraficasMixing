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
                case "Turno1": // 07:00 - 14:59
                    query = query.Where(x => x.Time >= new TimeSpan(07, 0, 1) &&
                                             x.Time < new TimeSpan(14, 59, 59));
                    break;

                case "Turno2": // 15:00 - 23:59
                    query = query.Where(x => x.Time >= new TimeSpan(15, 0, 1) &&
                                             x.Time < new TimeSpan(23, 59, 59));
                    break;

                case "Turno3":
                    // 00:00:01 - 06:59:59 del día siguiente
                    var siguienteDia = fechaInicio.Date.AddDays(1);

                    query = query.Where(x => x.Date == siguienteDia &&
                                             x.Time >= new TimeSpan(0, 0, 1) &&
                                             x.Time <= new TimeSpan(6, 59, 59));
                    break;
            }
        }

        var datos = query.OrderBy(x => x.Date)
                         .ThenBy(x => x.Time)
                         .ToList();

        return Json(datos);
    }
}