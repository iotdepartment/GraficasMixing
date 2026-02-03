namespace GraficasMixing.Models
{
    public class ScadaExtrudermaster
    {
        public int Id { get; set; }

        public DateTime Fecha { get; set; }

        public TimeSpan Hora { get; set; }  // correcto si en SQL es tipo TIME

        public string? Extruder { get; set; }

        public string? Familia { get; set; }

        public double? Metros { get; set; }

        public double? Od { get; set; }

        public double? Pitch { get; set; }

        // Si es un flag de operación, mejor int o bool
        public int? Totm { get; set; }

        public double? Velocidad { get; set; }
    }
}