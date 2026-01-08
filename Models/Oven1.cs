namespace GraficasMixing.Models
{
    public class Oven1 : OvenBase
    {
        public int id { get; set; }
        public string Pess { get; set; }
        public string Temp { get; set; } 
        public DateTime Date { get; set; }
        public TimeSpan Hors { get; set; }
    }
}
