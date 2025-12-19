namespace Backend.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";

    // Login demo (para práctica)
    public string Password { get; set; } = "";

    // Admin / Agent / Requester
    public string Role { get; set; } = "Agent";
}
