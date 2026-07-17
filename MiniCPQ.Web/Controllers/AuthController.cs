using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiniCPQ.Application.DTOs;
using MiniCPQ.Infrastructure.Identity;

namespace MiniCPQ.Web.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<CurrentUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserDto>> Login(LoginRequest request)
    {
        var user = await userManager.FindByNameAsync(request.Username.Trim());
        if (user is null)
        {
            return Unauthorized(new ProblemDetails { Title = "登录失败", Detail = "用户名或密码错误。", Status = 401 });
        }

        var result = await signInManager.PasswordSignInAsync(user, request.Password, request.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Unauthorized(new ProblemDetails { Title = "登录失败", Detail = "用户名或密码错误。", Status = 401 });
        }

        return Ok(await ToDtoAsync(user));
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserDto>> Me()
    {
        var user = await userManager.GetUserAsync(User);
        return user is null ? Unauthorized() : Ok(await ToDtoAsync(user));
    }

    private async Task<CurrentUserDto> ToDtoAsync(ApplicationUser user) =>
        new(user.Id, user.UserName!, user.Email!, [.. await userManager.GetRolesAsync(user)]);
}
