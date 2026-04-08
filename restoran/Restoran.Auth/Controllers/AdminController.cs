using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restoran.Auth.DTOs;
using Restoran.Auth.Models;
using Restoran.Auth.Services;

namespace Restoran.Auth.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenService _tokenService;

    public AdminController(UserManager<ApplicationUser> userManager, TokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    [HttpGet("users")]
    public async Task<ActionResult<UserPageDto>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] bool? isBanned,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.Email!.Contains(search) || u.FullName.Contains(search));

        if (isBanned.HasValue)
            query = query.Where(u => u.IsBanned == isBanned.Value);

        var total = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = new List<UserListDto>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            if (!string.IsNullOrEmpty(role) && !roles.Contains(role)) continue;
            items.Add(new UserListDto
            {
                Id = u.Id,
                Email = u.Email!,
                FullName = u.FullName,
                IsBanned = u.IsBanned,
                Roles = roles.ToList(),
                CreatedAt = u.CreatedAt
            });
        }

        return Ok(new UserPageDto { Items = items, TotalCount = total, Page = page, PageSize = pageSize });
    }

    [HttpPost("users/{id}/ban")]
    public async Task<IActionResult> BanUser(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        user.IsBanned = true;
        await _userManager.UpdateAsync(user);
        await _tokenService.RevokeAllUserTokensAsync(id);

        return NoContent();
    }

    [HttpPost("users/{id}/unban")]
    public async Task<IActionResult> UnbanUser(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        user.IsBanned = false;
        await _userManager.UpdateAsync(user);

        return NoContent();
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        await _tokenService.RevokeAllUserTokensAsync(id);
        var result = await _userManager.DeleteAsync(user);

        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors.Select(e => e.Description) });
    }

    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, [FromQuery] string newRole)
    {
        var allowedRoles = new[] { "Customer", "Courier", "Manager", "Cook" };
        if (!allowedRoles.Contains(newRole))
            return BadRequest(new { message = "Допустимые роли: Customer, Courier, Manager, Cook" });

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, newRole);

        return NoContent();
    }
}
