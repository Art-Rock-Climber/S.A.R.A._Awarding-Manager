using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sara_coursework.models
{
    public class AwardReason
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string ReasonName { get; set; } = null!;
    }
}
