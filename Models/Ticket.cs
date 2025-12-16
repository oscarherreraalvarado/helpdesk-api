namespace Backend.Api.Models;

public class Ticket
{
    public int Id { get; set; }
    public string TicketId { get; set; } = "";   // ejemplo: "TCK-1024" (como en tu UI)
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";

    public string Status { get; set; } = "Open";       // Open/InProgress/Resolved/Closed
    public string Priority { get; set; } = "Medium";   // Low/Medium/High/Critical
    public string Category { get; set; } = "General";

    public int RequesterId { get; set; }               // quien lo crea (User)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
