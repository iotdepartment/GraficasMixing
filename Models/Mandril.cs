using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraficasMixing.Models
{
    [Table("Mandriles")]
    public class Mandril
    {
        [Key]
        public int ID { get; set; }

        [Column("Mandril")]
        public string NombreMandril { get; set; } // nvarchar(100)

        public string Familia { get; set; }       // nvarchar(100), puede ser null
        public int? JDE { get; set; }             // int, puede ser null
        public string Kanban { get; set; }        // nvarchar(50), puede ser null
        public int? StdPack { get; set; }         // int, puede ser null

        public ICollection<Estado> Estados { get; set; }
    }
}