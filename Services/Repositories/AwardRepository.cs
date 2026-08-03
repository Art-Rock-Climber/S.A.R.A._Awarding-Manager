using Microsoft.EntityFrameworkCore;
using sara_coursework.data;
using sara_coursework.models;
using System.Collections.Generic;
using System.Linq;

namespace sara_coursework.Services.Repositories
{
    public class AwardRepository : IAwardRepository
    {
        public List<Award> GetAwards()
        {
            using var context = new AppDbContext();
            return context.Awards.AsNoTracking().ToList();
        }

        public void SaveAward(Award award)
        {
            using var context = new AppDbContext();
            if (award.Id == 0)
            {
                context.Awards.Add(award);
            }
            else
            {
                var existing = context.Awards.Find(award.Id);
                if (existing != null)
                {
                    existing.AwardName = award.AwardName;
                }
            }
            context.SaveChanges();
        }

        public void DeleteAward(int id)
        {
            using var context = new AppDbContext();
            var item = context.Awards.Find(id);
            if (item != null)
            {
                context.Awards.Remove(item);
                context.SaveChanges();
            }
        }
    }
}
