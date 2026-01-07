using Microsoft.AspNetCore.Mvc;
using GraficasMixing.Models;
using System;
using System.Linq;

public class OvenController : Controller
{
    private readonly GaficadoreTestContext _context;

    public OvenController(GaficadoreTestContext context)
    {
        _context = context;
    }

    // 👉 ESTA ES LA ACCIÓN QUE RETORNA LA VISTA
    public IActionResult Index()
    {
        return View();
    }

    // 👉 ESTA ES LA ACCIÓN QUE RETORNA LOS DATOS (JSON)
    [HttpGet]
    public JsonResult GetData(DateTime fecha, string oven, string turno)
    {
        IQueryable<Oven1> query = null;

        switch (oven)
        {
            case "Oven1":
                query = _context.Oven1;
                break;

            case "Oven2":
                query = _context.Oven2;
                break;

            case "Oven3":
                query = _context.Oven3;
                break;

            case "Oven4":
                query = _context.Oven4;
                break;

            case "Oven5":
                query = _context.Oven5;
                break;

            case "Oven6":
                query = _context.Oven6;
                break;

            default:
                return Json(new { error = "Oven no válido" });
        }

        // FILTRO POR TURNO
        switch (turno)
        {
            case "Turno1": // 07:00 - 14:59
                query = query.Where(x =>
                    x.Date.Date == fecha.Date &&
                    x.Hors >= new TimeSpan(7, 0, 0) &&
                    x.Hors <= new TimeSpan(14, 59, 59)
                );
                break;

            case "Turno2": // 15:00 - 23:59
                query = query.Where(x =>
                    x.Date.Date == fecha.Date &&
                    x.Hors >= new TimeSpan(15, 0, 0) &&
                    x.Hors <= new TimeSpan(23, 59, 59)
                );
                break;

            case "Turno3": // 00:00 - 06:59
                query = query.Where(x =>
                    x.Date.Date == fecha.Date &&
                    x.Hors >= new TimeSpan(0, 0, 1) &&
                    x.Hors <= new TimeSpan(6, 59, 59)
                );
                break;
        }

        var datos = query
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Hors)
            .Select(x => new
            {
                press = x.Pess,
                temp = x.Temp,
                date = x.Date.ToString("yyyy-MM-dd"),
                time = x.Hors.ToString(@"hh\:mm\:ss")
            })
            .ToList();

        return Json(datos);
    }
}