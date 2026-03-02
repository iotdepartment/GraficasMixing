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
    public JsonResult GetData(DateTime fecha, string kneader, string turno)
    {
        var query = _context.KneaderM
                            .Where(x => x.Kneader == kneader);

        switch (turno)
        {
            case "Turno1": // 07:00 - 14:59
                query = query.Where(x => x.Date.Date == fecha.Date &&
                                         x.Time >= new TimeSpan(7, 0, 0) &&
                                         x.Time <= new TimeSpan(14, 59, 59));
                break;

            case "Turno2": // 15:00 - 23:59
                query = query.Where(x => x.Date.Date == fecha.Date &&
                                         x.Time >= new TimeSpan(15, 0, 0) &&
                                         x.Time <= new TimeSpan(23, 59, 59));
                break;

            case "Turno3": // 00:00 - 06:59 del día siguiente
                var siguienteDia = fecha.Date.AddDays(0);
                query = query.Where(x => x.Date.Date == siguienteDia &&
                                         x.Time >= new TimeSpan(0, 0, 1) &&
                                         x.Time <= new TimeSpan(6, 59, 59));
                break;
        }

        var datos = query.OrderBy(x => x.Date)
                         .ThenBy(x => x.Time)
                         .ToList();

        return Json(datos);
    }
    [HttpGet]
    public IActionResult GetLatest(string kneader)
    {
        var last = _context.KneaderM
            .Where(x => x.Kneader == kneader)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.Time)
            .FirstOrDefault();

        return Json(last);
    }

    public IActionResult Chart()
    {

        return View();
    }

    [HttpGet]
    public IActionResult GetKneaderData1(DateTime fecha)
    {
        try
        {
            // Traemos los registros de DB y pasamos a memoria
            var registros = _context.KneaderM
                .Where(x => x.Kneader == "Kneader1")
                .OrderByDescending(x => x.Date) // EF sí entiende esto
                .Take(100) // traemos un poco más para luego filtrar en memoria
                .AsEnumerable() // 👈 pasamos a LINQ to Objects
                .Select(x =>
                {
                    var fechaCompleta = x.Date.Add(x.Time);

                    decimal.TryParse(x.Pressure, out var p);
                    decimal.TryParse(x.Power, out var pw);
                    decimal.TryParse(x.Revolution, out var r);
                    decimal.TryParse(x.Temperature, out var t);

                    return new
                    {
                        FechaCompleta = fechaCompleta,
                        Hora = fechaCompleta.ToString("HH:mm:ss"),
                        Pressure = p < 0 ? 0 : Math.Round(p, 0),
                        Power = pw < 0 ? 0 : Math.Round(pw, 0),
                        Revolution = r < 0 ? 0 : Math.Round(r, 0),
                        Temperature = t < 0 ? 0 : Math.Round(t, 0)
                    };
                })
                .OrderByDescending(x => x.FechaCompleta)
                .Take(30) // 👈 ahora sí, últimos 30 registros
                .OrderBy(x => x.FechaCompleta) // para graficar en orden cronológico
                .ToList();

            return Json(new { success = true, data = registros });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public IActionResult GetKneaderData2(DateTime fecha)
    {
        var hoy = DateTime.Today;
        DateTime haceVeinteMin = DateTime.Now.AddMinutes(-5);

        try
        {
            var data = _context.KneaderM
                .Where(x => x.Date.Date == hoy && x.Kneader == "Kneader2")
                .AsEnumerable()
                .Select(x => new
                {
                    FechaCompleta = x.Date.Add(x.Time),

                    // 🔥 Convertir string → decimal con TryParse
                    Pressure = decimal.TryParse(x.Pressure, out var p) ? (p < 0 ? 0 : Math.Round(p, 0)) : 0,
                    Power = decimal.TryParse(x.Power, out var pw) ? (pw < 0 ? 0 : Math.Round(pw, 0)) : 0,
                    Revolution = decimal.TryParse(x.Revolution, out var r) ? (r < 0 ? 0 : Math.Round(r, 0)) : 0,
                    Temperature = decimal.TryParse(x.Temperature, out var t) ? (t < 0 ? 0 : Math.Round(t, 0)) : 0
                })
                .Where(x => x.FechaCompleta >= haceVeinteMin)
                .OrderBy(x => x.FechaCompleta)
                .Select(x => new
                {
                    Hora = x.FechaCompleta.ToString("HH:mm:ss"),
                    x.Pressure,
                    x.Power,
                    x.Revolution,
                    x.Temperature
                })
                .ToList();

            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public IActionResult GetKneaderData3(DateTime fecha)
    {
        var hoy = DateTime.Today;
        DateTime haceVeinteMin = DateTime.Now.AddMinutes(-5);

        try
        {
            var data = _context.KneaderM
                .Where(x => x.Date.Date == hoy && x.Kneader == "Kneader3")
                .AsEnumerable()
                .Select(x => new
                {
                    FechaCompleta = x.Date.Add(x.Time),

                    // 🔥 Convertir string → decimal con TryParse
                    Pressure = decimal.TryParse(x.Pressure, out var p) ? (p < 0 ? 0 : Math.Round(p, 0)) : 0,
                    Power = decimal.TryParse(x.Power, out var pw) ? (pw < 0 ? 0 : Math.Round(pw, 0)) : 0,
                    Revolution = decimal.TryParse(x.Revolution, out var r) ? (r < 0 ? 0 : Math.Round(r, 0)) : 0,
                    Temperature = decimal.TryParse(x.Temperature, out var t) ? (t < 0 ? 0 : Math.Round(t, 0)) : 0
                })
                .Where(x => x.FechaCompleta >= haceVeinteMin)
                .OrderBy(x => x.FechaCompleta)
                .Select(x => new
                {
                    Hora = x.FechaCompleta.ToString("HH:mm:ss"),
                    x.Pressure,
                    x.Power,
                    x.Revolution,
                    x.Temperature
                })
                .ToList();

            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

}