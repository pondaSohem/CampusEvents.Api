using System.Security.Claims;
using CampusEvents.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CampusEvents.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // DELETE: api/users/{userId}
    // Admin can delete a normal user.
    // A normal user can delete themselves.
    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var currentUserId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (currentUserId == null)
            return Unauthorized();

        var userToDelete =
            await _userManager.FindByIdAsync(userId);

        if (userToDelete == null)
            return NotFound("User not found.");

        var isAdmin = User.IsInRole("ADMIN");
        var isDeletingSelf = currentUserId == userId;

        // Normal users can only delete themselves.
        if (!isAdmin && !isDeletingSelf)
        {
            return Forbid();
        }

        // Prevent an admin from deleting another admin.
        if (isAdmin && !isDeletingSelf)
        {
            var targetIsAdmin =
                await _userManager.IsInRoleAsync(
                    userToDelete,
                    "ADMIN");

            if (targetIsAdmin)
            {
                return BadRequest(
                    "An administrator cannot delete another administrator.");
            }
        }

        // Prevent the last administrator from deleting themselves.
        if (isAdmin && isDeletingSelf)
        {
            var admins =
                await _userManager.GetUsersInRoleAsync("ADMIN");

            if (admins.Count <= 1)
            {
                return BadRequest(
                    "The last administrator cannot delete their own account.");
            }
        }

        var result =
            await _userManager.DeleteAsync(userToDelete);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return NoContent();
    }
}