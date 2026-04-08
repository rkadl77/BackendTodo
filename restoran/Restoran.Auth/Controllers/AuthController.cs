using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Restoran.Auth.DTOs;
using Restoran.Auth.Models;
using Restoran.Auth.Services;

namespace Restoran.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TokenService _tokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<TokenResponseDto>> Register([FromBody] RegisterDto dto)
    {
        var allowedRoles = new[] { "Customer", "Courier", "Manager", "Cook" };
        if (!allowedRoles.Contains(dto.Role))
            return BadRequest(new { message = "Допустимые роли: Customer, Courier, Manager, Cook" });

        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
            return Conflict(new { message = "Пользователь с таким email уже существует" });

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            BirthDate = dto.BirthDate,
            Gender = dto.Gender,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, dto.Role);

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(await _tokenService.GenerateTokensAsync(user, roles));
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return Unauthorized(new { message = "Неверный email или пароль" });

        if (user.IsBanned)
            return StatusCode(403, new { message = "Аккаунт заблокирован" });

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Неверный email или пароль" });

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(await _tokenService.GenerateTokensAsync(user, roles));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto)
    {
        await _tokenService.RevokeRefreshTokenAsync(dto.RefreshToken);
        return NoContent();
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponseDto>> Refresh([FromBody] RefreshTokenDto dto)
    {
        var refreshToken = await _tokenService.GetValidRefreshTokenAsync(dto.RefreshToken);
        if (refreshToken == null)
            return Unauthorized(new { message = "Недействительный или просроченный refresh token" });

        var user = await _userManager.FindByIdAsync(refreshToken.UserId.ToString());
        if (user == null || user.IsBanned)
            return Unauthorized(new { message = "Пользователь не найден или заблокирован" });

        await _tokenService.RevokeRefreshTokenAsync(dto.RefreshToken);

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(await _tokenService.GenerateTokensAsync(user, roles));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(GetUserId());
        if (user == null) return NotFound();

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _tokenService.RevokeAllUserTokensAsync(user.Id);
        return NoContent();
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var user = await _userManager.FindByIdAsync(GetUserId());
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(MapToProfileDto(user, roles));
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var user = await _userManager.FindByIdAsync(GetUserId());
        if (user == null) return NotFound();

        if (dto.FullName != null) user.FullName = dto.FullName;
        if (dto.BirthDate.HasValue) user.BirthDate = dto.BirthDate.Value;
        if (dto.Gender.HasValue) user.Gender = dto.Gender;
        if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;
        if (dto.Address != null) user.Address = dto.Address;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(MapToProfileDto(user, roles));
    }

    [HttpGet("users/{id}")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> GetUserById(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(MapToProfileDto(user, roles));
    }

    private string GetUserId() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? throw new InvalidOperationException();

    private static UserProfileDto MapToProfileDto(ApplicationUser user, IList<string> roles) => new()
    {
        Id = user.Id,
        Email = user.Email!,
        FullName = user.FullName,
        BirthDate = user.BirthDate,
        Gender = user.Gender,
        PhoneNumber = user.PhoneNumber,
        Address = user.Address,
        CreatedAt = user.CreatedAt,
        Roles = roles.ToList()
    };
}
