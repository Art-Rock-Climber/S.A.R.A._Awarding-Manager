using sara_coursework.models;
using System.Collections.Generic;

namespace sara_coursework.Services.Repositories
{
    public interface IUserRepository
    {
        List<User> GetUsers();
        void SaveUser(User user);
        void DeleteUser(int id);
    }
}
