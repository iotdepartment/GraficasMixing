using GraficasMixing.Models;
using Microsoft.AspNetCore.Mvc;
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

        var datos = _context.KneaderM
                            .Where(x => x.Date.Date == hoy)   // solo registros del día actual
                            .OrderBy(x => x.Time)
                            .ToList();


        Console.WriteLine($"Registros de hoy: {datos.Count}");
        return View(datos);
    }

    public IActionResult Index1()
    {
        var hoy = DateTime.Today;

        var datos = _context.KneaderM
                            .Where(x => x.Date.Date == hoy)   // solo registros del día actual
                            .OrderBy(x => x.Time)
                            .ToList();

        Console.WriteLine($"Registros de hoy: {datos.Count}");
        return View(datos);
    }
}