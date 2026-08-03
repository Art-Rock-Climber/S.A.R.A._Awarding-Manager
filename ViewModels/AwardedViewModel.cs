using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sara_coursework.ViewModels
{
    public class AwardedViewModel
    {
        public int Id { get; set; }
        public string AwardedType { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
    }
}
