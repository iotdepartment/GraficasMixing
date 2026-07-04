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
        private readonly ExtruderContext _extruderContext;

        public ExtruderController(GaficadoreTestContext context, ExtruderContext extruderContext)
        {
            _context = context;
            _extruderContext = extruderContext;
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
            // 1. Definir los límites estrictos de la jornada (7:00 AM hoy hasta 6:59 AM de mañana)
            DateTime fechaHoy = date.Date;
            DateTime fechaManana = date.Date.AddDays(1);

            // 2. Realizar una ÚNICA consulta consolidada con AsNoTracking agrupando en la Base de Datos
            var datosTurnos = _context.ScadaExtrudermaster
                .AsNoTracking() // 👈 CLAVE: Evita cargar los objetos en el gestor de memoria de EF Core
                .Where(x => x.Extruder == extruder
                         // Filtro nativo traducible: Es hoy en horario operativo O es mañana en la madrugada
                         && ((x.Fecha.Date == fechaHoy && x.Hora >= new TimeSpan(7, 0, 0))
                          || (x.Fecha.Date == fechaManana && x.Hora < new TimeSpan(7, 0, 0))))
                .Select(x => new
                {
                    // Clasificación de turnos en base a la hora
                    Turno = x.Hora >= new TimeSpan(7, 0, 0) && x.Hora < new TimeSpan(15, 0, 0) ? "Turno 1" :
                            x.Hora >= new TimeSpan(15, 0, 0) && x.Hora <= new TimeSpan(23, 59, 59) ? "Turno 2" : "Turno 3",
                    IsProduciendo = x.Totm == 1 ? 1 : 0
                })
                .GroupBy(x => x.Turno)
                .Select(g => new
                {
                    Turno = g.Key,
                    TotalRegistros = g.Count(),
                    RegistrosProduccion = g.Sum(x => x.IsProduciendo)
                })
                .ToList(); // 🔹 Único viaje de ida y vuelta al servidor de base de datos

            // 3. Mapear y asegurar la estructura fija de los 3 turnos en el retorno
            var turnosEstructurales = new[] { "Turno 1", "Turno 2", "Turno 3" };

            return turnosEstructurales.Select(t =>
            {
                var datos = datosTurnos.FirstOrDefault(d => d.Turno == t);

                // Si no hay datos (ej. día festivo), el porcentaje es 0 de forma segura
                double porcentaje = (datos == null || datos.TotalRegistros == 0)
                    ? 0
                    : (datos.RegistrosProduccion * 100.0 / datos.TotalRegistros);

                return new ChartPoint
                {
                    Label = t,
                    Value = Math.Round(porcentaje, 2) // Retornamos el valor formateado a dos decimales
                };
            }).ToList();
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
            // 1. Límites estrictos del día para filtros booleanos indexados
            DateTime fechaHoy = date.Date;
            DateTime fechaManana = date.Date.AddDays(1);

            TimeSpan horaInicio = new TimeSpan(7, 0, 0);
            TimeSpan horaFin = new TimeSpan(6, 59, 59);
            bool esTurnoFijo = true;

            if (shift == "Turno1")
            {
                horaInicio = new TimeSpan(7, 0, 0);
                horaFin = new TimeSpan(15, 0, 0);
            }
            else if (shift == "Turno2")
            {
                horaInicio = new TimeSpan(15, 0, 0);
                horaFin = new TimeSpan(23, 59, 59);
            }
            else if (shift == "Turno3")
            {
                horaInicio = new TimeSpan(0, 0, 0);
                horaFin = new TimeSpan(7, 0, 0);
            }
            else if (shift == "All")
            {
                esTurnoFijo = false;
            }
            else
            {
                return new EficienciaRealResult { Familias = new List<ChartPoint>() };
            }

            // 2. Definir la consulta base filtrada con AsNoTracking para máxima velocidad de lectura
            var queryBase = _context.ScadaExtrudermaster
                .AsNoTracking() // 👈🔥 CLAVE: Deshabilita el tracking de memoria intermedia de EF Core
                .Where(x => x.Extruder == extruder);

            if (esTurnoFijo)
            {
                DateTime fechaFiltro = shift == "Turno3" ? fechaManana : fechaHoy;
                queryBase = queryBase.Where(x => x.Fecha.Date == fechaFiltro && x.Hora >= horaInicio && x.Hora < horaFin);
            }
            else
            {
                queryBase = queryBase.Where(x => (x.Fecha.Date == fechaHoy && x.Hora >= new TimeSpan(7, 0, 0))
                                              || (x.Fecha.Date == fechaManana && x.Hora < new TimeSpan(7, 0, 0)));
            }

            // 3. 🧠 UNA SOLA CONSULTA SQL: Agrupamos por familia trayendo todos los contadores a la vez
            var datosAgrupadosBD = queryBase
                .GroupBy(x => x.Familia)
                .Select(g => new
                {
                    Familia = g.Key,
                    TotalRegistros = g.Count(), // Para acumular el total general del turno
                    OperacionRegistros = g.Count(x => x.Totm == 1), // Para acumular el tiempo operativo
                    MetrosProducidos = g.Sum(x => x.Metros >= 0 ? (x.Metros ?? 0) : 0),
                    VelocidadPromedio = g.Average(v => v.Velocidad > 0 ? (v.Velocidad ?? 0) : (double?)null) ?? 0
                })
                .ToList(); // 🔹 Único viaje de ida y vuelta a la base de datos

            if (!datosAgrupadosBD.Any())
            {
                return new EficienciaRealResult { Familias = new List<ChartPoint>() };
            }

            // 4. Extraer los métricos globales sumando los totales ya pre-calculados en memoria
            double totalMinutos = datosAgrupadosBD.Sum(x => x.TotalRegistros);
            double operacionMinutos = datosAgrupadosBD.Sum(x => x.OperacionRegistros);
            double calcEfficiency = totalMinutos > 0 ? (operacionMinutos * 100.0 / totalMinutos) : 0;

            // 5. Filtrar las familias que sí produjeron metros para hacer el Left Join local de SetPoints
            var familiasConMetros = datosAgrupadosBD.Where(f => f.MetrosProducidos > 0).ToList();

            var produccionFamilias = (from f in familiasConMetros
                                      join sp in _context.SetPointExtruder.AsNoTracking()
                                      on new { Fam = f.Familia, Ext = extruder } equals new { Fam = sp.familia, Ext = sp.extruder } into spJoin
                                      from subSp in spJoin.DefaultIfEmpty()
                                      select new
                                      {
                                          f.Familia,
                                          f.VelocidadPromedio,
                                          Setpoint = subSp != null ? subSp.setpoint : 0d
                                      })
                                      .Select(f =>
                                      {
                                          double porcentajeEfectividad = (f.Setpoint > 0)
                                              ? (f.VelocidadPromedio * 100.0 / f.Setpoint)
                                              : 0;

                                          return new ChartPoint
                                          {
                                              Label = string.IsNullOrEmpty(f.Familia) ? "Sin Familia" : f.Familia,
                                              Value = Math.Round(porcentajeEfectividad, 2)
                                          };
                                      })
                                      .ToList();

            // 6. Cálculos matemáticos finales
            double promedioEfectividad = produccionFamilias.Count > 0 ? produccionFamilias.Average(x => x.Value) : 0;
            double eficienciaReal = (promedioEfectividad > 0) ? (calcEfficiency / promedioEfectividad) * 100 : 0;

            return new EficienciaRealResult
            {
                Familias = produccionFamilias,
                PromedioEfectividad = Math.Round(promedioEfectividad, 2),
                CalcEfficiency = Math.Round(calcEfficiency, 2),
                EficienciaReal = Math.Round(eficienciaReal, 2)
            };
        }

        private IEnumerable<object> CalcularDailyEficienciaRealLast7Days(string extruder, string shift, DateTime date)
        {
            DateTime endDate = date.Date;
            DateTime startDate = endDate.AddDays(-6);

            // 1. Traer TODOS los registros crudos de la semana usando filtros que SQL entiende nativamente
            var rawDataBD = _context.ScadaExtrudermaster
                .AsNoTracking() // Deshabilita el tracking para lecturas ultrarrápidas
                .Where(x => x.Extruder == extruder
                         // Filtro plano: El rango de fechas de los 7 días completos
                         && x.Fecha.Date >= startDate
                         && x.Fecha.Date <= endDate.AddDays(1))
                .Select(x => new
                {
                    x.Fecha,
                    x.Hora,
                    x.Velocidad,
                    x.Metros,
                    x.Totm,
                    x.Familia
                })
                .ToList(); // 🔹 Único viaje de ida y vuelta a la base de datos sin errores de traducción

            // 2. Procesar la lógica de turnos y agrupar por Fecha de Producción Real en memoria de C#
            var datosHistoricosAgrupados = rawDataBD
                .Select(x =>
                {
                    // Ajuste matemático de Jornada SCADA: de 12:00 AM a 6:59 AM pertenece al día de producción anterior
                    DateTime fechaProduccionReal = x.Hora < new TimeSpan(7, 0, 0)
                        ? x.Fecha.Date.AddDays(-1)
                        : x.Fecha.Date;

                    // Determinar el turno correspondiente del registro
                    string turnoRegistro = x.Hora >= new TimeSpan(7, 0, 0) && x.Hora < new TimeSpan(15, 0, 0) ? "Turno1" :
                                          x.Hora >= new TimeSpan(15, 0, 0) && x.Hora <= new TimeSpan(23, 59, 59) ? "Turno2" : "Turno3";

                    return new { x.Fecha, x.Hora, x.Velocidad, x.Metros, x.Totm, x.Familia, fechaProduccionReal, turnoRegistro };
                })
                // 🔹 Aplicamos el filtro de fecha límite estricto de la semana tras el ajuste de horas
                .Where(x => x.fechaProduccionReal >= startDate && x.fechaProduccionReal <= endDate)
                // 🔹 Si el usuario filtró por un turno específico, lo restringimos aquí en memoria
                .Where(x => shift == "All" || x.turnoRegistro == shift)
                .GroupBy(x => new { x.fechaProduccionReal, x.Familia })
                .Select(g => new
                {
                    g.Key.fechaProduccionReal,
                    g.Key.Familia,
                    TotalRegistros = g.Count(),
                    OperacionRegistros = g.Count(x => x.Totm == 1),
                    MetrosProducidos = g.Sum(x => x.Metros >= 0 ? (x.Metros ?? 0) : 0),
                    // AVG() en memoria simulando SQL ignorando nulos (valores menores o iguales a 0)
                    VelocidadPromedio = g.Where(v => v.Velocidad > 0).Select(v => v.Velocidad ?? 0).DefaultIfEmpty(0).Average()
                })
                .ToList();

            // 3. Descargar todos los SetPoints de este extrusor en un solo viaje para cruzarlos localmente
            var setPointsLocal = _context.SetPointExtruder
                .AsNoTracking()
                .Where(sp => sp.extruder == extruder)
                .ToList();

            var resultados = new List<object>();

            // 4. Construir la respuesta para la gráfica recorriendo los 7 días continuos en la RAM
            for (DateTime d = startDate; d <= endDate; d = d.AddDays(1))
            {
                var datosDelDia = datosHistoricosAgrupados.Where(x => x.fechaProduccionReal == d).ToList();

                double totalMinutos = datosDelDia.Sum(x => x.TotalRegistros);
                double operacionMinutos = datosDelDia.Sum(x => x.OperacionRegistros);
                double calcEfficiency = totalMinutos > 0 ? (operacionMinutos * 100.0 / totalMinutos) : 0;

                var familiasConMetros = datosDelDia.Where(f => f.MetrosProducidos > 0).ToList();

                var eficienciasFamilias = (from f in familiasConMetros
                                           join sp in setPointsLocal
                                           on f.Familia equals sp.familia into spJoin
                                           from subSp in spJoin.DefaultIfEmpty()
                                           select new
                                           {
                                               PorcentajeEfectividad = subSp != null && subSp.setpoint > 0
                                                   ? (f.VelocidadPromedio * 100.0 / subSp.setpoint)
                                                   : 0
                                           }).ToList();

                double promedioEfectividad = eficienciasFamilias.Count > 0 ? eficienciasFamilias.Average(x => x.PorcentajeEfectividad) : 0;
                double eficienciaReal = (promedioEfectividad > 0) ? (calcEfficiency / promedioEfectividad) * 100 : 0;

                resultados.Add(new
                {
                    label = d.ToString("yyyy-MM-dd"),
                    value = Math.Round(eficienciaReal, 2)
                });
            }

            return resultados;
        }

        private IEnumerable<object> CalcularFamilyRuntime(string extruder, DateTime date, string shift)
        {
            // 1. Definimos las fechas del día actual y siguiente para el SQL indexado
            DateTime fechaHoy = date.Date;
            DateTime fechaManana = date.Date.AddDays(1);

            TimeSpan horaInicio = new TimeSpan(7, 0, 0);
            TimeSpan horaFin = new TimeSpan(6, 59, 59);
            bool esTurnoFijo = true;

            // 2. Configurar las ventanas de tiempo por turno de manera nativa sin repetir consultas
            if (shift == "Turno1")
            {
                horaInicio = new TimeSpan(7, 0, 0);
                horaFin = new TimeSpan(15, 0, 0);
            }
            else if (shift == "Turno2")
            {
                horaInicio = new TimeSpan(15, 0, 0);
                horaFin = new TimeSpan(23, 59, 59);
            }
            else if (shift == "Turno3")
            {
                horaInicio = new TimeSpan(0, 0, 0);
                horaFin = new TimeSpan(7, 0, 0);
            }
            else if (shift == "All" || string.IsNullOrEmpty(shift))
            {
                esTurnoFijo = false; // Indicador de que abarcamos toda la jornada híbrida
            }
            else
            {
                return new List<object>();
            }

            // 3. Inicializar la consulta base con AsNoTracking filtrando solo registros operativos (Totm == 1)
            var queryBase = _context.ScadaExtrudermaster
                .AsNoTracking() // 👈 CLAVE: Evita cargar metadatos innecesarios en el gestor de EF Core
                .Where(x => x.Extruder == extruder && x.Totm == 1);

            // 4. Aplicar el rango de tiempo exacto traducible por el motor SQL
            if (esTurnoFijo)
            {
                DateTime fechaFiltro = shift == "Turno3" ? fechaManana : fechaHoy;
                queryBase = queryBase.Where(x => x.Fecha.Date == fechaFiltro && x.Hora >= horaInicio && x.Hora < horaFin);
            }
            else
            {
                // Filtro nativo unificado para "All Shifts" sin sumas complejas de columnas
                queryBase = queryBase.Where(x => (x.Fecha.Date == fechaHoy && x.Hora >= new TimeSpan(7, 0, 0))
                                                    || (x.Fecha.Date == fechaManana && x.Hora < new TimeSpan(7, 0, 0)));
            }

            // 5. Agrupación y conteo directo en la Base de Datos (1 registro = 1 minuto)
            var data = queryBase
                .GroupBy(x => x.Familia)
                .Select(g => new
                {
                    // Si el nombre de la familia es nulo o vacío, asignamos una etiqueta limpia
                    label = string.IsNullOrEmpty(g.Key) ? "Sin Familia" : g.Key,
                    value = g.Count() // minutos = total de registros en el SCADA
                })
                .OrderByDescending(x => x.value)
                .ToList(); // 🔹 Único viaje de ida y vuelta a la base de datos

            return data;
        }

        private IEnumerable<object> CalcularFamilyMeters(string extruder, DateTime date, string shift)
        {
            // 1. Definimos las fechas del día actual y siguiente para el SQL indexado
            DateTime fechaHoy = date.Date;
            DateTime fechaManana = date.Date.AddDays(1);

            TimeSpan horaInicio = new TimeSpan(7, 0, 0);
            TimeSpan horaFin = new TimeSpan(6, 59, 59);
            bool esTurnoFijo = true;

            // 2. Configurar las ventanas de tiempo por turno de manera nativa sin repetir código
            if (shift == "Turno1")
            {
                horaInicio = new TimeSpan(7, 0, 0);
                horaFin = new TimeSpan(15, 0, 0);
            }
            else if (shift == "Turno2")
            {
                horaInicio = new TimeSpan(15, 0, 0);
                horaFin = new TimeSpan(23, 59, 59);
            }
            else if (shift == "Turno3")
            {
                horaInicio = new TimeSpan(0, 0, 0);
                horaFin = new TimeSpan(7, 0, 0);
            }
            else if (shift == "All" || string.IsNullOrEmpty(shift))
            {
                esTurnoFijo = false; // Indicador de que abarcamos toda la jornada híbrida
            }
            else
            {
                return new List<object>();
            }

            // 3. Inicializar la consulta base con AsNoTracking ignorando valores negativos
            var queryBase = _context.ScadaExtrudermaster
                .AsNoTracking() // 👈 CLAVE: Deshabilita el tracking de memoria intermedia para reportes rápidos
                .Where(x => x.Extruder == extruder && x.Metros >= 0);

            // 4. Aplicar el rango de tiempo exacto traducible por el motor SQL
            if (esTurnoFijo)
            {
                DateTime fechaFiltro = shift == "Turno3" ? fechaManana : fechaHoy;
                queryBase = queryBase.Where(x => x.Fecha.Date == fechaFiltro && x.Hora >= horaInicio && x.Hora < horaFin);
            }
            else
            {
                // Filtro nativo unificado para "All Shifts" sin sumas complejas de columnas
                queryBase = queryBase.Where(x => (x.Fecha.Date == fechaHoy && x.Hora >= new TimeSpan(7, 0, 0))
                                                    || (x.Fecha.Date == fechaManana && x.Hora < new TimeSpan(7, 0, 0)));
            }

            // 5. Agrupación y sumatoria directa en el motor de la Base de Datos
            var data = queryBase
                .GroupBy(x => x.Familia)
                .Select(g => new
                {
                    // Si el nombre de la familia es nulo, asignamos una etiqueta limpia
                    label = string.IsNullOrEmpty(g.Key) ? "Sin Familia" : g.Key,
                    value = Math.Round(g.Sum(x => x.Metros ?? 0), 2) // Suma y redondeo a 2 decimales directamente en SQL
                })
                .OrderByDescending(x => x.value)
                .ToList(); // 🔹 Único viaje de ida y vuelta a la base de datos

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

        [HttpGet]
        public IActionResult GetEfficiencyByShift(string extruder, DateTime date)
        {
            return Json(CalcularEfficiencyByShift(extruder, date));
        }

        [HttpGet]
        public IActionResult GetDowntimeByShift(string extruder, DateTime date)
        {
            // 1. Definimos los rangos de fecha estrictos para el SQL
            DateTime fechaHoy = date.Date;
            DateTime fechaManana = date.Date.AddDays(1);

            // 2. Traer registros usando filtros separados que EF Core SÍ puede traducir a SQL
            var datosParos = _context.ScadaExtrudermaster
                .Where(x => x.Extruder == extruder
                         && x.Totm == 0
                         // Condición: Es hoy después de las 7:00 AM O es mañana antes de las 7:00 AM
                         && ((x.Fecha.Date == fechaHoy && x.Hora >= new TimeSpan(7, 0, 0))
                          || (x.Fecha.Date == fechaManana && x.Hora < new TimeSpan(7, 0, 0))))
                .Select(x => new
                {
                    x.Fecha,
                    x.Hora,
                    // Si PP es nulo o vacío en la BD, lo nombramos "No Especificado"
                    CodigoPP = string.IsNullOrEmpty(x.PP) ? "No Especificado" : x.PP
                })
                .ToList() // 🔹 Aquí ejecutamos una única consulta limpia en la base de datos

                // 3. Procesamos los turnos en memoria (Client Evaluation) de forma segura
                .Select(x => new
                {
                    Turno = x.Hora >= new TimeSpan(7, 0, 0) && x.Hora < new TimeSpan(15, 0, 0) ? "Turno 1" :
                            x.Hora >= new TimeSpan(15, 0, 0) && x.Hora <= new TimeSpan(23, 59, 59) ? "Turno 2" : "Turno 3",
                    x.CodigoPP
                })
                .GroupBy(x => new { x.Turno, x.CodigoPP })
                .Select(g => new
                {
                    g.Key.Turno,
                    g.Key.CodigoPP,
                    Minutos = g.Count() // Cada registro equivale a 1 minuto
                })
                .ToList();

            // 4. Formatear la respuesta estructurada para Chart.js Stacked Bar
            var todosLosTurnos = new[] { "Turno 1", "Turno 2", "Turno 3" };
            var todosLosCodigosPP = datosParos.Select(d => d.CodigoPP).Distinct().ToList();

            var resultadoFinal = todosLosCodigosPP.Select(pp => new
            {
                CodigoPP = pp,
                // Genera los minutos correspondientes a [Turno 1, Turno 2, Turno 3] en ese orden
                DatosPorTurno = todosLosTurnos.Select(t =>
                    datosParos.FirstOrDefault(d => d.Turno == t && d.CodigoPP == pp)?.Minutos ?? 0
                ).ToList()
            }).ToList();

            return Json(new { turnos = todosLosTurnos, desglose = resultadoFinal });
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
            var cadenaId = $"Extruder {id}";

            // 1. Datos de PUMASTER
            var ultimoPuMaster = _extruderContext.PUMASTER
                .Where(p => p.EXTRUDER == cadenaId)
                .OrderByDescending(p => p.FECHA)
                .ThenByDescending(p => p.HORA)
                .FirstOrDefault();

            // 2. 🔹 Devolvemos a ExtruderId que es el nombre real en tu clase C#
            var estado = _context.Estado
                .Include(e => e.Tubo1Ref)
                .Include(e => e.Tubo2Ref)
                .Include(e => e.CoverRef)
                .FirstOrDefault(e => e.ExtruderId == id);

            // 3. Mapeo al ViewModel
            var cardInfo = new DetalleExtruderViewModel
            {
                Extruder = ultimoPuMaster?.EXTRUDER ?? $"Extruder {id}",
                Empleado = ultimoPuMaster?.NOMBRE ?? "Sin Asignar",
                NumeroEmpleado = ultimoPuMaster?.NRO_EMPLEADO ?? "N/A",
                Mandril = ultimoPuMaster?.MANDRIL ?? "N/A",
                Familia = ultimoPuMaster?.FAMILIA ?? "N/A",

                // 🔹 Forzamos la asignación del contador directo si el registro existe
                Contador = estado != null ? estado.Contador : 0,

                Tubo1 = estado?.Tubo1Ref?.Batch ?? "N/A",
                Tubo2 = estado?.Tubo2Ref?.Batch ?? "N/A",
                Cover = estado?.CoverRef?.Batch ?? "N/A"
            };

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
        public IActionResult GetContador(int extruderId)
        {
            // Buscamos el registro en la tabla Estado por su ExtruderId (el ID limpio 1, 2, 3...)
            var estado = _context.Estado.FirstOrDefault(e => e.ExtruderId == extruderId);

            // Devolvemos el valor del contador en formato JSON
            return Json(new { contador = estado?.Contador ?? 0 });
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