using Microsoft.EntityFrameworkCore;
using sara_coursework.data;
using sara_coursework.models;
using System.Collections.Generic;
using System.Linq;

namespace sara_coursework.Services.Repositories
{
    public class AwardedRepository : IAwardedRepository
    {
        public List<Awarded> GetAwarded()
        {
            using var context = new AppDbContext();
            var list = context.Awarded.AsNoTracking().ToList();

            var collectivesMap = list.OfType<Collective>().ToDictionary(c => c.Id);
            foreach (var citizen in list.OfType<Citizen>())
            {
                if (citizen.CollectiveId.HasValue && collectivesMap.TryGetValue(citizen.CollectiveId.Value, out var coll))
                {
                    citizen.Collective = coll;
                }
            }

            return list;
        }

        public void SaveAwarded(Awarded awarded)
        {
            using var context = new AppDbContext();
            if (awarded.Id == 0)
            {
                if (awarded is Collective collective)
                {
                    foreach (var member in collective.Members)
                    {
                        context.Entry(member).State = EntityState.Unchanged;
                    }
                    context.Awarded.Add(collective);
                }
                else
                {
                    context.Awarded.Add(awarded);
                }
            }
            else
            {
                if (awarded is Citizen citizen)
                {
                    var existing = context.Awarded.OfType<Citizen>().FirstOrDefault(c => c.Id == citizen.Id);
                    if (existing != null)
                    {
                        existing.LastName = citizen.LastName;
                        existing.FirstName = citizen.FirstName;
                        existing.MiddleName = citizen.MiddleName;
                        existing.Position = citizen.Position;
                        existing.CollectiveId = citizen.CollectiveId;
                    }
                }
                else if (awarded is Collective collective)
                {
                    var existing = context.Awarded.OfType<Collective>()
                        .Include(c => c.Members)
                        .FirstOrDefault(c => c.Id == collective.Id);
                    if (existing != null)
                    {
                        existing.CollectiveName = collective.CollectiveName;
                        existing.Members.Clear();
                        foreach (var m in collective.Members)
                        {
                            var member = context.Awarded.OfType<Citizen>().FirstOrDefault(c => c.Id == m.Id);
                            if (member != null)
                            {
                                existing.Members.Add(member);
                            }
                        }
                    }
                }
            }
            context.SaveChanges();
        }

        public void DeleteAwarded(int id)
        {
            using var context = new AppDbContext();
            var item = context.Awarded.Find(id);
            if (item != null)
            {
                context.Awarded.Remove(item);
                context.SaveChanges();
            }
        }
    }
}
