using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraficasMixing.Models
{
    public class Materiales
    {

        [Key]
        public int ID { get; set; }

        [Column("Material")]
        public string MATERIAL { get; set; }   // int en la BD

        [Column("Batch")]
        public string Batch { get; set; }   // int en la BD

    }
}
