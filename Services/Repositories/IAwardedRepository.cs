using sara_coursework.models;
using System.Collections.Generic;

namespace sara_coursework.Services.Repositories
{
    public interface IAwardedRepository
    {
        List<Awarded> GetAwarded();
        void SaveAwarded(Awarded awarded);
        void DeleteAwarded(int id);
    }
}
