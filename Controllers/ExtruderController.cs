using GraficasMixing.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace GraficasMixing.Controllers
{
    public class ExtruderController : Controller
    {
        private readonly GaficadoreTestContext _context;

        public ExtruderController(GaficadoreTestContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Extruders distintos
            var extruders = _context.ScadaExtrudermaster
                .Where(r => !string.IsNullOrEmpty(r.Extruder))
                .Select(r => r.Extruder)
                .Distinct()
                .OrderBy(e => e)
                .ToList();

            // Todos los setpoints
            var setpoints = _context.SetPointExtruder
                .OrderBy(x => x.extruder)
                .ThenBy(x => x.familia)
                .ToList();

            ViewBag.Extruders = extruders;

            // Pasamos la lista de setpoints como modelo
            return View(setpoints);
        }
        [HttpGet]
        public IActionResult GetSetPointModal()
        {
            // Traer todos los extruders distintos
            var extruders = _context.ScadaExtrudermaster
                .Where(r => !string.IsNullOrEmpty(r.Extruder))
                .Select(r => r.Extruder)
                .Distinct()
                .OrderBy(e => e)
                .ToList();

            ViewBag.Extruders = extruders;

            // Pasamos todos los setpoints para que el modal tenga la info
            var setpoints = _context.SetPointExtruder.ToList();
            return PartialView("Partials/_ExtruderModal", setpoints);
        }

        [HttpPost]
        public IActionResult SaveSetPoint(SetPointExtruder model)
        {
            if (ModelState.IsValid)
            {
                var existing = _context.SetPointExtruder.FirstOrDefault(x => x.id == model.id);
                if (existing != null)
                {
                    existing.setpoint = model.setpoint;
                    _context.SaveChanges();
                    return Json(new { success = true, message = "Setpoint actualizado correctamente" });
                }
            }
            return Json(new { success = false, message = "Error al guardar" });
        }

        [HttpGet]
        public IActionResult GetTotmData(string extruder, string shift, DateTime date)
        {
            // Base query
            IQueryable<ScadaExtrudermaster> query;

            switch (shift)
            {
                case "Turno1": // 07:00 - 15:00
                    query = _context.ScadaExtrudermaster
                        .Where(x => x.Extruder == extruder &&
                                    x.Fecha.Date == date.Date &&
                                    x.Hora >= new TimeSpan(7, 0, 0) &&
                                    x.Hora < new TimeSpan(15, 0, 0));
                    break;

                case "Turno2": // 15:00 - 23:59
                    query = _context.ScadaExtrudermaster
                        .Where(x => x.Extruder == extruder &&
                                    x.Fecha.Date == date.Date &&
                                    x.Hora >= new TimeSpan(15, 0, 0) &&
                                    x.Hora <= new TimeSpan(23, 59, 59));
                    break;

                case "Turno3": // 00:00 - 07:00 (pertenece al día siguiente en lógica de producción)
                    query = _context.ScadaExtrudermaster
                        .Where(x => x.Extruder == extruder &&
                                    x.Fecha.Date == date.AddDays(1).Date &&
                                    x.Hora >= new TimeSpan(0, 0, 0) &&
                                    x.Hora < new TimeSpan(7, 0, 0));
                    break;

                case "All": // Jornada completa: 07:00 del día seleccionado a 06:59 del siguiente día
                    var sameDay = _context.ScadaExtrudermaster
                        .Where(x => x.Extruder == extruder &&
                                    x.Fecha.Date == date.Date &&
                                    x.Hora >= new TimeSpan(7, 0, 0));

                    var nextDay = _context.ScadaExtrudermaster
                        .Where(x => x.Extruder == extruder &&
                                    x.Fecha.Date == date.AddDays(1).Date &&
                                    x.Hora < new TimeSpan(7, 0, 0));

                    query = sameDay.Concat(nextDay);
                    break;

                default:
                    query = Enumerable.Empty<ScadaExtrudermaster>().AsQueryable();
                    break;
            }

            // Contar explícitamente TOTM=1 y TOTM=0
            var totm1 = query.Count(x => x.Totm == 1);
            var totm0 = query.Count(x => x.Totm == 0);

            // Devolver siempre ambas categorías
            var data = new[]
            {
                new { Totm = 1, Count = totm1 },
                new { Totm = 0, Count = totm0 }
            };

            return Json(data);
        }
        [HttpGet]
        public IActionResult GetEfficiencyByShift(string extruder, DateTime date)
        {
            // Definir rangos de cada turno
            var turno1 = _context.ScadaExtrudermaster
                .Where(x => x.Extruder == extruder &&
                            x.Fecha.Date == date.Date &&
                            x.Hora >= new TimeSpan(7, 0, 0) &&
                            x.Hora < new TimeSpan(15, 0, 0));

            var turno2 = _context.ScadaExtrudermaster
                .Where(x => x.Extruder == extruder &&
                            x.Fecha.Date == date.Date &&
                            x.Hora >= new TimeSpan(15, 0, 0) &&
                            x.Hora <= new TimeSpan(23, 59, 59));

            var turno3 = _context.ScadaExtrudermaster
                .Where(x => x.Extruder == extruder &&
                            x.Fecha.Date == date.AddDays(1).Date &&
                            x.Hora < new TimeSpan(7, 0, 0));

            // Calcular eficiencia = TOTM=1 / total * 100
            double CalcEfficiency(IQueryable<ScadaExtrudermaster> q) =>
                q.Count() == 0 ? 0 : (q.Count(x => x.Totm == 1) * 100.0 / q.Count());

            var data = new[]
            {
        new { Shift = "Turno1", Efficiency = CalcEfficiency(turno1) },
        new { Shift = "Turno2", Efficiency = CalcEfficiency(turno2) },
        new { Shift = "Turno3", Efficiency = CalcEfficiency(turno3) }
    };

            return Json(data);
        }

        [HttpGet]
        public IActionResult GetDowntimeByShift(string extruder, DateTime date)
        {
            var turno1 = _context.ScadaExtrudermaster
                .Where(x => x.Extruder == extruder &&
                            x.Fecha.Date == date.Date &&
                            x.Hora >= new TimeSpan(7, 0, 0) &&
                            x.Hora < new TimeSpan(15, 0, 0));

            var turno2 = _context.ScadaExtrudermaster
                .Where(x => x.Extruder == extruder &&
                            x.Fecha.Date == date.Date &&
                            x.Hora >= new TimeSpan(15, 0, 0) &&
                            x.Hora <= new TimeSpan(23, 59, 59));

            var turno3 = _context.ScadaExtrudermaster
                .Where(x => x.Extruder == extruder &&
                            x.Fecha.Date == date.AddDays(1).Date &&
                            x.Hora < new TimeSpan(7, 0, 0));

            double CalcDowntime(IQueryable<ScadaExtrudermaster> q) =>
                q.Count() == 0 ? 0 : (q.Count(x => x.Totm == 0) * 100.0 / q.Count());

            var data = new[]
            {
        new { shift = "Turno1", downtime = CalcDowntime(turno1) },
        new { shift = "Turno2", downtime = CalcDowntime(turno2) },
        new { shift = "Turno3", downtime = CalcDowntime(turno3) }
    };

            return Json(data);
        }

        [HttpGet]
        public IActionResult GetDailyEfficiencyLast7Days(string extruder, DateTime date)
        {
            // Tomamos la fecha seleccionada como referencia
            DateTime endDate = date.Date;
            DateTime startDate = endDate.AddDays(-6); // últimos 7 días hacia atrás desde la fecha seleccionada

            var data = _context.ScadaExtrudermaster
                .Where(x => x.Extruder == extruder &&
                            x.Fecha.Date >= startDate &&
                            x.Fecha.Date <= endDate)
                .GroupBy(x => x.Fecha.Date)
                .Select(g => new
                {
                    date = g.Key,
                    efficiency = g.Count() == 0 ? 0 :
                                 (g.Count(x => x.Totm == 1) * 100.0 / g.Count())
                })
                .OrderBy(x => x.date)
                .ToList();

            return Json(data);
        }
        [HttpGet]
        public IActionResult GetFamilyRuntime(string extruder, DateTime date, string shift)
        {
            IQueryable<ScadaExtrudermaster> query;

            if (shift == "Turno1")
            {
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == date.Date &&
                    x.Hora >= new TimeSpan(7, 0, 0) &&
                    x.Hora < new TimeSpan(15, 0, 0) &&
                    x.Totm == 1);
            }
            else if (shift == "Turno2")
            {
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == date.Date &&
                    x.Hora >= new TimeSpan(15, 0, 0) &&
                    x.Hora <= new TimeSpan(23, 59, 59) &&
                    x.Totm == 1);
            }
            else if (shift == "Turno3")
            {
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == date.AddDays(1).Date &&
                    x.Hora < new TimeSpan(7, 0, 0) &&
                    x.Totm == 1);
            }
            else // ALL SHIFTS
            {
                var turno1 = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == date.Date &&
                    x.Hora >= new TimeSpan(7, 0, 0) &&
                    x.Hora < new TimeSpan(15, 0, 0) &&
                    x.Totm == 1);

                var turno2 = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == date.Date &&
                    x.Hora >= new TimeSpan(15, 0, 0) &&
                    x.Hora <= new TimeSpan(23, 59, 59) &&
                    x.Totm == 1);

                var turno3 = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == date.AddDays(1).Date &&
                    x.Hora < new TimeSpan(7, 0, 0) &&
                    x.Totm == 1);

                query = turno1.Concat(turno2).Concat(turno3);
            }

            var data = query
                .GroupBy(x => x.Familia)
                .Select(g => new
                {
                    family = g.Key,
                    minutes = g.Count()
                })
                .OrderByDescending(x => x.minutes)
                .ToList();

            return Json(data);
        }

        [HttpGet]
        public IActionResult GetFamilyMeters(string extruder, DateTime date, string shift)
        {
            IQueryable<ScadaExtrudermaster> query;

            if (shift == "Turno1")
            {
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == date.Date &&
                    x.Hora >= new TimeSpan(7, 0, 0) &&
                    x.Hora < new TimeSpan(15, 0, 0) &&
                    x.Metros >= 0);   // ← IGNORAR NEGATIVOS
            }
            else if (shift == "Turno2")
            {
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == date.Date &&
                    x.Hora >= new TimeSpan(15, 0, 0) &&
                    x.Hora <= new TimeSpan(23, 59, 59) &&
                    x.Metros >= 0);   // ← IGNORAR NEGATIVOS
            }
            else if (shift == "Turno3")
            {
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == date.AddDays(1).Date &&
                    x.Hora < new TimeSpan(7, 0, 0) &&
                    x.Metros >= 0);   // ← IGNORAR NEGATIVOS
            }
            else // ALL SHIFTS
            {
                var turno1 = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == date.Date &&
                    x.Hora >= new TimeSpan(7, 0, 0) &&
                    x.Hora < new TimeSpan(15, 0, 0) &&
                    x.Metros >= 0);

                var turno2 = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == date.Date &&
                    x.Hora >= new TimeSpan(15, 0, 0) &&
                    x.Hora <= new TimeSpan(23, 59, 59) &&
                    x.Metros >= 0);

                var turno3 = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == date.AddDays(1).Date &&
                    x.Hora < new TimeSpan(7, 0, 0) &&
                    x.Metros >= 0);

                query = turno1.Concat(turno2).Concat(turno3);
            }

            var data = query
                .GroupBy(x => x.Familia)
                .Select(g => new
                {
                    family = g.Key,
                    meters = g.Sum(x => x.Metros)
                })
                .OrderByDescending(x => x.meters)
                .ToList();

            return Json(data);
        }

        [HttpGet]
        public IActionResult GetSpeedData(string extruder, DateTime date, string shift)
        {
            IQueryable<ScadaExtrudermaster> query;

            if (shift == "Turno1")
            {
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == date.Date &&
                    x.Hora >= new TimeSpan(7, 0, 0) &&
                    x.Hora < new TimeSpan(15, 0, 0));
            }
            else if (shift == "Turno2")
            {
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == date.Date &&
                    x.Hora >= new TimeSpan(15, 0, 0) &&
                    x.Hora <= new TimeSpan(23, 59, 59));
            }
            else if (shift == "Turno3")
            {
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == date.AddDays(1).Date &&
                    x.Hora < new TimeSpan(7, 0, 0));
            }
            else // ALL SHIFTS
            {
                var turno1 = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == date.Date &&
                    x.Hora >= new TimeSpan(7, 0, 0) &&
                    x.Hora < new TimeSpan(15, 0, 0));

                var turno2 = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == date.Date &&
                    x.Hora >= new TimeSpan(15, 0, 0) &&
                    x.Hora <= new TimeSpan(23, 59, 59));

                var turno3 = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == date.AddDays(1).Date &&
                    x.Hora < new TimeSpan(7, 0, 0));

                query = turno1.Concat(turno2).Concat(turno3);
            }

            var data = query
                .OrderBy(x => x.Fecha)
                .ThenBy(x => x.Hora)
                .Select(x => new
                {
                    timestamp = x.Fecha.ToString("yyyy-MM-dd") + " " + x.Hora.ToString(),
                    speed = x.Velocidad,   // ← TU CAMPO REAL
                    family = x.Familia // ← aquí incluyes la familia
                })
                .ToList();

            return Json(data);
        }

        [HttpGet]
        public IActionResult GetProduccionFamilias(string extruder, string shift, DateTime date)
        {
            IEnumerable<ScadaExtrudermaster> query;

            switch (shift)
            {
                case "Turno1":
                    query = _context.ScadaExtrudermaster
                        .Where(x => x.Extruder == extruder &&
                                    x.Fecha.Date == date.Date &&
                                    x.Hora >= new TimeSpan(7, 0, 0) &&
                                    x.Hora < new TimeSpan(15, 0, 0) &&
                                    x.Metros >= 0)
                        .ToList();
                    break;

                case "Turno2":
                    query = _context.ScadaExtrudermaster
                        .Where(x => x.Extruder == extruder &&
                                    x.Fecha.Date == date.Date &&
                                    x.Hora >= new TimeSpan(15, 0, 0) &&
                                    x.Hora <= new TimeSpan(23, 59, 59) &&
                                    x.Metros >= 0)
                        .ToList();
                    break;

                case "Turno3":
                    query = _context.ScadaExtrudermaster
                        .Where(x => x.Extruder == extruder &&
                                    x.Fecha.Date == date.AddDays(1).Date &&
                                    x.Hora >= new TimeSpan(0, 0, 0) &&
                                    x.Hora < new TimeSpan(7, 0, 0) &&
                                    x.Metros >= 0)
                        .ToList();
                    break;

                case "All":
                    var turno1 = _context.ScadaExtrudermaster
                        .Where(x => x.Extruder == extruder &&
                                    x.Fecha.Date == date.Date &&
                                    x.Hora >= new TimeSpan(7, 0, 0) &&
                                    x.Hora < new TimeSpan(15, 0, 0) &&
                                    x.Metros >= 0)
                        .ToList();

                    var turno2 = _context.ScadaExtrudermaster
                        .Where(x => x.Extruder == extruder &&
                                    x.Fecha.Date == date.Date &&
                                    x.Hora >= new TimeSpan(15, 0, 0) &&
                                    x.Hora <= new TimeSpan(23, 59, 59) &&
                                    x.Metros >= 0)
                        .ToList();

                    var turno3 = _context.ScadaExtrudermaster
                        .Where(x => x.Extruder == extruder &&
                                    x.Fecha.Date == date.AddDays(1).Date &&
                                    x.Hora < new TimeSpan(7, 0, 0) &&
                                    x.Metros >= 0)
                        .ToList();

                    query = turno1.Concat(turno2).Concat(turno3);
                    break;

                default:
                    query = new List<ScadaExtrudermaster>();
                    break;
            }

            // Ahora trabajamos en memoria
            var grouped = query.GroupBy(x => x.Familia);

            var produccionFamilias = grouped
                .Select(g =>
                {
                    var velocidadPromedio = g
                        .Where(v => v.Velocidad > 0)
                        .Select(v => v.Velocidad ?? 0)
                        .DefaultIfEmpty(0)
                        .Average();
                    var setpoint = _context.SetPointExtruder
                        .Where(sp => sp.familia == g.Key && sp.extruder == extruder)
                        .Select(sp => sp.setpoint)
                        .FirstOrDefault();

                    return new ProduccionFamiliaViewModel
                    {
                        Familia = g.Key,
                        MetrosProducidos = g.Sum(x => x.Metros ?? 0),
                        VelocidadPromedio = velocidadPromedio,
                        SetPointVelocidad = setpoint,
                        PorcentajeEfectividad = (setpoint > 0) ? (velocidadPromedio * 100 / setpoint) : 0
                    };
                })
                .OrderByDescending(x => x.MetrosProducidos)
                .ToList();

            return Json(produccionFamilias);
        }

    }
}