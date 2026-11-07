using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RoomBookingApi.Domain.Entities;

namespace RoomBookingApi.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<RoleApplication>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<UserApplication>>();
            var settings = serviceProvider.GetRequiredService<IOptions<SeedDataSettings>>().Value;

            foreach (var roleName in settings.Roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new RoleApplication { Name = roleName });
            }

            var adminUser = await userManager.FindByEmailAsync(settings.AdminEmail);
            if (adminUser == null)
            {
                adminUser = new UserApplication
                {
                    UserName = settings.AdminName,
                    Email = settings.AdminEmail,
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                var result = await userManager.CreateAsync(adminUser, settings.AdminPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Error creating administrator: {errors}");
                }

                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}
