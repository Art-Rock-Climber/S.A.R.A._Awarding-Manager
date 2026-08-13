using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sara_coursework.models
{
    [Table("Awards")]
    public class Award
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(1000)]
        public string AwardName { get; set; } = null!;
    }
}
