public class PUMASTER
{
    //Id del registro
    public int ID { get; set; }

    //Datos generales del registro
    public string? NRO_EMPLEADO { get; set; }
    public string? NOMBRE { get; set; }
    public string? TURNO { get; set; }
    public DateTime FECHA { get; set; }
    public TimeSpan HORA { get; set; }
    public string? EXTRUDER { get; set; }
    public string? MANDRIL { get; set; }
    public string? FAMILIA { get; set; }
}
