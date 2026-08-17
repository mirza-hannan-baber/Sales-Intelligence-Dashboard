using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SalesIntelligence.Api.DTOs;
using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !user.IsActive)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var result = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!result)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new AuthResponseDto
            {
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? "User"
            });
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me([FromQuery] string? email)
        {
            var userEmail = email ?? User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
            {
                // Return default admin info if not provided
                userEmail = "admin@salesintelligence.com";
            }

            var user = await _userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                user = _userManager.Users.FirstOrDefault();
                if (user == null) return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new UserDto
            {
                Id = user.Id,
                Name = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "User",
                Department = user.Department,
                Status = user.IsActive ? "Active" : "Inactive"
            });
        }
    }
}
