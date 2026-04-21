using ClosedXML.Excel;
using GraficasMixing.Models;
using GraficasMixing.Utils;
using GraficasMixing.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;




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
            // Normalizar extruders desde ScadaExtrudermaster
            var extruders = _context.ScadaExtrudermaster
                .Where(r => !string.IsNullOrEmpty(r.Extruder))
                .Select(r => r.Extruder.Replace(" ", "").Trim()) // quitar espacios
                .Distinct()
                .OrderBy(e => e)
                .ToList();

            // Normalizar setpoints también
            var setpoints = _context.SetPointExtruder
                .Select(sp => new SetPointExtruder
                {
                    id = sp.id,
                    extruder = sp.extruder.Replace(" ", "").Trim(),
                    familia = sp.familia,
                    setpoint = sp.setpoint
                })
                .OrderBy(x => x.extruder)
                .ThenBy(x => x.familia)
                .ToList();

            ViewBag.Extruders = extruders;

            return View(setpoints);
        }

        [HttpGet]
        public IActionResult GetSetPointModal()
        {
            var extruders = _context.ScadaExtrudermaster
                .Where(r => !string.IsNullOrEmpty(r.Extruder))
                .Select(r => r.Extruder.Replace(" ", "").Trim())
                .Distinct()
                .OrderBy(e => e)
                .ToList();

            ViewBag.Extruders = extruders;

            var setpoints = _context.SetPointExtruder
                .Select(sp => new SetPointExtruder
                {
                    id = sp.id,
                    extruder = sp.extruder.Replace(" ", "").Trim(),
                    familia = sp.familia,
                    setpoint = sp.setpoint
                })
                .ToList();

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

        //TABLA EFICIENCIA
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


        [HttpGet]
        public IActionResult GetReportData(string extruder, DateTime date, string shift)
        {
            var report = new
            {
                EfficiencyByShift = CalcularEfficiencyByShift(extruder, date),
                DowntimeByShift = CalcularDowntimeByShift(extruder, date),
                EficienciaReal = CalcularEficienciaRealData(extruder, shift, date),
                FamilyRuntime = CalcularFamilyRuntime(extruder, date, shift),
                FamilyMeters = CalcularFamilyMeters(extruder, date, shift),
                SpeedData = CalcularSpeedData(extruder, date, shift),
                DailyEfficiency = CalcularDailyEficienciaRealLast7Days(extruder, shift, date)
            };

            return Json(report);
        }

        public class ChartPoint
        {
            public string Label { get; set; }
            public double Value { get; set; }
        }

        public class EficienciaRealResult
        {
            public List<ChartPoint> Familias { get; set; }
            public double PromedioEfectividad { get; set; }
            public double CalcEfficiency { get; set; }
            public double EficienciaReal { get; set; }
        }

        private List<ChartPoint> CalcularEfficiencyByShift(string extruder, DateTime date)
        {

            var turno1 = _context.ScadaExtrudermaster.Where(x => x.Extruder == extruder && x.Fecha.Date == date.Date && x.Hora >= new TimeSpan(7, 0, 0) && x.Hora < new TimeSpan(15, 0, 0));
            var turno2 = _context.ScadaExtrudermaster.Where(x => x.Extruder == extruder && x.Fecha.Date == date.Date && x.Hora >= new TimeSpan(15, 0, 0) && x.Hora <= new TimeSpan(23, 59, 59));
            var turno3 = _context.ScadaExtrudermaster.Where(x => x.Extruder == extruder && x.Fecha.Date == date.AddDays(1).Date && x.Hora < new TimeSpan(7, 0, 0));

            double CalcEfficiency(IQueryable<ScadaExtrudermaster> q) =>
                q.Count() == 0 ? 0 : (q.Count(x => x.Totm == 1) * 100.0 / q.Count());

            return new List<ChartPoint>
                {
                    new ChartPoint { Label = "Turno1", Value = CalcEfficiency(turno1) },
                    new ChartPoint { Label = "Turno2", Value = CalcEfficiency(turno2) },
                    new ChartPoint { Label = "Turno3", Value = CalcEfficiency(turno3) }
                };

        }


        private List<ChartPoint> CalcularDowntimeByShift(string extruder, DateTime date)
        {

            var turno1 = _context.ScadaExtrudermaster.Where(x => x.Extruder == extruder && x.Fecha.Date == date.Date && x.Hora >= new TimeSpan(7, 0, 0) && x.Hora < new TimeSpan(15, 0, 0));
            var turno2 = _context.ScadaExtrudermaster.Where(x => x.Extruder == extruder && x.Fecha.Date == date.Date && x.Hora >= new TimeSpan(15, 0, 0) && x.Hora <= new TimeSpan(23, 59, 59));
            var turno3 = _context.ScadaExtrudermaster.Where(x => x.Extruder == extruder && x.Fecha.Date == date.AddDays(1).Date && x.Hora < new TimeSpan(7, 0, 0));

            double CalcDowntime(IQueryable<ScadaExtrudermaster> q) =>
                q.Count() == 0 ? 0 : (q.Count(x => x.Totm == 0) * 100.0 / q.Count());

            return new List<ChartPoint>
            {
                new ChartPoint { Label = "Turno1", Value = CalcDowntime(turno1) },
                new ChartPoint { Label = "Turno2", Value = CalcDowntime(turno2) },
                new ChartPoint { Label = "Turno3", Value = CalcDowntime(turno3) }
            };

        }

        private EficienciaRealResult CalcularEficienciaRealData(string extruder, string shift, DateTime date)
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

            var grouped = query.GroupBy(x => x.Familia);

            var produccionFamilias = grouped
                .Select(g =>
                {
                    var metrosProducidos = g.Sum(x => x.Metros ?? 0);
                    if (metrosProducidos <= 0) return null;

                    var velocidadPromedio = g.Where(v => v.Velocidad > 0)
                                             .Select(v => v.Velocidad ?? 0)
                                             .DefaultIfEmpty(0)
                                             .Average();

                    var setpoint = _context.SetPointExtruder
                        .Where(sp => sp.familia == g.Key && sp.extruder == extruder)
                        .Select(sp => sp.setpoint)
                        .FirstOrDefault();

                    var porcentajeEfectividad = (setpoint > 0)
                        ? (velocidadPromedio * 100 / setpoint)
                        : 0;

                    return new ChartPoint { Label = g.Key, Value = porcentajeEfectividad };
                })
                .Where(x => x != null)
                .ToList();

            double promedioEfectividad = produccionFamilias.Count > 0 ? produccionFamilias.Average(x => x.Value) : 0;
            double total = query.Count();
            double operacion = query.Count(x => x.Totm == 1);
            double calcEfficiency = total > 0 ? (operacion * 100.0 / total) : 0;
            double eficienciaReal = (promedioEfectividad > 0) ? (calcEfficiency / promedioEfectividad) * 100 : 0;

            return new EficienciaRealResult
            {
                Familias = produccionFamilias,
                PromedioEfectividad = promedioEfectividad,
                CalcEfficiency = calcEfficiency,
                EficienciaReal = eficienciaReal
            };
        }

        private IEnumerable<object> CalcularDailyEficienciaRealLast7Days(string extruder, string shift, DateTime date)
        {
            DateTime endDate = date.Date;
            DateTime startDate = endDate.AddDays(-6);

            var resultados = new List<object>();

            for (DateTime d = startDate; d <= endDate; d = d.AddDays(1))
            {
                double eficienciaReal = CalcularEficienciaReal(extruder, shift, d);

                resultados.Add(new
                {
                    label = d.ToString("yyyy-MM-dd"),
                    value = eficienciaReal
                });
            }

            return resultados;
        }

        private IEnumerable<object> CalcularFamilyRuntime(string extruder, DateTime date, string shift)
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
                .Select(g => new { label = g.Key, value = g.Count() }) // minutos = count
                .OrderByDescending(x => x.value)
                .ToList();

            return data;
        }

        private IEnumerable<object> CalcularFamilyMeters(string extruder, DateTime date, string shift)
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
                .Select(g => new { label = g.Key, value = g.Sum(x => x.Metros ?? 0) })
                .OrderByDescending(x => x.value)
                .ToList();

            return data;
        }

        private IEnumerable<object> CalcularSpeedData(string extruder, DateTime date, string shift)
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
                    label = (x.Fecha.Date + x.Hora).ToString("yyyy-MM-dd HH:mm:ss"),
                    value = x.Velocidad ?? 0,

                    // Si velocidad es 0 → usar PP, si no → usar Familia
                    family = (x.Velocidad ?? 0) == 0
                    ? (x.PP ?? "TIEMPO MUERTO NO IDENTIFICADO")
                    : (x.Familia ?? "FAMILIA NO IDENTIFICADA"),

                    // 👇 NUEVO: indicador real de PP
                    isPP = (x.Velocidad ?? 0) == 0
                })
                .ToList();

            return data;

            //.Select(x => new
            // {
            //     label = x.Fecha.ToString("yyyy-MM-dd") + " " + x.Hora.ToString(),
            //     value = x.Velocidad ?? 0,
            //     family = x.Familia
            // })
        }


        [HttpPost]
        public IActionResult GenerateExcelAllExtruders([FromBody] ExportRequest request)
        {
            using var workbook = new XLWorkbook();

            DateTime date = DateTime.Parse(request.Date);

            // 🔹 Lista de extruders que quieres incluir en el reporte
            var extruders = new List<string> { "Extruder1", "Extruder3", "Extruder4", "Extruder5", "Extruder6" };

            foreach (var extruder in extruders)
            {
                var ws = workbook.Worksheets.Add(extruder);

                // Cabecera
                ws.Cell(1, 1).Value = "Extruder";
                ws.Cell(1, 2).Value = extruder;
                ws.Cell(2, 1).Value = "Shift";
                ws.Cell(2, 2).Value = request.Shift ?? "-";
                ws.Cell(3, 1).Value = "Date";
                ws.Cell(3, 2).Value = request.Date ?? "-";

                // 🔹 Obtener datos
                var efficiencyData = CalcularEfficiencyByShift(extruder, date);
                var downtimeData = CalcularDowntimeByShift(extruder, date);
                var eficienciaReal = CalcularEficienciaRealData(extruder, "All", date);
                var dailyEffData = CalcularDailyEficienciaRealLast7Days(extruder, request.Shift, date);
                var runtimeData = CalcularFamilyRuntime(extruder, date, request.Shift);
                var metersData = CalcularFamilyMeters(extruder, date, request.Shift);
                var speedData = CalcularSpeedData(extruder, date, request.Shift);

                // 🔹 Preparar labels y valores
                var labelsEff = efficiencyData.Select(r => r.Label).ToList();
                var valuesEff = efficiencyData.Select(r => r.Value).ToList();

                var labelsDown = downtimeData.Select(r => r.Label).ToList();
                var valuesDown = downtimeData.Select(r => r.Value).ToList();

                var labelsPie = new List<string> { "Eficiencia Real", "Resto" };
                var valuesPie = new List<double> { eficienciaReal.EficienciaReal, 100 - eficienciaReal.EficienciaReal };

                var labelsDaily = dailyEffData.Select(r => r.GetType().GetProperty("label").GetValue(r).ToString()).ToList();
                var valuesDaily = dailyEffData.Select(r => Convert.ToDouble(r.GetType().GetProperty("value").GetValue(r))).ToList();

                var labelsRuntime = runtimeData.Select(r => r.GetType().GetProperty("label").GetValue(r).ToString()).ToList();
                var valuesRuntime = runtimeData.Select(r => Convert.ToDouble(r.GetType().GetProperty("value").GetValue(r))).ToList();

                var labelsMeters = metersData.Select(r => r.GetType().GetProperty("label").GetValue(r).ToString()).ToList();
                var valuesMeters = metersData.Select(r => Convert.ToDouble(r.GetType().GetProperty("value").GetValue(r))).ToList();

                var labelsSpeed = speedData.Select(r => r.GetType().GetProperty("label").GetValue(r).ToString()).ToList();
                var valuesSpeed = speedData.Select(r => Convert.ToDouble(r.GetType().GetProperty("value").GetValue(r))).ToList();

                // 🔹 Generar gráficas
                byte[] barChartEfficiency = ChartHelper.GenerateBarChart(labelsEff, valuesEff, $"Eficiencia {extruder}");
                byte[] barChartDowntime = ChartHelper.GenerateBarChart(labelsDown, valuesDown, $"Downtime {extruder}");
                byte[] pieChartEficiencia = ChartHelper.GeneratePieChart(labelsPie, valuesPie, $"Eficiencia Real {extruder}");
                byte[] barChartDaily = ChartHelper.GenerateBarChart(labelsDaily, valuesDaily, $"Eficiencia Real últimos 7 días");
                byte[] barChartRuntime = ChartHelper.GenerateBarChart(labelsRuntime, valuesRuntime, $"Runtime por familia {extruder}");
                byte[] barChartMeters = ChartHelper.GenerateBarChart(labelsMeters, valuesMeters, $"Metros por familia {extruder}");

                // 🔹 Insertar gráficas en estilo grid con separación extra
                int row = 5;

                // Primera fila (3 gráficas lado a lado con espacio)
                ws.AddPicture(new MemoryStream(pieChartEficiencia)).MoveTo(ws.Cell(row, 1)).WithSize(500, 360);
                ws.AddPicture(new MemoryStream(barChartEfficiency)).MoveTo(ws.Cell(row, 10)).WithSize(500, 360);
                ws.AddPicture(new MemoryStream(barChartDowntime)).MoveTo(ws.Cell(row, 19)).WithSize(500, 360);

                row += 22;

                // Segunda fila (gráfica diaria ocupando todo el ancho)
                ws.AddPicture(new MemoryStream(barChartDaily)).MoveTo(ws.Cell(row, 1)).WithSize(1600, 360);

                row += 22;

                // Tercera fila (2 gráficas lado a lado con espacio)
                ws.AddPicture(new MemoryStream(barChartRuntime)).MoveTo(ws.Cell(row, 1)).WithSize(700, 360);
                ws.AddPicture(new MemoryStream(barChartMeters)).MoveTo(ws.Cell(row, 12)).WithSize(700, 360);
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileDate = string.IsNullOrEmpty(request.Date)
                ? DateTime.Now.ToString("yyyyMMdd")
                : request.Date;

            return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"Reporte_TodosExtruders_{fileDate}.xlsx");
        }

        ////////////////
        [HttpGet]
        public IActionResult GetEfficiencyByShift(string extruder, DateTime date)
        {
            return Json(CalcularEfficiencyByShift(extruder, date));
        }

        [HttpGet]
        public IActionResult GetDowntimeByShift(string extruder, DateTime date)
        {
            return Json(CalcularDowntimeByShift(extruder, date));
        }

        [HttpGet]
        public IActionResult GetEficienciaRealData(string extruder, string shift, DateTime date)
        {
            return Json(CalcularEficienciaRealData(extruder, shift, date));
        }

        private double CalcularEficienciaReal(string extruder, string shift, DateTime date)
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

            var grouped = query.GroupBy(x => x.Familia);

            var produccionFamilias = grouped
                .Where(g => g.Sum(x => x.Metros ?? 0) > 0)
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

                    return (setpoint > 0) ? (velocidadPromedio * 100 / setpoint) : 0;
                })
                .ToList();

            double promedioEfectividad = produccionFamilias.Count > 0
                ? produccionFamilias.Average()
                : 0;

            double total = query.Count();
            double operacion = query.Count(x => x.Totm == 1);
            double calcEfficiency = total > 0 ? (operacion * 100.0 / total) : 0;

            double eficienciaReal = (promedioEfectividad > 0)
                ? (calcEfficiency / promedioEfectividad) * 100
                : 0;

            return eficienciaReal;
        }


        [HttpGet]
        public IActionResult GetDailyEficienciaRealLast7Days(string extruder, string shift, DateTime date)
        {
            return Json(CalcularDailyEficienciaRealLast7Days(extruder, shift, date));
        }

        [HttpGet]
        public IActionResult GetFamilyRuntime(string extruder, DateTime date, string shift)
        {
            return Json(CalcularFamilyRuntime(extruder, date, shift));
        }

        [HttpGet]
        public IActionResult GetFamilyMeters(string extruder, DateTime date, string shift)
        {
            return Json(CalcularFamilyMeters(extruder, date, shift));
        }

        [HttpGet]
        public IActionResult GetSpeedData(string extruder, DateTime date, string shift)
        {
            return Json(CalcularSpeedData(extruder, date, shift));
        }

        public IActionResult Chart(int id)
        {
            var estado = _context.Estado
                .Include(e => e.ExtruderRef)
                .Include(e => e.EmpleadoRef)
                .Include(e => e.MandrilRef)
                .Include(e => e.Tubo1Ref)
                .Include(e => e.Tubo2Ref)
                .Include(e => e.CoverRef)
                .FirstOrDefault(e => e.ExtruderId == id);

            if (estado == null)
            {
                return View(new EstadoCardViewModel
                {
                    Extruder = "N/A",
                    Empleado = "N/A",
                    NumeroEmpleado = "N/A",
                    Mandril = "N/A",
                    Familia = "N/A",
                    Contador = 0,
                    Tubo1 = "",
                    Tubo2 = "",
                    Cover = ""
                });
            }

            var cardInfo = new EstadoCardViewModel
            {
                Extruder = estado.ExtruderRef?.NombreExtruder,
                Empleado = estado.EmpleadoRef?.Nombre,
                NumeroEmpleado = estado.EmpleadoRef?.NumeroEmpleado.ToString(),
                Mandril = estado.MandrilRef?.NombreMandril,
                Familia = estado.MandrilRef?.Familia,
                Contador = estado.Contador,
                Tubo1 = estado.Tubo1Ref?.Batch,
                Tubo2 = estado.Tubo2Ref?.Batch,
                Cover = estado.CoverRef?.Batch
            };

            // 🔹 Pasamos también el id del extruder a la vista
            ViewBag.ExtruderId = id;

            return View(cardInfo);
        }

        [HttpGet]
        public IActionResult GetFotoEmpleado(int numeroEmpleado)
        {
            // Ruta base donde están las fotos
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "tm");

            // Nombre del archivo = NumeroEmpleado.png
            var fileName = $"{numeroEmpleado}.png";
            var fullPath = Path.Combine(basePath, fileName);

            if (!System.IO.File.Exists(fullPath))
            {
                // Si no existe la foto, usar la imagen predeterminada thumbnail.png
                var defaultPath = Path.Combine(basePath, "thumbnail.png");
                var defaultImage = System.IO.File.OpenRead(defaultPath);
                return File(defaultImage, "image/png");
            }

            var image = System.IO.File.OpenRead(fullPath);
            return File(image, "image/png");
        }

        [HttpGet]
        public IActionResult GetDailyTotmData(int id)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
            var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);

            var parsedDate = nowLocal.Hour < 7 ? nowLocal.Date.AddDays(-1) : nowLocal.Date;
            var extruder = "Extruder" + id;

            var inicio = parsedDate.AddHours(7);
            var fin = parsedDate.AddDays(1).AddHours(6).AddMinutes(59);

            var registros = _context.ScadaExtrudermaster
                .Where(x => x.Extruder == extruder &&
                            (x.Fecha.Date == parsedDate || x.Fecha.Date == parsedDate.AddDays(1)))
                .AsEnumerable()
                .Where(x =>
                {
                    var fechaHora = x.Fecha.Date.Add(x.Hora);
                    return fechaHora >= inicio && fechaHora <= fin;
                })
                .ToList();

            var operacion = registros.Count(x => x.Totm == 1);
            var tiempoMuerto = registros.Count(x => x.Totm == 0);

            var data = new[]
            {
        new { label = "Operación", value = operacion },
        new { label = "Downtime", value = tiempoMuerto }
    };

            return Json(data);
        }

        [HttpGet]
        public JsonResult GetProductionVsDowntimeByShift(int id)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
            var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);

            var parsedDate = nowLocal.Hour < 7 ? nowLocal.Date.AddDays(-1) : nowLocal.Date;
            var extruder = "Extruder" + id;

            var turno1 = _context.ScadaExtrudermaster
                .Where(x => x.Extruder == extruder &&
                            x.Fecha.Date == parsedDate &&
                            x.Hora >= new TimeSpan(7, 0, 0) && x.Hora <= new TimeSpan(15, 29, 59))
                .ToList();

            var turno2 = _context.ScadaExtrudermaster
                .Where(x => x.Extruder == extruder &&
                            x.Fecha.Date == parsedDate &&
                            x.Hora >= new TimeSpan(15, 30, 0) && x.Hora <= new TimeSpan(23, 29, 59))
                .ToList();

            var turno3Parte1 = _context.ScadaExtrudermaster
                .Where(x => x.Extruder == extruder &&
                            x.Fecha.Date == parsedDate &&
                            x.Hora >= new TimeSpan(23, 30, 0) && x.Hora <= new TimeSpan(23, 59, 59))
                .ToList();

            var turno3Parte2 = _context.ScadaExtrudermaster
                .Where(x => x.Extruder == extruder &&
                            x.Fecha.Date == parsedDate.AddDays(1) &&
                            x.Hora >= new TimeSpan(0, 0, 0) && x.Hora <= new TimeSpan(6, 59, 59))
                .ToList();

            var turno3 = turno3Parte1.Concat(turno3Parte2).ToList();

            var data = new[]
            {
        new { turno = "Turno 1", produccion = turno1.Count(x => x.Totm == 1), downtime = turno1.Count(x => x.Totm == 0) },
        new { turno = "Turno 2", produccion = turno2.Count(x => x.Totm == 1), downtime = turno2.Count(x => x.Totm == 0) },
        new { turno = "Turno 3", produccion = turno3.Count(x => x.Totm == 1), downtime = turno3.Count(x => x.Totm == 0) }
    };

            return Json(data);
        }

        [HttpGet]
        public JsonResult GetSpeedHistoryByShift(int id)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
            var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);

            var fechaHoy = nowLocal.Hour < 7 ? nowLocal.Date.AddDays(-1) : nowLocal.Date;
            var extruder = "Extruder" + id;

            var inicio = fechaHoy.AddHours(7);
            var fin = fechaHoy.AddDays(1).AddHours(7);

            // 🔹 Obtener registros crudos
            var registrosRaw = _context.ScadaExtrudermaster
                .Where(x => x.Extruder == extruder &&
                            (x.Fecha.Date == fechaHoy || x.Fecha.Date == fechaHoy.AddDays(1)))
                .OrderBy(x => x.Fecha)
                .AsEnumerable()
                .Select(x => new
                {
                    fechaHora = x.Fecha.Date + x.Hora,
                    velocidad = x.Velocidad,
                    familia = x.Familia
                })
                .Where(x => x.fechaHora >= inicio && x.fechaHora < fin)
                .ToList();

            // 🔹 FIX: evitar duplicados en setpoints
            var setpoints = _context.SetPointExtruder
                .Where(sp => sp.extruder == extruder)
                .AsEnumerable()
                .GroupBy(sp => sp.familia)
                .ToDictionary(g => g.Key, g => g.First().setpoint);

            // 🔹 Construir registros finales
            var registros = registrosRaw.Select(x => new
            {
                hora = x.fechaHora.ToString("HH:mm"),
                velocidad = x.velocidad,
                turno =
                    x.fechaHora.TimeOfDay >= new TimeSpan(7, 0, 0) &&
                    x.fechaHora.TimeOfDay < new TimeSpan(15, 29, 59)
                        ? "Turno 1"
                        : x.fechaHora.TimeOfDay >= new TimeSpan(15, 30, 0) &&
                          x.fechaHora.TimeOfDay < new TimeSpan(23, 29, 59)
                            ? "Turno 2"
                            : "Turno 3",
                familia = x.familia,
                setpoint = setpoints.ContainsKey(x.familia) ? setpoints[x.familia] : 0
            }).ToList();

            return Json(new { registros });
        }

        [HttpGet]
        public JsonResult GetContador(int numeroEmpleado)
        {
            // Buscar el último estado asociado al empleado
            var estado = _context.Estado
                .Where(e => e.EmpleadoRef.NumeroEmpleado == numeroEmpleado)
                .OrderByDescending(e => e.ID)
                .FirstOrDefault();

            return Json(new
            {
                contador = estado?.Contador ?? 0
            });
        }

        public IActionResult GeneralChart()
        {

            return View();
        }

        public IActionResult GetSpeedDataToday1()
        {
            var extruder = "Extruder1";

            var tz = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            var ahoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

            var hoy = ahoraLocal.Date;
            var horaActual = ahoraLocal.TimeOfDay;

            IQueryable<ScadaExtrudermaster> query;

            if (horaActual >= new TimeSpan(7, 0, 0) && horaActual < new TimeSpan(15, 0, 0))
            {
                // Turno 1
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == hoy &&
                    x.Hora >= new TimeSpan(7, 0, 0) &&
                    x.Hora < new TimeSpan(15, 0, 0));
            }
            else if (horaActual >= new TimeSpan(15, 0, 0) && horaActual <= new TimeSpan(23, 59, 59))
            {
                // Turno 2
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == hoy &&
                    x.Hora >= new TimeSpan(15, 0, 0) &&
                    x.Hora <= new TimeSpan(23, 59, 59));
            }
            else
            {
                // Turno 3 (madrugada del mismo día)
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == hoy &&
                    x.Hora < new TimeSpan(7, 0, 0));
            }

            var data = query
                .OrderBy(x => x.Fecha)
                .ThenBy(x => x.Hora)
                .Select(x => new
                {
                    timestamp = x.Fecha.ToString("yyyy-MM-dd") + " " + x.Hora.ToString(@"hh\:mm"),
                    speed = x.Velocidad,
                    family = x.Familia
                })
                .ToList();

            return Json(data);
        }

        public IActionResult GetSpeedDataToday3()
        {
            var extruder = "Extruder3";

            var tz = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            var ahoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

            var hoy = ahoraLocal.Date;
            var horaActual = ahoraLocal.TimeOfDay;

            IQueryable<ScadaExtrudermaster> query;

            if (horaActual >= new TimeSpan(7, 0, 0) && horaActual < new TimeSpan(15, 0, 0))
            {
                // Turno 1
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == hoy &&
                    x.Hora >= new TimeSpan(7, 0, 0) &&
                    x.Hora < new TimeSpan(15, 0, 0));
            }
            else if (horaActual >= new TimeSpan(15, 0, 0) && horaActual <= new TimeSpan(23, 59, 59))
            {
                // Turno 2
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == hoy &&
                    x.Hora >= new TimeSpan(15, 0, 0) &&
                    x.Hora <= new TimeSpan(23, 59, 59));
            }
            else
            {
                // Turno 3 (madrugada del mismo día)
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == hoy &&
                    x.Hora < new TimeSpan(7, 0, 0));
            }

            var data = query
                .OrderBy(x => x.Fecha)
                .ThenBy(x => x.Hora)
                .Select(x => new
                {
                    timestamp = x.Fecha.ToString("yyyy-MM-dd") + " " + x.Hora.ToString(@"hh\:mm"),
                    speed = x.Velocidad,
                    family = x.Familia
                })
                .ToList();

            return Json(data);
        }

        public IActionResult GetSpeedDataToday4()
        {
            var extruder = "Extruder4";

            var tz = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            var ahoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

            var hoy = ahoraLocal.Date;
            var horaActual = ahoraLocal.TimeOfDay;

            IQueryable<ScadaExtrudermaster> query;

            if (horaActual >= new TimeSpan(7, 0, 0) && horaActual < new TimeSpan(15, 0, 0))
            {
                // Turno 1
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == hoy &&
                    x.Hora >= new TimeSpan(7, 0, 0) &&
                    x.Hora < new TimeSpan(15, 0, 0));
            }
            else if (horaActual >= new TimeSpan(15, 0, 0) && horaActual <= new TimeSpan(23, 59, 59))
            {
                // Turno 2
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == hoy &&
                    x.Hora >= new TimeSpan(15, 0, 0) &&
                    x.Hora <= new TimeSpan(23, 59, 59));
            }
            else
            {
                // Turno 3 (madrugada del mismo día)
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == hoy &&
                    x.Hora < new TimeSpan(7, 0, 0));
            }

            var data = query
                .OrderBy(x => x.Fecha)
                .ThenBy(x => x.Hora)
                .Select(x => new
                {
                    timestamp = x.Fecha.ToString("yyyy-MM-dd") + " " + x.Hora.ToString(@"hh\:mm"),
                    speed = x.Velocidad,
                    family = x.Familia
                })
                .ToList();

            return Json(data);
        }

        public IActionResult GetSpeedDataToday5()
        {
            var extruder = "Extruder5";

            var tz = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            var ahoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

            var hoy = ahoraLocal.Date;
            var horaActual = ahoraLocal.TimeOfDay;

            IQueryable<ScadaExtrudermaster> query;

            if (horaActual >= new TimeSpan(7, 0, 0) && horaActual < new TimeSpan(15, 0, 0))
            {
                // Turno 1
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == hoy &&
                    x.Hora >= new TimeSpan(7, 0, 0) &&
                    x.Hora < new TimeSpan(15, 0, 0));
            }
            else if (horaActual >= new TimeSpan(15, 0, 0) && horaActual <= new TimeSpan(23, 59, 59))
            {
                // Turno 2
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == hoy &&
                    x.Hora >= new TimeSpan(15, 0, 0) &&
                    x.Hora <= new TimeSpan(23, 59, 59));
            }
            else
            {
                // Turno 3 (madrugada del mismo día)
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == hoy &&
                    x.Hora < new TimeSpan(7, 0, 0));
            }

            var data = query
                .OrderBy(x => x.Fecha)
                .ThenBy(x => x.Hora)
                .Select(x => new
                {
                    timestamp = x.Fecha.ToString("yyyy-MM-dd") + " " + x.Hora.ToString(@"hh\:mm"),
                    speed = x.Velocidad,
                    family = x.Familia
                })
                .ToList();

            return Json(data);
        }

        public IActionResult GetSpeedDataToday6()
        {
            var extruder = "Extruder6";

            var tz = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            var ahoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

            var hoy = ahoraLocal.Date;
            var horaActual = ahoraLocal.TimeOfDay;

            IQueryable<ScadaExtrudermaster> query;

            if (horaActual >= new TimeSpan(7, 0, 0) && horaActual < new TimeSpan(15, 0, 0))
            {
                // Turno 1
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == hoy &&
                    x.Hora >= new TimeSpan(7, 0, 0) &&
                    x.Hora < new TimeSpan(15, 0, 0));
            }
            else if (horaActual >= new TimeSpan(15, 0, 0) && horaActual <= new TimeSpan(23, 59, 59))
            {
                // Turno 2
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == hoy &&
                    x.Hora >= new TimeSpan(15, 0, 0) &&
                    x.Hora <= new TimeSpan(23, 59, 59));
            }
            else
            {
                // Turno 3 (madrugada del mismo día)
                query = _context.ScadaExtrudermaster.Where(x =>
                    x.Extruder == extruder &&
                    x.Fecha.Date == hoy &&
                    x.Hora < new TimeSpan(7, 0, 0));
            }

            var data = query
                .OrderBy(x => x.Fecha)
                .ThenBy(x => x.Hora)
                .Select(x => new
                {
                    timestamp = x.Fecha.ToString("yyyy-MM-dd") + " " + x.Hora.ToString(@"hh\:mm"),
                    speed = x.Velocidad,
                    family = x.Familia
                })
                .ToList();

            return Json(data);
        }

    }
}