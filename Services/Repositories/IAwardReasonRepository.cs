using sara_coursework.models;
using System.Collections.Generic;

namespace sara_coursework.Services.Repositories
{
    public interface IAwardReasonRepository
    {
        List<AwardReason> GetAwardReasons();
        void SaveAwardReason(AwardReason reason);
        void DeleteAwardReason(int id);
    }
}
