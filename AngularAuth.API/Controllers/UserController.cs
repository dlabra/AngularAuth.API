using AngularAuth.API.Context;
using AngularAuth.API.Helpers;
using AngularAuth.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularAuth.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        //Inject appDbContext
        private readonly AppDbContext _appDbContext;
        public UserController(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] User userObj)
        {
            if (userObj == null)
                return BadRequest();

            var user = await _appDbContext.Users.FirstOrDefaultAsync(x => x.Username == userObj.Username);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            var passwordMatch = PasswordHasher.VerifyPassword(userObj.Password, user.Password);
            if (!passwordMatch)
                return NotFound(new { Message = "Password incorrect" });


            return Ok( new { Message = "Login Success!"});
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser([FromBody] User userObj)
        {
            if (userObj == null)
                return BadRequest();

            var userUsername = await _appDbContext.Users.FirstOrDefaultAsync(x => x.Username == userObj.Username);
            if (userUsername != null)
                return NotFound(new { Message = "User already exist" });

            var userEmail = await _appDbContext.Users.FirstOrDefaultAsync(x => x.Email == userObj.Email);
            if (userEmail != null)
                return NotFound(new { Message = "Email already registered" });

            //check password strengh
            var pass = PasswordHasher.CheckPasswordStrength(userObj.Password);
            if(!string.IsNullOrEmpty(pass))
                return BadRequest(new { Message = pass });

            userObj.Role = string.IsNullOrEmpty(userObj.Role) ? "User" : userObj.Role;
            userObj.Password = PasswordHasher.HashPassword(userObj.Password);
            userObj.Token = string.Empty;

            await _appDbContext.Users.AddAsync(userObj);
            await _appDbContext.SaveChangesAsync();

            return Ok(new { Message = "User Registered!" });
        }
    }
}
