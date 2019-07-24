using System.Linq;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;

        public AdminController(DataContext context, UserManager<User> userManager)
        {
            this._userManager = userManager;
            this._context = context;
        }

        [Authorize(Policy = "RequiredAdminRole")] //informado na Startup
        [HttpGet("usersWithRoles")]
        public async Task<IActionResult> GetUsersWithRoles()
        {
            var userLists = await (from user in this._context.Users
                                   orderby user.Id
                                   select new
                                   {
                                       Id = user.Id,
                                       UserName = user.UserName,
                                       Roles = (from userRole in user.UserRoles
                                                join role in this._context.Roles
                                                on userRole.RoleId equals role.Id
                                                select role.Name).ToList()
                                   }).ToListAsync();

            return Ok(userLists);
        }

        [Authorize(Policy = "RequiredAdminRole")] //informado na Startup
        [HttpPost("editRoles/{userName}")]
        public async Task<IActionResult> EditRoles(string userName, RoleEditDto roleEditDto)
        {
            var user = await this._userManager.FindByNameAsync(userName);

            var userRoles = await this._userManager.GetRolesAsync(user);

            var selectedRoles = roleEditDto.RoleNames;

            selectedRoles = selectedRoles ?? new System.Collections.Generic.List<string>();

            var result = await this._userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

            if (!result.Succeeded)
                return BadRequest("Failed to add to roles");

            result = await this._userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

            if (!result.Succeeded)
                return BadRequest("Failed to add to roles");

            return Ok(await this._userManager.GetRolesAsync(user));
        }

        [Authorize(Policy = "ModeratePhotoRole")] //informado na Startup
        [HttpGet("photosForModeration")]
        public IActionResult GetPhotosForModeration()
        {
            return Ok("Admins or moderatores can see this");
        }
    }
}