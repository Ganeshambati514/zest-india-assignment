using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using StudentManagementSystem.Application.Common;
using StudentManagementSystem.Application.DTOs;

namespace StudentManagementSystem.API.Controllers;

/// <summary>
/// Issues JWT tokens. In this assignment, credentials are validated against a single
/// admin account from configuration. Swap this for a real user store / Identity in production.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT bearer token.
    /// Default dev credentials: admin / Admin@123 (see appsettings.json).
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginDto login)
    {
        var configuredUsername = _configuration["AdminCredentials:Username"];
        var configuredPassword = _configuration["AdminCredentials:Password"];

        if (login.Username != configuredUsername || login.Password != configuredPassword)
        {
            _logger.LogWarning("Failed login attempt for username {Username}", login.Username);
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid username or password"));
        }

        var token = GenerateJwtToken(login.Username);
        _logger.LogInformation("User {Username} logged in successfully", login.Username);

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            token,
            expiresInMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60")
        }, "Login successful"));
    }

    private string GenerateJwtToken(string username)
    {
        var jwtKey = _configuration["Jwt:Key"]!;
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60");

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
