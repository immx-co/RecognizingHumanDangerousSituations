using ClassLibrary.Database.Models;
using ClassLibrary.Database;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ClassLibrary;
using System.Reactive.Linq;


namespace DangerousSituationsUI.Services
{
    public class UserService
    {
        private readonly IServiceProvider _serviceProvider;
        PasswordHasher _hasher;

        public UserService(IServiceProvider serviceProvider, PasswordHasher hasher)
        {
            _serviceProvider = serviceProvider;
            _hasher = hasher;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            using var db = _serviceProvider.GetRequiredService<ApplicationContext>();
            return await db.Users.ToListAsync();
        }

        public async Task UpdateUserAdminStatusAsync(int userId, bool isAdmin)
        {
            using var db = _serviceProvider.GetRequiredService<ApplicationContext>();
            var user = await db.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsAdmin = isAdmin;
                await db.SaveChangesAsync();
            }
        }


        public async Task<User> AddUserAsync(string username, string email, string password, bool isAdmin)
        {
            using var db = _serviceProvider.GetRequiredService<ApplicationContext>();

            string hashedPassword = _hasher.HashPassword(password);

            var newUser = new User
            {
                Name = username,
                Email = email,
                HashPassword = hashedPassword,
                IsAdmin = isAdmin
            };

            db.Users.Add(newUser);
            await db.SaveChangesAsync();
            return newUser;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            using var db = _serviceProvider.GetRequiredService<ApplicationContext>();
            var user = await db.Users.FindAsync(userId);
            if (user != null)
            {
                db.Users.Remove(user);
                await db.SaveChangesAsync();
                return true;
            }
            return false;
        }

    }

}

