using sara_coursework.models;
using System.Collections.Generic;

namespace sara_coursework.Services.Repositories
{
    public interface IAwardAssignmentRepository
    {
        List<AwardAssignment> GetAwardAssignments();
        void SaveAwardAssignment(AwardAssignment assignment);
        void DeleteAwardAssignment(int id);
    }
}
