namespace GraficasMixing.Models
{
    public class ScadaExtrudermaster
    {
        public int Id { get; set; }

        public DateTime Fecha { get; set; }

        public TimeSpan Hora { get; set; } 

        public string? Extruder { get; set; }

        public string? Familia { get; set; }

        public double? Metros { get; set; }

        public double? Od { get; set; }

        public double? Pitch { get; set; }

        public int? Totm { get; set; }

        public double? Velocidad { get; set; }
    }
}