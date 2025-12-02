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

        // Traer todos los registros
        var todos = _context.KneaderM
                            .OrderBy(x => x.Date)
                            .ThenBy(x => x.Time)
                            .ToList();

        // Filtrar los de hoy para inicializar
        var deHoy = todos.Where(x => x.Date.Date == hoy).ToList();

        // Pasar ambos conjuntos a la vista
        ViewBag.Hoy = deHoy;
        return View(todos);
    }
}