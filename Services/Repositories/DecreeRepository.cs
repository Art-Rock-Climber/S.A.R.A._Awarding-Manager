using Microsoft.EntityFrameworkCore;
using sara_coursework.data;
using sara_coursework.models;
using System.Collections.Generic;
using System.Linq;

namespace sara_coursework.Services.Repositories
{
    public class DecreeRepository : IDecreeRepository
    {
        public List<Decree> GetDecrees()
        {
            using var context = new AppDbContext();
            return context.Decrees.AsNoTracking().Include(d => d.AwardReason).Include(d => d.AwardAssignments).ToList();
        }

        public void SaveDecree(Decree decree)
        {
            using var context = new AppDbContext();
            if (decree.Id == 0)
            {
                if (decree.AwardReason != null) context.Entry(decree.AwardReason).State = EntityState.Unchanged;
                context.Decrees.Add(decree);
            }
            else
            {
                var existing = context.Decrees.Find(decree.Id);
                if (existing != null)
                {
                    existing.Number = decree.Number;
                    existing.Date = decree.Date;
                    existing.AwardReasonId = decree.AwardReasonId;
                }
            }
            context.SaveChanges();
        }

        public void DeleteDecree(int id)
        {
            using var context = new AppDbContext();
            var item = context.Decrees.Find(id);
            if (item != null)
            {
                context.Decrees.Remove(item);
                context.SaveChanges();
            }
        }
    }
}
