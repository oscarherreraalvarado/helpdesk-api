namespace Backend.Api.Models;

public class UserDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
}
