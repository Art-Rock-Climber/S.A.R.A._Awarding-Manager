using sara_coursework.models;
using System.Collections.Generic;

namespace sara_coursework.Services.Repositories
{
    public interface IAwardRepository
    {
        List<Award> GetAwards();
        void SaveAward(Award award);
        void DeleteAward(int id);
    }
}
