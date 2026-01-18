using Microsoft.AspNetCore.Mvc;
using GraficasMixing.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace GraficasMixing.Controllers
{
    public class BatchOffController : Controller
    {
        private readonly GaficadoreTestContext _context;

        public BatchOffController(GaficadoreTestContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // ---------------------------------------------------------
        //  GET: /BatchOff/GetData?fecha=2025-01-18&turno=Turno1
        // ---------------------------------------------------------
        [HttpGet]
        public JsonResult GetData(DateTime fecha, string turno)
        {
            // 1. Obtener datos del día seleccionado
            var query = _context.BatchOff
                .Where(x => x.Fecha.Date == fecha.Date);

            // 2. Filtrar por turno (si no es "All")
            if (turno != "All")
            {
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
                        start = new TimeSpan(0, 0, 0);
                        end = new TimeSpan(6, 59, 59);
                        break;

                    default:
                        return Json(new { error = "Turno inválido" });
                }

                query = query.Where(x => x.Hora >= start && x.Hora <= end);
            }

            // 3. Ordenar y proyectar datos limpios
            var datos = query
                .OrderBy(x => x.Fecha)
                .ThenBy(x => x.Hora)
                .Select(x => new
                {
                    peso = x.Peso,
                    temperatura = x.Temperatura,
                    fecha = x.Fecha.ToString("yyyy-MM-dd"),
                    hora = x.Hora.ToString(@"hh\:mm\:ss")
                })
                .ToList();

            return Json(datos);
        }
    }
}