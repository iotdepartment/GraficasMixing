using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraficasMixing.Models
{
    [Table("Extruders")]
    public class Extruder
    {
        [Key]
        public int ID { get; set; }

        [Column("Extruder")]
        public string NombreExtruder { get; set; } // nvarchar(100)

        public ICollection<Estado> Estados { get; set; }
    }
}