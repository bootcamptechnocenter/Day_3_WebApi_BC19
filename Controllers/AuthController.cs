using EasyCreditApi.Application.DTO.Auth;
using EasyCreditApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace EasyCreditApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]  // Anyone can hit this endpoint
    public async Task<IActionResult> Login([FromBody] ReqLoginDto request)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password);
        if (result is null)
            return Unauthorized(new { message = "Invalid email or password" });

        return Ok(new
        {
            accessToken = result.AccessToken,
            role = result.Role,
            expiresAt = result.ExpiresAt
        });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] ReqRegisterDto request)
    {
        try
        {
            var user = await _authService.RegisterAsync(request.Email, request.Password, request.Name);
            return Ok(new { user.UserId, user.Email, user.Name });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });  // 409 — email already exists
        }
    }
}
