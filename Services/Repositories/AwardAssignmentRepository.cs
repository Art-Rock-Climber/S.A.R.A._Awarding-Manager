using Microsoft.EntityFrameworkCore;
using sara_coursework.data;
using sara_coursework.models;
using System.Collections.Generic;
using System.Linq;

namespace sara_coursework.Services.Repositories
{
    public class AwardAssignmentRepository : IAwardAssignmentRepository
    {
        public List<AwardAssignment> GetAwardAssignments()
        {
            using var context = new AppDbContext();
            return context.AwardAssignments
                .Include(aa => aa.Awarded)
                .Include(aa => aa.Award)
                .Include(aa => aa.Decree)
                .ThenInclude(d => d.AwardReason)
                .ToList();
        }

        public void SaveAwardAssignment(AwardAssignment assignment)
        {
            using var context = new AppDbContext();
            if (assignment.Id == 0)
            {
                if (assignment.Award != null) context.Entry(assignment.Award).State = EntityState.Unchanged;
                if (assignment.Awarded != null) context.Entry(assignment.Awarded).State = EntityState.Unchanged;
                if (assignment.Decree != null) context.Entry(assignment.Decree).State = EntityState.Unchanged;
                context.AwardAssignments.Add(assignment);
            }
            else
            {
                var existing = context.AwardAssignments.Find(assignment.Id);
                if (existing != null)
                {
                    existing.AwardedId = assignment.AwardedId;
                    existing.AwardId = assignment.AwardId;
                    existing.DecreeId = assignment.DecreeId;
                }
            }
            context.SaveChanges();
        }

        public void DeleteAwardAssignment(int id)
        {
            using var context = new AppDbContext();
            var item = context.AwardAssignments.Find(id);
            if (item != null)
            {
                context.AwardAssignments.Remove(item);
                context.SaveChanges();
            }
        }
    }
}
