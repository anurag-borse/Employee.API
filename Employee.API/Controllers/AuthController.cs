using Employee.API.Data;
using Employee.API.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using Employee.API.Model.ViewModel;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;

namespace Employee.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if the username is already taken
            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Username is already taken.");
                return BadRequest(ModelState);
            }

            // Hash the password before storing it
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            // Create a new user object
            var user = new User
            {
                Username = model.Username,
                Password = hashedPassword
            };

            // Add the user to the database
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Registration successful" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            // Find user by username
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);

            // Check if user exists and verify the password
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                return Unauthorized(); // Invalid username or password
            }

            // Generate JWT token
            var tokenString = GenerateJwtToken(user);

            return Ok(new {result = true, Token = tokenString });
        }

        [HttpGet("profile")]
        [Authorize] // Protect this endpoint with authorization
        public IActionResult Profile()
        {
            // Retrieve user profile based on the authenticated user
            var userId = User.FindFirst(ClaimTypes.Name)?.Value;
            // Fetch user profile from the database based on userId
            var userProfile = _context.Users.Find(int.Parse(userId));
            if (userProfile == null)
            {
                return NotFound("User not found");
            }

            return Ok(userProfile);
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"]);


            // Get secret key from configuration

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString())
                    // Add more claims as needed
                }),
                Expires = DateTime.UtcNow.AddDays(1), // Token expiration time
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
