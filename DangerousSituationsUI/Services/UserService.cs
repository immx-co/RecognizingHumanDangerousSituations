using ClassLibrary.Database.Models;
using ClassLibrary.Database;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;


namespace DangerousSituationsUI.Services
{

    public class UserService
    {
        private readonly IServiceProvider _serviceProvider;

        public UserService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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
    }

}

