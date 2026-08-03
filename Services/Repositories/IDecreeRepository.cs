using sara_coursework.models;
using System.Collections.Generic;

namespace sara_coursework.Services.Repositories
{
    public interface IDecreeRepository
    {
        List<Decree> GetDecrees();
        void SaveDecree(Decree decree);
        void DeleteDecree(int id);
    }
}
