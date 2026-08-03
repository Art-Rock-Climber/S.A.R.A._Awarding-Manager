using sara_coursework.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sara_coursework.ViewModels
{
    public class AwardingViewModel
    {
        public int Id { get; set; }
        public string AwardedName { get; set; } = null!;
        public string AwardedType { get; set; } = null!;
        public string Position { get; set; } = null!;
        public string AwardTitle { get; set; } = null!;
        public string Reason { get; set; } = null!;
        public string DecreeNumber { get; set; } = null!;
        public DateTime DecreeDate { get; set; }

        public AwardingViewModel() { }

        public AwardingViewModel(AwardAssignment entity)
        {
            Id = entity.Id;
            AwardedName = entity.Awarded?.ToString() ?? string.Empty;
            AwardedType = entity.Awarded is Citizen ? "Гражданин" : "Коллектив";
            Position = entity.Awarded is Citizen citizen ? citizen.Position : "-";
            AwardTitle = entity.Award?.AwardName ?? string.Empty;
            Reason = entity.Decree.AwardReason.ReasonName;
            DecreeNumber = entity.Decree?.Number ?? string.Empty;
            DecreeDate = entity.Decree?.Date ?? DateTime.MinValue;
        }
    }
}
