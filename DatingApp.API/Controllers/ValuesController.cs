using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        public DataContext _context;

        public ValuesController(DataContext context)
        {
            this._context = context;
        }

        //[Authorize(Roles = "Admin, Moderator")]
        [HttpGet]
        public async Task<IActionResult> GetValues()
        {
            var values = await this._context.Values.ToListAsync();

            return this.Ok(values);
        }

        //[Authorize(Roles = "Member")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetValue(int id)
        {
            var value = await this._context.Values.FirstOrDefaultAsync(a => a.Id.Equals(id));

            return this.Ok(value);
        }

        // POST api/values
        [AllowAnonymous]
        [HttpPost("{id}")]
        public void Post([FromBody] string value)
        {

        }

        [AllowAnonymous]
        [HttpPost]
        public void EditValues(string valor)
        {

            
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
