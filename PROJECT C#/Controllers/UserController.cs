using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT_C_.DTOs;

namespace PROJECT_C_.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;

        public UserController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<List<UserDto>>> GetAllUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Roles = roles.ToList(),
                    IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow
                });
            }

            return Ok(userDtos);
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveSeller(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Seller"))
            {
                user.LockoutEnd = null;
                await _userManager.UpdateAsync(user);
                return Ok(new { message = "Seller approved successfully" });
            }
            
            return BadRequest("User is not a seller or already active");
        }

        [HttpPost("{id}/toggle-lock")]
        public async Task<IActionResult> ToggleLock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Cannot lock Admin to prevent lockout
            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return BadRequest("Cannot lock Admin account");

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                user.LockoutEnd = null; // Unlock
            }
            else
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100); // Lock
            }

            await _userManager.UpdateAsync(user);
            return Ok(new { message = "Lock status updated", isLocked = user.LockoutEnd.HasValue });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return BadRequest("Cannot delete Admin account");

            await _userManager.DeleteAsync(user);
            return NoContent();
        }
    }

    public class UserDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
        public bool IsLocked { get; set; }
    }
}
