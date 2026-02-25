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


        [ForeignKey("ExtruderId")]
        public Extruder ExtruderRef { get; set; }

        [ForeignKey("EmpleadoId")]
        public Empleado EmpleadoRef { get; set; }

        [ForeignKey("MandrilId")]
        public Mandril MandrilRef { get; set; }


        [Column("Tubo1")]
        public int Tubo1 { get; set; }
        [ForeignKey("Tubo1")]
        public Materiales Tubo1Ref { get; set; }

        [Column("Tubo2")]
        public int Tubo2 { get; set; }
        [ForeignKey("Tubo2")]
        public Materiales Tubo2Ref { get; set; }

        [Column("Cover")]
        public int Cover { get; set; }
        [ForeignKey("Cover")]
        public Materiales CoverRef { get; set; }

    }
}