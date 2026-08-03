using sara_coursework.Services.Security;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sara_coursework.models
{
    public class Citizen : Awarded
    {
        [Required]
        [MaxLength(500)]
        public string LastName { get; set; } = null!;

        [Required]
        [MaxLength(500)]
        public string FirstName { get; set; } = null!;

        [MaxLength(500)]
        public string? MiddleName { get; set; }

        [Required]
        [MaxLength(200)]
        public string Position { get; set; } = null!;

        public override string DisplayName => this.ToString();

        public int? CollectiveId { get; set; }
        public Collective? Collective { get; set; }


        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(MiddleName)
                ? $"{LastName} {FirstName}".Trim()
                : $"{LastName} {FirstName} {MiddleName}".Trim();
        }
    }
}
