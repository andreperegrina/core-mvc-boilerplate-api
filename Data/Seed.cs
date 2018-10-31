using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Entities;

namespace WebApi.Data
{
    public static class Seed
    {
        public static async Task InitData(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            //adding customs roles
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            string[] roleNames = { "Admin", "Member" };

            foreach (var roleName in roleNames)
            {
                //creating the roles and seeding them to the database
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            //creating a super user who could maintain the web app
            var powerUser = new User
            {
                UserName = configuration.GetSection("AppSettings")["UserEmail"],
                Email = configuration.GetSection("AppSettings")["UserEmail"],
                FirstName = configuration.GetSection("AppSettings")["UserFirstName"],
                LastName = configuration.GetSection("AppSettings")["UserLastName"]
            };

            string userPassword = configuration.GetSection("AppSettings")["UserPassword"];
            var user = await userManager.FindByEmailAsync(configuration.GetSection("AppSettings")["UserEmail"]);

            if(user == null)
            {
                var createPowerUser = await userManager.CreateAsync(powerUser, userPassword);
                if (createPowerUser.Succeeded)
                {
                    //here we tie the new user to the "Admin" role 
                    var identityResult = await userManager.AddToRoleAsync(powerUser, "Admin");
                    if (identityResult.Succeeded)
                    {
                        
                    }
                }
            }
        }
    }
}