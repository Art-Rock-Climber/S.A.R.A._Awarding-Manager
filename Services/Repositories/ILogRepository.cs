using sara_coursework.models;
using System;
using System.Collections.Generic;

namespace sara_coursework.Services.Repositories
{
    public interface ILogRepository
    {
        List<LogEntry> GetLogs();
        void LogAction(string level, string action, string message, string username);
        void ClearLogs(DateTime startDate, DateTime endDate);
    }
}
