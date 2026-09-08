using Microsoft.AspNetCore.Identity;
using TMApi.Models;

namespace TMApi.Data
{
    public static class Seed
    {
        public static async Task SeedAsync(RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager)
        {
            //Seed roles
            string[] roles =
            {
                "Admin",
                "Agent",
                "User"
            };

            foreach(var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(role));
                }

            }

            const string adminUsername = "admin@example.com";
            const string adminEmail = "admin@example.com";
            const string adminPassword = "Sinaye@123";

            var adminUser = await userManager.FindByNameAsync(adminUsername);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminUsername,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    IsActive = true,
                    FullName = "System Administrator"
                };

                var result = await userManager.CreateAsync(
                    adminUser, adminPassword);

                if (!result.Succeeded)
                {
                    throw new Exception(
                         $"Failed to create Administrator: " +
                        $"{string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            // Add Administrator to Admin role
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}
