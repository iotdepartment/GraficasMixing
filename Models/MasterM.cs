namespace GraficasMixing.Models
{
    public class MasterM
    {
        public int ID { get; set; }
        public int ExtruderID { get; set; }
        public DateTime EventTime { get; set; }
        public DateTime FechaAjustada { get; set; }
        public string NR { get; set; }
        public Double VelocidadRPM { get; set; }
    }
}
