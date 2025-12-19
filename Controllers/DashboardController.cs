using Backend.Api.Data;
using Backend.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    public DashboardController(AppDbContext db) => _db = db;

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var now = DateTime.UtcNow;
        var last7 = now.AddDays(-7);

        var total = await _db.Tickets.AsNoTracking().CountAsync();
        var open = await _db.Tickets.AsNoTracking().CountAsync(t => t.Status == "Open");
        var inProgress = await _db.Tickets.AsNoTracking().CountAsync(t => t.Status == "InProgress");
        var resolved = await _db.Tickets.AsNoTracking().CountAsync(t => t.Status == "Resolved");
        var closed = await _db.Tickets.AsNoTracking().CountAsync(t => t.Status == "Closed");

        var createdLast7 = await _db.Tickets.AsNoTracking()
            .CountAsync(t => t.CreatedAt >= last7);

        var byPriority = await _db.Tickets.AsNoTracking()
            .GroupBy(t => t.Priority)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToListAsync();

        var byCategory = await _db.Tickets.AsNoTracking()
            .GroupBy(t => t.Category)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToListAsync();

        var summary = new DashboardSummary
        {
            TotalTickets = total,
            OpenTickets = open,
            InProgressTickets = inProgress,
            ResolvedTickets = resolved,
            ClosedTickets = closed,
            CreatedLast7Days = createdLast7,
            ByPriority = byPriority
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .ToDictionary(x => x.Key!, x => x.Count),
            ByCategory = byCategory
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .ToDictionary(x => x.Key!, x => x.Count)
        };

        return Ok(summary);
    }
}
