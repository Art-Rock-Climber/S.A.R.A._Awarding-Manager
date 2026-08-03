using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sara_coursework.models
{
    public class AwardAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AwardedId { get; set; }

        [Required]
        public int AwardId { get; set; }

        [Required]
        public int DecreeId { get; set; }


        [ForeignKey("AwardId")]
        public Award Award { get; set; } = null!;

        [ForeignKey("AwardedId")]
        public Awarded Awarded { get; set; } = null!;

        [ForeignKey("DecreeId")]
        public Decree Decree { get; set; } = null!;

        public override string ToString()
        {
            return string.Concat(Awarded.ToString(), Award.AwardName, Decree.Number, Decree.AwardReason.ReasonName);
        }
    }
}
