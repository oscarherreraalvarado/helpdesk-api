using Backend.Api.Data;
using Backend.Api.Models;
using Backend.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Email y Password son requeridos.");

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == req.Email);

        if (user == null || !PasswordHasher.Verify(req.Password, user.PasswordHash))
            return Unauthorized("Credenciales inválidas.");

        var token = GenerateJwt(user);

        var userDto = new UserDto
        {
            Id = user.Id,
            Nombre = user.Nombre,
            Email = user.Email,
            Role = user.Role
        };

        return Ok(new { token, user = userDto });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Nombre) ||
            string.IsNullOrWhiteSpace(req.Email) ||
            string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Nombre, Email y Password son requeridos.");

        var email = req.Email.Trim().ToLowerInvariant();

        // ¿ya existe usuario con ese email?
        var exists = await _db.Users.AnyAsync(u => u.Email == email);
        if (exists)
            return Conflict("Ya existe un usuario con ese email.");

        // crear usuario
        var user = new User
        {
            Nombre = req.Nombre.Trim(),
            Email = email,
            //Role = string.IsNullOrWhiteSpace(req.Role) ? "User" : req.Role.Trim()
            Role = "User"
        };

        // hashear password (con tu clase PasswordHasher)
        user.PasswordHash = PasswordHasher.Hash(req.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // opcional: devolver token ya logueado automáticamente
        var token = GenerateJwt(user);

        var userDto = new UserDto
        {
            Id = user.Id,
            Nombre = user.Nombre,
            Email = user.Email,
            Role = user.Role
        };

        return Created("", new { token, user = userDto });
    }

    private string GenerateJwt(User user)
    {
        var jwt = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("nombre", user.Nombre),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var expires = DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiresMinutes"]!));

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RegisterRequest
{
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "User"; // opcional, por defecto User
}
