using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using EcommerceApi.Auth;
using EcommerceApi.Common;

namespace EcommerceApi.Controllers;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly JwtSettings _jwt;

    public AuthController(IOptions<JwtSettings> jwtOptions)
    {
        _jwt = jwtOptions.Value;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public ActionResult<ApiResponse<LoginResponse>> Login([FromBody] LoginRequest input)
    {
        // Demo authentication: accept a fixed username/password
        if (string.IsNullOrWhiteSpace(input.Username) || string.IsNullOrWhiteSpace(input.Password))
            return BadRequest(ApiResponse<LoginResponse>.FailureResponse("Username and password are required."));

        // Very basic demo check. Replace with real user store.
        var role = string.Empty;
        if (input.Username == "admin" && input.Password == "password_123")
        {
            role = "admin";
        }
        else if (input.Username == "user" && input.Password == "password_321")
        {
            role = "user";
        }
        else
        {
            return Unauthorized(ApiResponse<LoginResponse>.FailureResponse("Invalid credentials"));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(4);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, input.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add role claim for authorization policies
        claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(ApiResponse<LoginResponse>.SuccessResponse(new LoginResponse { Token = tokenString, ExpiresAt = expires }, "Login successful"));
    }
}
