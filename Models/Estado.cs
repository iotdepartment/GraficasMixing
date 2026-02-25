using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraficasMixing.Models
{
    [Table("Estado")]
    public class Estado
    {
        [Key]
        public int ID { get; set; }

        [Column("Extruder")]
        public int ExtruderId { get; set; }   // int en la BD

        [Column("Empleado")]
        public int EmpleadoId { get; set; }   // int en la BD

        [Column("Mandril")]
        public int MandrilId { get; set; }    // int en la BD

        [Column("Contador")]
        public int Contador { get; set; }

        // 🔹 Nuevos campos en la tabla
        [Column("Tubo1")]
        public int Tubo1 { get; set; }

        [Column("Tubo2")]
        public int Tubo2 { get; set; }

        [Column("Cover")]
        public int Cover { get; set; }

        [ForeignKey("ExtruderId")]
        public Extruder ExtruderRef { get; set; }

        [ForeignKey("EmpleadoId")]
        public Empleado EmpleadoRef { get; set; }

        [ForeignKey("MandrilId")]
        public Mandril MandrilRef { get; set; }
    }
}