namespace Backend.Api.Models;

public class DashboardSummary
{
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int InProgressTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int ClosedTickets { get; set; }

    public int CreatedLast7Days { get; set; }

    public Dictionary<string, int> ByPriority { get; set; } = new();
    public Dictionary<string, int> ByCategory { get; set; } = new();
    public Dictionary<string, int> OpenByPriority { get; set; } = new();
}
