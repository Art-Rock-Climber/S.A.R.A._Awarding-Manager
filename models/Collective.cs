using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sara_coursework.models
{
    public class Collective : Awarded
    {
        [Required]
        [MaxLength(1000)]
        public string CollectiveName { get; set; } = null!;

        public override string DisplayName => this.CollectiveName;

        // Связь с гражданами (через скрытую промежуточную таблицу)
        public ICollection<Citizen> Members { get; set; } = new List<Citizen>();

        public override string ToString() => this.CollectiveName;
    }
}
