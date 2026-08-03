using Microsoft.EntityFrameworkCore;
using sara_coursework.data;
using sara_coursework.models;
using System.Collections.Generic;
using System.Linq;

namespace sara_coursework.Services.Repositories
{
    public class UserRepository : IUserRepository
    {
        public List<User> GetUsers()
        {
            using var context = new AppDbContext();
            return context.Users.AsNoTracking().ToList();
        }

        public void SaveUser(User user)
        {
            using var context = new AppDbContext();
            if (user.Id == 0)
            {
                context.Users.Add(user);
            }
            else
            {
                context.Users.Update(user);
            }
            context.SaveChanges();
        }

        public void DeleteUser(int id)
        {
            using var context = new AppDbContext();
            var user = context.Users.Find(id);
            if (user != null)
            {
                context.Users.Remove(user);
                context.SaveChanges();
            }
        }
    }
}
