using AngularAuth.API.Context;
using AngularAuth.API.Helpers;
using AngularAuth.API.Models;
using AngularAuth.API.Models.Dto;
using AngularAuth.API.Utility;
using Microsoft.AspNetCore.Authorization;
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
        private readonly JwtToken _jwtToken;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public UserController(AppDbContext appDbContext, JwtToken jwtToken, IConfiguration configuration, IEmailService emailService)
        {
            _jwtToken = jwtToken;
            _appDbContext = appDbContext;
            _configuration = configuration;
            _emailService = emailService;
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

            user.Token = _jwtToken.CreateJwt(user);
            var newAccessToken = user.Token;
            var newRefreshToken = _jwtToken.CreateRefreshToken(_appDbContext);

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(5);
            await _appDbContext.SaveChangesAsync();

            return Ok(new TokenApiDto
            {
                AccessToken = newAccessToken,
                RefreeshToken = newRefreshToken
            });
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
            if (!string.IsNullOrEmpty(pass))
                return BadRequest(new { Message = pass });

            userObj.Role = string.IsNullOrEmpty(userObj.Role) ? "User" : userObj.Role;
            userObj.Password = PasswordHasher.HashPassword(userObj.Password);
            userObj.Token = string.Empty;

            await _appDbContext.Users.AddAsync(userObj);
            await _appDbContext.SaveChangesAsync();

            return Ok(new { Message = "User Registered!" });
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAllUsers()
        {
            var allUsers = await _appDbContext.Users.ToListAsync();
            return allUsers;
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenApiDto tokenApiDto)
        {
            if (tokenApiDto is null)
                return BadRequest();

            string accessToken = tokenApiDto.AccessToken;
            string refreshToken = tokenApiDto.RefreeshToken;

            var principal = _jwtToken.GetPrincipalFromExpiredToken(accessToken);
            var username = principal.Identity.Name;
            var user = await _appDbContext.Users.FirstOrDefaultAsync(x => x.Username == username);

            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
                return BadRequest(new { Message = "Invalid token" });

            var newAccessToken = _jwtToken.CreateJwt(user);
            var newRefreshToken = _jwtToken.CreateRefreshToken(_appDbContext);

            user.RefreshToken = newRefreshToken;
            await _appDbContext.SaveChangesAsync();

            return Ok(new TokenApiDto
            {
                AccessToken = newAccessToken,
                RefreeshToken = newRefreshToken
            });
        }

        [HttpPost("send-reset-email/{email}")]
        public async Task<IActionResult> SendEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest();
            var user = await _appDbContext.Users.FirstOrDefaultAsync(x => x.Email == email);
            if (user == null)
                return NotFound(new { Message = "Email not found" });

            var token = _jwtToken.CreateRefreshToken(_appDbContext);
            user.ResetPasswordToken = token;
            user.ResetPasswordExpiry = DateTime.Now.AddMinutes(15);
            await _appDbContext.SaveChangesAsync();

            //send email
            string from = _configuration["EmailSettings:From"];
            EmailModel emailModel = new EmailModel(email, "Reset Password", EmailBody.EmailStringBody(email, token));
            _emailService.SendEmail(emailModel);
            _appDbContext.Entry(user).State = EntityState.Modified;

            await _appDbContext.SaveChangesAsync();

            return Ok(new 
            {
                StatusCode = 200,
                Message = "Email sent" 
            });

        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            if (resetPasswordDto is null)
                return BadRequest();

            var user = await _appDbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == resetPasswordDto.Email);
            if (user == null)
                return NotFound(new { Message = "Email not found" });

            if (user.ResetPasswordToken != resetPasswordDto.EmailToken || user.ResetPasswordExpiry <= DateTime.Now)
                return BadRequest(new { Message = "Invalid token" });

            var passwordStrenght = PasswordHasher.CheckPasswordStrength(resetPasswordDto.NewPassword);
            if (!string.IsNullOrEmpty(passwordStrenght))
                return BadRequest(new { Message = passwordStrenght });

            user.Password = PasswordHasher.HashPassword(resetPasswordDto.NewPassword);
            user.ResetPasswordToken = string.Empty;
            user.ResetPasswordExpiry = DateTime.Now;
            _appDbContext.Entry(user).State = EntityState.Modified;
            await _appDbContext.SaveChangesAsync();

            return Ok(new { Message = "Password reset successful" });
        }
    }
}
