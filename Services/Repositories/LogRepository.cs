using Microsoft.EntityFrameworkCore;
using sara_coursework.data;
using sara_coursework.models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace sara_coursework.Services.Repositories
{
    public class LogRepository : ILogRepository
    {
        public List<LogEntry> GetLogs()
        {
            using var context = new AppDbContext();
            return context.Logs.AsNoTracking().OrderByDescending(l => l.Timestamp).ToList();
        }

        public void LogAction(string level, string action, string message, string username)
        {
            using var context = new AppDbContext();
            context.Logs.Add(new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                UserName = username,
                Action = action,
                Message = message
            });
            context.SaveChanges();
        }

        public void ClearLogs(DateTime startDate, DateTime endDate)
        {
            using var context = new AppDbContext();
            var endDateExclusive = endDate.Date.AddDays(1);
            var logsToDelete = context.Logs.Where(l => l.Timestamp >= startDate.Date && l.Timestamp < endDateExclusive);
            context.Logs.RemoveRange(logsToDelete);
            context.SaveChanges();
        }
    }
}
