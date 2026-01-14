using Backend.Api.Data;
using Backend.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Api.Controllers;

[Authorize]
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

        // single aggregated query to reduce multiple round-trips
        var agg = await _db.Tickets.AsNoTracking()
            .GroupBy(t => 1)
            .Select(g => new {
                Total = g.Count(),
                Open = g.Sum(t => t.Status == "Open" ? 1 : 0),
                InProgress = g.Sum(t => t.Status == "InProgress" ? 1 : 0),
                Resolved = g.Sum(t => t.Status == "Resolved" ? 1 : 0),
                Closed = g.Sum(t => t.Status == "Closed" ? 1 : 0),
                CreatedLast7 = g.Sum(t => t.CreatedAt >= last7 ? 1 : 0)
            })
            .FirstOrDefaultAsync();

        var total = agg?.Total ?? 0;
        var open = agg?.Open ?? 0;
        var inProgress = agg?.InProgress ?? 0;
        var resolved = agg?.Resolved ?? 0;
        var closed = agg?.Closed ?? 0;
        var createdLast7 = agg?.CreatedLast7 ?? 0;

        // counts per priority (all tickets)
        var byPriority = await _db.Tickets.AsNoTracking()
            .GroupBy(t => t.Priority)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToListAsync();

        // optimized: open tickets grouped by priority (filtered first)
        var openByPriority = await _db.Tickets.AsNoTracking()
            .Where(t => t.Status == "Open")
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
            ,
            OpenByPriority = openByPriority
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .ToDictionary(x => x.Key!, x => x.Count)
        };

        return Ok(summary);
    }
}
