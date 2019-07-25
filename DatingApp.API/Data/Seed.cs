using System;
using System.Collections.Generic;
using System.Linq;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace DatingApp.API.Data
{
    public class Seed
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        public Seed(UserManager<User> userManagar, RoleManager<Role> roleManager)
        {
            this._roleManager = roleManager;
            this._userManager = userManagar;
        }

        public void SeedUsers()
        {
            if (!this._userManager.Users.Any())
            {
                var userData = System.IO.File.ReadAllText("Data/UserSeedData.json");
                var users = JsonConvert.DeserializeObject<List<User>>(userData);

                var roles = new List<Role>
                {
                    new Role{ Name = "Member"},
                    new Role{ Name = "Admin"},
                    new Role{ Name = "Moderator"},
                    new Role{ Name = "VIP"}
                };

                foreach (var role in roles)
                    this._roleManager.CreateAsync(role).Wait();

                foreach (var user in users)
                {
                    user.Photos.SingleOrDefault().IsApproved = true;
                    this._userManager.CreateAsync(user, "password").Wait();
                    this._userManager.AddToRoleAsync(user, "Member").Wait();
                }

                var adminUser = new User
                {
                    UserName = "Admin",
                    LastActive = DateTime.Now,
                    Created = DateTime.Now,
                    City = "Piauí",
                    Country = "Brasil"
                };

                IdentityResult result = this._userManager.CreateAsync(adminUser, "password").Result;

                if (result.Succeeded)
                {
                    var admin = this._userManager.FindByNameAsync("Admin").Result;
                    this._userManager.AddToRolesAsync(admin, new[] { "Admin", "Moderator" }).Wait();
                }
            }
        }
    }
}
