using Microsoft.EntityFrameworkCore;
using sara_coursework.data;
using sara_coursework.models;
using System.Collections.Generic;
using System.Linq;

namespace sara_coursework.Services.Repositories
{
    public class AwardReasonRepository : IAwardReasonRepository
    {
        public List<AwardReason> GetAwardReasons()
        {
            using var context = new AppDbContext();
            return context.AwardReasons.AsNoTracking().ToList();
        }

        public void SaveAwardReason(AwardReason reason)
        {
            using var context = new AppDbContext();
            if (reason.Id == 0)
            {
                context.AwardReasons.Add(reason);
            }
            else
            {
                var existing = context.AwardReasons.Find(reason.Id);
                if (existing != null)
                {
                    existing.ReasonName = reason.ReasonName;
                }
            }
            context.SaveChanges();
        }

        public void DeleteAwardReason(int id)
        {
            using var context = new AppDbContext();
            var item = context.AwardReasons.Find(id);
            if (item != null)
            {
                context.AwardReasons.Remove(item);
                context.SaveChanges();
            }
        }
    }
}
