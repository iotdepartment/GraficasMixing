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
    public IActionResult GetData(string fecha, string shift)
    {
        DateTime selectedDate = DateTime.Parse(fecha);

        // Consulta directa SOLO a Oven1
        var query = _context.Oven1
            .Where(x => x.Date.Date == selectedDate.Date)
            .OrderBy(x => x.Hors)
            .ToList();

        // Filtro por turno
        if (shift == "Turno1") // 7:00 - 15:00
        {
            query = query.Where(x =>
                x.Hors >= TimeSpan.FromHours(7) &&
                x.Hors < TimeSpan.FromHours(15)
            ).ToList();
        }
        else if (shift == "Turno2") // 15:00 - 24:00
        {
            query = query.Where(x =>
                x.Hors >= TimeSpan.FromHours(15) &&
                x.Hors < TimeSpan.FromHours(24)
            ).ToList();
        }
        else if (shift == "Turno3") // 00:00 - 7:00
        {
            query = query.Where(x =>
                x.Hors >= TimeSpan.Zero &&
                x.Hors < TimeSpan.FromHours(7)
            ).ToList();
        }

        // Serialización para el frontend
        var result = query.Select(x => new
        {
            Pess = x.Pess,
            Temp = x.Temp,
            date = x.Date.ToString("yyyy-MM-dd"),
            time = x.Hors.ToString(@"hh\:mm\:ss")
        });

        return Json(result);
    }
}