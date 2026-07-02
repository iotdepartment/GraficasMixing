using GraficasMixing.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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

    public IActionResult IndividualChart(int id)
    {
        // Si el id es inválido o es 0, asígnale un valor por defecto o redirige
        if (id < 1 || id > 6)
        {
            return RedirectToAction("Index", "Oven");
        }

        ViewBag.OvenNumber = id; // Asegura que nunca vaya vacío
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

    [HttpGet]
    public async Task<IActionResult> GettByTurno(int oven)
    {
        var query = GetOvenDbSet(oven);
        if (query == null) return BadRequest("Oven inválido");

        // 1. Calcular tiempos del turno de forma simple
        TimeSpan horaActual = DateTime.Now.TimeOfDay;
        TimeSpan inicioTurno = (horaActual >= new TimeSpan(7, 0, 0) && horaActual < new TimeSpan(15, 30, 0)) ? new TimeSpan(7, 0, 0)
                             : (horaActual >= new TimeSpan(15, 30, 0) && horaActual <= new TimeSpan(23, 59, 59)) ? new TimeSpan(15, 30, 0)
                             : new TimeSpan(0, 0, 0);

        TimeSpan finTurno = (inicioTurno == new TimeSpan(7, 0, 0)) ? new TimeSpan(15, 30, 0)
                          : (inicioTurno == new TimeSpan(15, 30, 0)) ? new TimeSpan(23, 59, 59)
                          : new TimeSpan(7, 0, 0);

        DateTime hoy = DateTime.Today;

        // 2. OPTIMIZACIÓN CRÍTICA: 
        // Usamos AsNoTracking para no saturar la memoria de Entity Framework.
        // Usamos Select para traer exclusivamente las 3 propiedades que ocupa la gráfica.
        var datosGrafica = await query
            .AsNoTracking()
            .Where(x => x.Date.Date == hoy && x.Hors >= inicioTurno && x.Hors <= finTurno)
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Hors)
            .Select(x => new { x.Hors, x.Pess, x.Temp })
            .ToListAsync();

        // 3. ESTRATEGIA DE RENDIMIENTO: 
        // Si la máquina genera demasiados datos (ej. más de 100 puntos por turno),
        // tomamos una muestra representativa (ej. los últimos 60 puntos) para que la gráfica vuele.
        if (datosGrafica.Count > 100)
        {
            datosGrafica = datosGrafica.Skip(datosGrafica.Count - 60).ToList();
        }

        // Proyectamos el JSON final formateando la hora como lo requiere tu JS (.substring(0,5))
        var resultado = datosGrafica.Select(x => new
        {
            hors = x.Hors.ToString().Substring(0, 5),
            pess = x.Pess,
            temp = x.Temp
        });

        return Json(resultado);
    }


    // Método auxiliar para mapear el número de horno al DbSet correspondiente usando la clase base
    private IQueryable<OvenBase> GetOvenDbSet(int oven)
    {
        return oven switch
        {
            1 => _context.Oven1,
            2 => _context.Oven2,
            3 => _context.Oven3,
            4 => _context.Oven4,
            5 => _context.Oven5,
            6 => _context.Oven6,
            _ => null
        };
    }

    // 1. TU MÉTODO ORIGINAL (Intacto, no le cambies nada)
    [HttpGet]
    public async Task<IActionResult> GettLatest(int oven)
    {
        var query = GetOvenDbSet(oven);
        if (query == null) return BadRequest("Oven inválido");

        // OPTIMIZACIÓN: Buscamos directamente por el ID más alto en lugar de ordenar por fecha/hora
        var lastRecord = await query
            .OrderByDescending(x => x.id) // Cambia 'id' por 'Id' según tenga tu modelo
            .FirstOrDefaultAsync();

        if (lastRecord == null)
        {
            return Json(new { pess = "0", temp = "0", hors = "00:00" });
        }

        return Json(lastRecord);
    }


    // 2. NUEVO ENDPOINT EXCLUSIVO PARA CONTAR CICLOS
    [HttpGet]
    public async Task<IActionResult> GetTurnCycles(int oven)
    {
        var query = GetOvenDbSet(oven);
        if (query == null) return Json(0);

        try
        {
            TimeSpan horaActual = DateTime.Now.TimeOfDay;
            TimeSpan inicioTurno = (horaActual >= new TimeSpan(7, 0, 0) && horaActual < new TimeSpan(15, 30, 0)) ? new TimeSpan(7, 0, 0)
                                 : (horaActual >= new TimeSpan(15, 30, 0) && horaActual <= new TimeSpan(23, 59, 59)) ? new TimeSpan(15, 30, 0)
                                 : new TimeSpan(0, 0, 0);

            TimeSpan finTurno = (inicioTurno == new TimeSpan(7, 0, 0)) ? new TimeSpan(15, 30, 0)
                              : (inicioTurno == new TimeSpan(15, 30, 0)) ? new TimeSpan(23, 59, 59)
                              : new TimeSpan(7, 0, 0);

            DateTime hoy = DateTime.Today;
            var historial = await query
                .Where(x => x.Date.Date == hoy && x.Hors >= inicioTurno && x.Hors <= finTurno)
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Hors)
                .Select(x => x.Pess)
                .ToListAsync();

            int totalCiclos = 0;
            if (historial != null && historial.Count > 0)
            {
                bool estabaMasDe50 = false;
                bool primerRegistro = true;

                foreach (var pessTexto in historial)
                {
                    if (double.TryParse(pessTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out double presionActual))
                    {
                        if (primerRegistro)
                        {
                            estabaMasDe50 = presionActual > 50;
                            primerRegistro = false;
                            continue;
                        }

                        if (estabaMasDe50 && presionActual < 50)
                        {
                            totalCiclos++;
                            estabaMasDe50 = false;
                        }
                        else if (!estabaMasDe50 && presionActual > 50)
                        {
                            estabaMasDe50 = true;
                        }
                    }
                }
            }
            return Json(totalCiclos);
        }
        catch
        {
            return Json(0); // Si falla el cálculo, regresa 0 de forma segura
        }
    }

}