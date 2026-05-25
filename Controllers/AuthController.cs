using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Store.Models;
using Store.Models.Dtos;
using Supabase;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Store.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly Client _supabaseClient;
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
        var url = configuration["Supabase:Url"];
        var key = configuration["Supabase:ServiceRoleKey"]; 
        _supabaseClient = new Client(url, key);
        _supabaseClient.InitializeAsync().GetAwaiter().GetResult();
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            var signUpOptions = new Supabase.Gotrue.SignUpOptions
            {
                Data = new Dictionary<string, object>
            {
                { "name", dto.Name } // 👈 THIS sets display_name internally
            }
            };

            var response = await _supabaseClient.Auth.SignUp(
                Supabase.Gotrue.Constants.SignUpType.Email, 
                dto.Email,
                dto.Password,
                signUpOptions
            );

            if (response.User == null)
                return BadRequest(new { Message = "Registration failed" });

            // Your existing table insert (trigger or manual – both fine)
            var userRole = new UserRole
            {
                Id = Guid.Parse(response.User.Id),
                Name = dto.Name,
                Email = dto.Email,
                Role = "user",
                CreatedAt = DateTime.UtcNow
            };

            await _supabaseClient.From<UserRole>().Insert(userRole);

            return Ok(new { Token = response.AccessToken });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var response = await _supabaseClient.Auth.SignInWithPassword(dto.Email, dto.Password);
            if (response.User == null)
                return BadRequest(new { Message = "Login failed: User object is null" });

            if (!Guid.TryParse(response.User.Id, out Guid userId))
                return BadRequest(new { Message = $"Invalid user ID format: {response.User.Id}" });

            var userRole = await _supabaseClient.From<UserRole>()
                .Where(x => x.Id == userId)
                .Single();

            return Ok(new { Token = response.AccessToken }); 
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = $"Login failed: {ex.Message}" });
        }
    }

    [HttpGet("verify")]
    [Authorize]
    public async Task<IActionResult> Verify()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID is missing in the JWT token");

            var userEmail = User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Email claim is missing in the JWT token");

            if (!Guid.TryParse(userId, out Guid parsedUserId))
                return BadRequest(new { Message = "Invalid user ID in token" });

            var userRole = await _supabaseClient.From<UserRole>()
                .Where(x => x.Id == parsedUserId)
                .Single();

            return Ok(new UserDto
            {
                Id = parsedUserId,
                Name = userRole?.Name ?? string.Empty,
                Email = userEmail,
                Role = userRole?.Role ?? "user"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = $"Verification failed: {ex.Message}" });
        }
    }

    [HttpGet("allusers")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID is missing in the JWT token");

            var userEmail = User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Email claim is missing in the JWT token");

            var users = await _supabaseClient.From<UserRole>().Get();
            var userDtos = users.Models.Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role
            }).ToList();
            return Ok(userDtos);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPut("updaterole")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRole([FromBody] UpdateRoleDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID is missing in the JWT token");

            var userEmail = User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Email claim is missing in the JWT token");

            if (!Guid.TryParse(userId, out Guid currentUserId))
                return Unauthorized(new { Message = "Invalid current user ID" });

            var currentUserRole = await _supabaseClient.From<UserRole>()
                .Where(x => x.Id == currentUserId)
                .Single();

            if (currentUserRole?.Role != "Admin")
                return Unauthorized(new { Message = "Insufficient permissions" });

            var userRole = await _supabaseClient.From<UserRole>()
                .Where(x => x.Id == dto.UserId)
                .Single();

            if (userRole == null)
                return BadRequest(new { Message = "User not found" });

            if (dto.Role != "user" && dto.Role != "Admin")
                return BadRequest(new { Message = "Invalid role" });

            userRole.Role = dto.Role;
            await _supabaseClient.From<UserRole>().Update(userRole);
            return Ok(new { Message = "Role updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}