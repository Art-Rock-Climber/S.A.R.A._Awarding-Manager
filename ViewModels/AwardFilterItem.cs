using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sara_coursework.ViewModels
{
    public class AwardFilterItem
    {
        public int Id { get; set; }
        public string AwardName { get; set; } = null!;
        public bool IsSelected { get; set; }
    }
}
