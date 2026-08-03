using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sara_coursework.models
{
    public class Decree
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Number { get; set; } = null!;

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public int AwardReasonId { get; set; }


        [ForeignKey("AwardReasonId")]
        public AwardReason AwardReason { get; set; } = null!;

        // Навигационное свойство для связи с награждениями
        public ICollection<AwardAssignment> AwardAssignments { get; set; } = new List<AwardAssignment>();

        // Свойство для отображения в DataGrid
        [NotMapped]
        public string DisplayText => $"{Number} от {Date:dd.MM.yyyy} ({AwardReason?.ReasonName})";
    }
}
