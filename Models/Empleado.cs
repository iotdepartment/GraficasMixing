using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraficasMixing.Models
{
    [Table("Empleados")]
    public class Empleado
    {
        [Key]
        public int ID { get; set; }

        [Column("Nombre")]
        public string Nombre { get; set; } // nvarchar(100)

        [Column("NumeroEmpleado")]
        public int NumeroEmpleado { get; set; } // int en la BD

        public ICollection<Estado> Estados { get; set; }
    }
}