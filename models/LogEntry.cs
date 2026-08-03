using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sara_coursework.models
{
    public class LogEntry
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = null!; // "Info", "Warning", "Error"
        public string UserName { get; set; } = null!;
        public string Action { get; set; } = null!; // "Login", "DataChange", etc.
        public string Message { get; set; } = null!;
    }
}
