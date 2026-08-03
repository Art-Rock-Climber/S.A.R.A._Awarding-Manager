using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sara_coursework.models
{
    // Базовый класс для награждаемых (Table-Per-Hierarchy подход)
    [Table("Awarded")]
    public abstract class Awarded
    {
        [Key]
        public int Id { get; set; }

        public abstract string DisplayName { get; }

        // Навигационное свойство для связи с награждениями
        public ICollection<AwardAssignment> AwardAssignments { get; set; } = new List<AwardAssignment>();

        public abstract override string ToString();
    }
}
