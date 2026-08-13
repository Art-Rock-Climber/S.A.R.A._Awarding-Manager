using System;

namespace sara_coursework.ViewModels
{
    public class AwardedViewModel
    {
        public int Id { get; set; }
        public string AwardedType { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Position { get; set; } = "—";
        public string CollectiveName { get; set; } = "—";
    }
}
