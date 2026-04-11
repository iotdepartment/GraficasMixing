using System.Text.Json;

namespace GraficasMixing.Models
{
    public class ExportRequest
    {
        public string Extruder { get; set; }
        public string Shift { get; set; }
        public string Date { get; set; }
        public JsonElement Report { get; set; }   // 👈 en lugar de object
        public List<ChartImage> Charts { get; set; }
    }

    public class ChartImage
    {
        public string Name { get; set; }   // Ejemplo: "Eficiencia por turno"
        public string Image { get; set; }  // Base64 del canvas
    }
}