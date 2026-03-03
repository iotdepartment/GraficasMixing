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
    public JsonResult GetKneaderData1()
    {
        try
        {
            // Ajustar a zona horaria de México
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
            var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);

            // Día laboral: si estamos antes de las 7:00 am, retrocedemos un día
            var fechaHoy = nowLocal.Hour < 7 ? nowLocal.Date.AddDays(-1) : nowLocal.Date;

            // Rango de tiempo: de hoy 7:00 am a mañana 7:00 am
            var inicio = fechaHoy.AddHours(7);
            var fin = fechaHoy.AddDays(1).AddHours(7);

            var kneader = "Kneader1";

            // Filtrar registros en SQL por fecha aproximada
            var registrosRaw = _context.KneaderM
                .Where(x => x.Kneader == kneader &&
                            (x.Date.Date == fechaHoy || x.Date.Date == fechaHoy.AddDays(1)))
                .OrderBy(x => x.Date)
                .ToList();

            // Procesar en memoria
            var registros = registrosRaw
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
                .Where(x => x.FechaCompleta >= inicio && x.FechaCompleta < fin)
                .OrderBy(x => x.FechaCompleta)
                .ToList();

            return Json(new { success = true, data = registros });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public JsonResult GetKneaderData2()
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
            var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);

            // Día laboral: si estamos antes de las 7:00 am, retrocedemos un día
            var fechaHoy = nowLocal.Hour < 7 ? nowLocal.Date.AddDays(-1) : nowLocal.Date;

            // Rango de tiempo: de hoy 7:00 am a mañana 7:00 am
            var inicio = fechaHoy.AddHours(7);
            var fin = fechaHoy.AddDays(1).AddHours(7);

            var kneader = "Kneader2";

            var registrosRaw = _context.KneaderM
                .Where(x => x.Kneader == kneader &&
                            (x.Date.Date == fechaHoy || x.Date.Date == fechaHoy.AddDays(1)))
                .OrderBy(x => x.Date)
                .ToList();

            var registros = registrosRaw
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
                .Where(x => x.FechaCompleta >= inicio && x.FechaCompleta < fin)
                .OrderBy(x => x.FechaCompleta)
                .ToList();

            return Json(new { success = true, data = registros });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public JsonResult GetKneaderData3()
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
            var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);

            // Día laboral: si estamos antes de las 7:00 am, retrocedemos un día
            var fechaHoy = nowLocal.Hour < 7 ? nowLocal.Date.AddDays(-1) : nowLocal.Date;

            // Rango de tiempo: de hoy 7:00 am a mañana 7:00 am
            var inicio = fechaHoy.AddHours(7);
            var fin = fechaHoy.AddDays(1).AddHours(7);

            var kneader = "Kneader3";

            var registrosRaw = _context.KneaderM
                .Where(x => x.Kneader == kneader &&
                            (x.Date.Date == fechaHoy || x.Date.Date == fechaHoy.AddDays(1)))
                .OrderBy(x => x.Date)
                .ToList();

            var registros = registrosRaw
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
                .Where(x => x.FechaCompleta >= inicio && x.FechaCompleta < fin)
                .OrderBy(x => x.FechaCompleta)
                .ToList();

            return Json(new { success = true, data = registros });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

}