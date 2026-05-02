using GraficasMixing.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class OvenController : Controller
{
    private readonly GaficadoreTestContext _context;

    public OvenController(GaficadoreTestContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Chart()
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
    public IActionResult GetLast10Main(string oven)
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
    public IActionResult GetLatestMain(string oven)
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

    [HttpGet]
    public async Task<IActionResult> GetLatest(int oven)
    {
        switch (oven)
        {
            case 1:
                var last1 = await _context.Oven1
                    .OrderByDescending(x => x.Date)
                    .ThenByDescending(x => x.Hors)
                    .FirstOrDefaultAsync();

                return Json(last1);

            case 2:
                var last2 = await _context.Oven2
                    .OrderByDescending(x => x.Date)
                    .ThenByDescending(x => x.Hors)
                    .FirstOrDefaultAsync();

                return Json(last2);

            case 3:
                var last3 = await _context.Oven3
                    .OrderByDescending(x => x.Date)
                    .ThenByDescending(x => x.Hors)
                    .FirstOrDefaultAsync();

                return Json(last3);

            case 4:
                var last4 = await _context.Oven4
                    .OrderByDescending(x => x.Date)
                    .ThenByDescending(x => x.Hors)
                    .FirstOrDefaultAsync();

                return Json(last4);

            case 5:
                var last5 = await _context.Oven5
                    .OrderByDescending(x => x.Date)
                    .ThenByDescending(x => x.Hors)
                    .FirstOrDefaultAsync();

                return Json(last5);

            case 6:
                var last6 = await _context.Oven6
                    .OrderByDescending(x => x.Date)
                    .ThenByDescending(x => x.Hors)
                    .FirstOrDefaultAsync();

                return Json(last6);

            default:
                return BadRequest("Oven inválido");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetByTurno(int oven)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        var ahoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

        var hoy = ahoraLocal.Date;
        var horaActual = ahoraLocal.TimeOfDay;

        TimeSpan inicioTurno;
        TimeSpan finTurno;

        // Turno 1 → 07:00 a 15:30
        if (horaActual >= new TimeSpan(7, 0, 0) &&
            horaActual < new TimeSpan(15, 30, 0))
        {
            inicioTurno = new TimeSpan(7, 0, 0);
            finTurno = new TimeSpan(15, 30, 0);
        }
        // Turno 2 → 15:30 a 24:00
        else if (horaActual >= new TimeSpan(15, 30, 0) &&
                 horaActual <= new TimeSpan(23, 59, 59))
        {
            inicioTurno = new TimeSpan(15, 30, 0);
            finTurno = new TimeSpan(23, 59, 59);
        }
        // Turno 3 → 00:00 a 07:00
        else
        {
            inicioTurno = new TimeSpan(0, 0, 0);
            finTurno = new TimeSpan(7, 0, 0);
        }

        switch (oven)
        {
            case 1:
                return Json(await _context.Oven1
                    .Where(x => x.Date.Date == hoy &&
                                x.Hors >= inicioTurno &&
                                x.Hors <= finTurno)
                    .OrderBy(x => x.Date)
                    .ThenBy(x => x.Hors)
                    .ToListAsync());

            case 2:
                return Json(await _context.Oven2
                    .Where(x => x.Date.Date == hoy &&
                                x.Hors >= inicioTurno &&
                                x.Hors <= finTurno)
                    .OrderBy(x => x.Date)
                    .ThenBy(x => x.Hors)
                    .ToListAsync());

            case 3:
                return Json(await _context.Oven3
                    .Where(x => x.Date.Date == hoy &&
                                x.Hors >= inicioTurno &&
                                x.Hors <= finTurno)
                    .OrderBy(x => x.Date)
                    .ThenBy(x => x.Hors)
                    .ToListAsync());

            case 4:
                return Json(await _context.Oven4
                    .Where(x => x.Date.Date == hoy &&
                                x.Hors >= inicioTurno &&
                                x.Hors <= finTurno)
                    .OrderBy(x => x.Date)
                    .ThenBy(x => x.Hors)
                    .ToListAsync());

            case 5:
                return Json(await _context.Oven5
                    .Where(x => x.Date.Date == hoy &&
                                x.Hors >= inicioTurno &&
                                x.Hors <= finTurno)
                    .OrderBy(x => x.Date)
                    .ThenBy(x => x.Hors)
                    .ToListAsync());

            case 6:
                return Json(await _context.Oven6
                    .Where(x => x.Date.Date == hoy &&
                                x.Hors >= inicioTurno &&
                                x.Hors <= finTurno)
                    .OrderBy(x => x.Date)
                    .ThenBy(x => x.Hors)
                    .ToListAsync());

            default:
                return BadRequest("Oven inválido");
        }
    }
}