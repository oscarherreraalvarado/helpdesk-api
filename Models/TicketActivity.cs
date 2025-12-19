namespace Backend.Api.Models;

public class TicketActivity
{
    public int Id { get; set; }
    public int TicketId { get; set; }

    public string Type { get; set; } = "Comment"; // Created, StatusChanged, Comment, Assigned
    public string Message { get; set; } = "";

    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }

    public int? CreatedById { get; set; } // user opcional para demo
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
