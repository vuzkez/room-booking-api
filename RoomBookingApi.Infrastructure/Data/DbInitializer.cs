using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using RoomBookingApi.Domain.Entities;

namespace RoomBookingApi.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<RoleApplication>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<UserApplication>>();

            var findAdmin = await userManager.FindByNameAsync("Admin");
            if (findAdmin != null)
                return;

            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new RoleApplication { Name = "Admin"});

            if (!await roleManager.RoleExistsAsync("User"))
                await roleManager.CreateAsync(new RoleApplication { Name = "User" });

            var adminUser = new UserApplication
            {
                UserName = "Admin",
                Email = "Admin@gmail.com",
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123");
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new Exception($"Ошибка создания администратора: {errors}");
            }

            if (!await userManager.IsInRoleAsync(adminUser,"Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}
