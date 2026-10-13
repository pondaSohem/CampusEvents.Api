using CampusEvents.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize(Roles = "ADMIN")]
public class RolesController : ControllerBase
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public RolesController(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    // GET: api/roles
    // Get all roles
    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleManager.Roles
            .Select(r => new
            {
                r.Id,
                r.Name
            })
            .ToListAsync();

        return Ok(roles);
    }

    // GET: api/roles/users/{userId}
    // Get all roles assigned to a specific user
    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUserRoles(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return NotFound("User not found.");

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles
        });
    }

    // POST: api/roles/{roleName}
    // Create a new role
    [HttpPost("{roleName}")]
    public async Task<IActionResult> CreateRole(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest("Role name is required.");

        roleName = roleName.Trim();

        var roleExists = await _roleManager.RoleExistsAsync(roleName);

        if (roleExists)
            return BadRequest("Role already exists.");

        var result = await _roleManager.CreateAsync(
            new IdentityRole(roleName));

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new
        {
            message = "Role created successfully.",
            role = roleName
        });
    }

    // DELETE: api/roles/{roleName}
    // Delete a role
    [HttpDelete("{roleName}")]
    public async Task<IActionResult> DeleteRole(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest("Role name is required.");

        roleName = roleName.Trim();

        // Protect the application's core roles.
        if (roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
            roleName.Equals("User", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(
                "The User and Admin roles cannot be deleted.");
        }

        var role = await _roleManager.FindByNameAsync(roleName);

        if (role == null)
            return NotFound("Role not found.");

        // Do not delete a role that is currently assigned to users.
        var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);

        if (usersInRole.Count > 0)
        {
            return BadRequest(
                "The role cannot be deleted because it is currently assigned to one or more users.");
        }

        var result = await _roleManager.DeleteAsync(role);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return NoContent();
    }

    // POST: api/roles/users/{userId}/assign/{roleName}
    // Assign a role to a user
    [HttpPost("users/{userId}/assign/{roleName}")]
    public async Task<IActionResult> AssignRole(
        string userId,
        string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest("Role name is required.");

        roleName = roleName.Trim();

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return NotFound("User not found.");

        var roleExists = await _roleManager.RoleExistsAsync(roleName);

        if (!roleExists)
            return NotFound("Role not found.");

        var alreadyHasRole =
            await _userManager.IsInRoleAsync(user, roleName);

        if (alreadyHasRole)
        {
            return BadRequest(
                $"User already has the '{roleName}' role.");
        }

        var result = await _userManager.AddToRoleAsync(
            user,
            roleName);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new
        {
            message = "Role assigned successfully.",
            userId = user.Id,
            role = roleName
        });
    }

    // DELETE: api/roles/users/{userId}/remove/{roleName}
    // Remove a role from a user
    [HttpDelete("users/{userId}/remove/{roleName}")]
    public async Task<IActionResult> RemoveRole(
        string userId,
        string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest("Role name is required.");

        roleName = roleName.Trim();

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return NotFound("User not found.");

        var roleExists = await _roleManager.RoleExistsAsync(roleName);

        if (!roleExists)
            return NotFound("Role not found.");

        var hasRole =
            await _userManager.IsInRoleAsync(user, roleName);

        if (!hasRole)
        {
            return BadRequest(
                $"User does not have the '{roleName}' role.");
        }

        // Prevent the last administrator from losing Admin access.
        if (roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            var admins = await _userManager.GetUsersInRoleAsync("ADMIN");

            if (admins.Count <= 1)
            {
                return BadRequest(
                    "The last remaining administrator cannot have the Admin role removed.");
            }
        }

        var result = await _userManager.RemoveFromRoleAsync(
            user,
            roleName);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new
        {
            message = "Role removed successfully.",
            userId = user.Id,
            role = roleName
        });
    }
}