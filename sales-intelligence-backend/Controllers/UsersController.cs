using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesIntelligence.Api.DTOs;
using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] string? role)
        {
            var users = await _userManager.Users.ToListAsync();
            var result = new List<UserDto>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var userRole = roles.FirstOrDefault() ?? "User";

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.ToLower();
                    if (!u.FullName.ToLower().Contains(s) && !(u.Email?.ToLower().Contains(s) ?? false))
                    {
                        continue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(role) && !role.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    if (!userRole.Equals(role, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                result.Add(new UserDto
                {
                    Id = u.Id,
                    Name = u.FullName,
                    Email = u.Email ?? string.Empty,
                    Role = userRole,
                    Department = u.Department,
                    Status = u.IsActive ? "Active" : "Inactive"
                });
            }

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var u = await _userManager.FindByIdAsync(id);
            if (u == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(u);
            return Ok(new UserDto
            {
                Id = u.Id,
                Name = u.FullName,
                Email = u.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "User",
                Department = u.Department,
                Status = u.IsActive ? "Active" : "Inactive"
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] RegisterUserDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null) return BadRequest(new { message = "Email already registered" });

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                EmailConfirmed = true,
                FullName = dto.FullName,
                Department = dto.Department,
                IsActive = true
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                return BadRequest(createResult.Errors);
            }

            var roleName = dto.Role.Equals("Superadmin", StringComparison.OrdinalIgnoreCase) ? "Superadmin" : "User";
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }
            await _userManager.AddToRoleAsync(user, roleName);

            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new UserDto
            {
                Id = user.Id,
                Name = user.FullName,
                Email = user.Email,
                Role = roleName,
                Department = user.Department,
                Status = "Active"
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto dto)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.FullName = dto.FullName;
            user.Department = dto.Department;
            user.IsActive = dto.IsActive;

            await _userManager.UpdateAsync(user);

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            var targetRole = dto.Role.Equals("Superadmin", StringComparison.OrdinalIgnoreCase) ? "Superadmin" : "User";
            await _userManager.AddToRoleAsync(user, targetRole);

            return Ok(new UserDto
            {
                Id = user.Id,
                Name = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = targetRole,
                Department = user.Department,
                Status = user.IsActive ? "Active" : "Inactive"
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Deactivate instead of hard delete to preserve integrity
            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "User deactivated successfully" });
        }
    }
}
