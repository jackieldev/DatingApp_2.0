using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;

        private Cloudinary _cloudinary;

        public AdminController(DataContext context,
            UserManager<User> userManager,
            IOptions<CloudinarySettings> cloudinaryConfig)
        {
            this._userManager = userManager;
            this._cloudinaryConfig = cloudinaryConfig;
            this._context = context;

            Account acc = new Account(
                this._cloudinaryConfig.Value.CloudName,
                this._cloudinaryConfig.Value.ApiKey,
                this._cloudinaryConfig.Value.ApiSecret
            );

            this._cloudinary = new Cloudinary(acc);
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

            selectedRoles = selectedRoles ?? new string[] { };

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
        public async Task<IActionResult> GetPhotosForModeration()
        {
            var photos = await this._context.Photos
                        .Include(x => x.User)
                        .IgnoreQueryFilters()
                        .Where(x => !x.IsApproved)
                        .Select(x => new
                        {
                            Id = x.Id,
                            UserName = x.User.UserName,
                            Url = x.Url,
                            IsApproved = x.IsApproved
                        }).ToListAsync();

            return Ok(photos);
        }


        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpGet("approvePhoto/{photoId}")]
        public async Task<IActionResult> ApprovePhoto(int photoId)
        {
            var photo = await this._context.Photos
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(x => x.Id.Equals(photoId));

            photo.IsApproved = true;

            await this._context.SaveChangesAsync();

            return Ok();
        }


        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpGet("rejectPhoto/{photoId}")]
        public async Task<IActionResult> RejectPhoto(int photoId)
        {
            var photo = await this._context.Photos
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(x => x.Equals(photoId));

            if (photo.IsMain)
                return BadRequest("You cannot reject the main photo");

            if (photo.PublicId != null)
            {
                var deleteParams = new DeletionParams(photo.PublicId);

                var result = this._cloudinary.Destroy(deleteParams);

                if (result.Result.Equals("ok"))
                    this._context.Photos.Remove(photo);
            }

            if (photo.PublicId == null)
                this._context.Photos.Remove(photo);

            await this._context.SaveChangesAsync();

            return Ok();
        }
    }
}