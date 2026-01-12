using Microsoft.AspNetCore.Mvc;
using GraficasMixing.Models;
using System;
using System.Linq;

using Microsoft.EntityFrameworkCore;

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

    [HttpGet]
    public JsonResult GetData(DateTime fecha, string oven, string turno)
    {
        if (turno == "Turno3")
        {
            fecha = fecha.AddDays(1);
        }

        IQueryable<OvenBase> query = oven switch
        {
            "Oven1" => _context.Oven1,
            "Oven2" => _context.Oven2,
            "Oven3" => _context.Oven3,
            "Oven4" => _context.Oven4,
            "Oven5" => _context.Oven5,
            "Oven6" => _context.Oven6,
            _ => null
        };

        if (query == null)
            return Json(new { error = "Oven inválido" });

        TimeSpan start, end;


        switch (turno)
        {
            case "Turno1":
                start = new TimeSpan(7, 0, 0);
                end = new TimeSpan(14, 59, 59);
                break;

            case "Turno2":
                start = new TimeSpan(15, 0, 0);
                end = new TimeSpan(23, 59, 59);
                break;

            case "Turno3":
                start = new TimeSpan(0, 0, 1);
                end = new TimeSpan(6, 59, 59);
                break;

            default:
                return Json(new { error = "Turno inválido" });
        }

        var datos = query
            .Where(x => x.Date.Date == fecha.Date &&
                        x.Hors >= start &&
                        x.Hors <= end)
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
    [HttpGet]
    public IActionResult GetLast10(string oven)
    {
        IQueryable<OvenBase> query = oven switch
        {
            "Oven1" => _context.Oven1,
            "Oven2" => _context.Oven2,
            "Oven3" => _context.Oven3,
            "Oven4" => _context.Oven4,
            "Oven5" => _context.Oven5,
            "Oven6" => _context.Oven6,
            _ => null
        };

        if (query == null)
            return NotFound();

        var last10 = query
            .OrderByDescending(x => x.id)
            .Take(10)
            .Select(x => new
            {
                date = x.Date.ToString("yyyy-MM-dd"),
                time = x.Hors.ToString(@"hh\:mm\:ss"),
                press = x.Pess,
                temp = x.Temp
            })
            .ToList();

        return Json(last10);
    }
[HttpGet]
public IActionResult GetLatest(string oven)
{
    IQueryable<OvenBase> query = oven switch
    {
        "Oven1" => _context.Oven1,
        "Oven2" => _context.Oven2,
        "Oven3" => _context.Oven3,
        "Oven4" => _context.Oven4,
        "Oven5" => _context.Oven5,
        "Oven6" => _context.Oven6,
        _ => null
    };

    if (query == null)
        return NotFound();

    var last = query
        .OrderByDescending(x => x.id)
        .Select(x => new
        {
            press = x.Pess,
            temp = x.Temp,
            date = x.Date.ToString("yyyy-MM-dd"),
            time = x.Hors.ToString(@"hh\:mm\:ss")
        })
        .FirstOrDefault();

    return Json(last);
}
}