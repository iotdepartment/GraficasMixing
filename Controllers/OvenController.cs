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

    [HttpGet]
    public async Task<IActionResult> GetTurnCycles(int oven)
    {
        var query = GetOvenDbSet(oven);
        if (query == null) return Json(0);

        try
        {
            // 1. Obtener la zona horaria de forma segura (Compatible con Windows, Linux y Docker)
            TimeZoneInfo tz;
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
            }

            var ahoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var hoy = ahoraLocal.Date;
            var horaActual = ahoraLocal.TimeOfDay;

            TimeSpan inicioTurno;
            TimeSpan finTurno;

            // 2. Tu lógica exacta de asignación de turnos
            // Turno 1 → 07:00 a 15:30
            if (horaActual >= new TimeSpan(7, 0, 0) && horaActual < new TimeSpan(15, 30, 0))
            {
                inicioTurno = new TimeSpan(7, 0, 0);
                finTurno = new TimeSpan(15, 30, 0);
            }
            // Turno 2 → 15:30 a 24:00
            else if (horaActual >= new TimeSpan(15, 30, 0) && horaActual <= new TimeSpan(23, 59, 59))
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

            // 3. Consulta Ultra Rápida a la Base de Datos
            // Usamos AsNoTracking para no saturar memoria y traemos EXCLUSIVAMENTE la columna Pess
            var presiones = await query
                .AsNoTracking()
                .Where(x => x.Date.Date == hoy &&
                            x.Hors >= inicioTurno &&
                            x.Hors <= finTurno)
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Hors)
                .Select(x => x.Pess)
                .ToListAsync();

            int totalCiclos = 0;
            int totalRegistros = presiones.Count;

            if (totalRegistros > 0)
            {
                double[] valores = new double[totalRegistros];
                for (int i = 0; i < totalRegistros; i++)
                {
                    double.TryParse(presiones[i], NumberStyles.Any, CultureInfo.InvariantCulture, out valores[i]);
                }

                // --- ALGORITMO POR ESTADOS CONFIGURADO A 500 KPA ---
                // Evaluamos si el horno arrancó el turno arriba de 500
                bool hornoEstabaArriba = valores[0] >= 500;

                for (int i = 1; i < totalRegistros; i++)
                {
                    double presionActual = valores[i];

                    // DETECCIÓN: El horno estaba trabajando (>500) y el ciclo terminó al caer por debajo de 500
                    if (hornoEstabaArriba && presionActual < 500)
                    {
                        totalCiclos++;
                        hornoEstabaArriba = false; // Cambia estado a abajo
                    }
                    // PREPARACIÓN: El horno volvió a cargar presión superando los 500 KPA
                    else if (!hornoEstabaArriba && presionActual >= 500)
                    {
                        hornoEstabaArriba = true; // Cambia estado a arriba
                    }
                }
            }

            return Json(totalCiclos);

        }
        catch (Exception ex)
        {
            // En caso de cualquier falla imprevista con la BD, retorna 0 de forma segura para no trabar la vista
            System.Diagnostics.Debug.WriteLine($"Error en GetTurnCycles: {ex.Message}");
            return Json(0);
        }
    }
}