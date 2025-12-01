namespace GraficasMixing.Models
{

    public class KneaderM
    {
        public int Id { get; set; }
        public string Kneader { get; set; }
        public string Pressure { get; set; }
        public string Power { get; set; }
        public string Revolution { get; set; }
        public string Temperature { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
    }
}
