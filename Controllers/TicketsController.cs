using Backend.Api.Data;
using Backend.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Backend.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;
    public TicketsController(AppDbContext db) => _db = db;

    // Lista tipo "Queue" (como tu panel)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? q)
    {
        var query = _db.Tickets.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(t => t.TicketId.Contains(q) || t.Title.Contains(q));

        var list = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ticket = await _db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        return ticket == null ? NotFound() : Ok(ticket);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest("Title requerido.");
        if (string.IsNullOrWhiteSpace(req.Description))
            return BadRequest("Description requerido.");

        // ✅ UserId desde JWT (sub / nameidentifier)
        var userIdStr =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue("sub");

        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized("Token inválido (sin userId).");

        var ticket = new Ticket
        {
            TicketId = $"TCK-{DateTime.UtcNow:yyyyMMddHHmmss}",
            Title = req.Title.Trim(),
            Description = req.Description.Trim(),
            Status = string.IsNullOrWhiteSpace(req.Status) ? "Open" : req.Status,
            Priority = string.IsNullOrWhiteSpace(req.Priority) ? "Medium" : req.Priority,
            Category = string.IsNullOrWhiteSpace(req.Category) ? "General" : req.Category,
            RequesterId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        _db.TicketActivities.Add(new TicketActivity
        {
            TicketId = ticket.Id,
            Type = "Created",
            Message = "Ticket creado",
            ToStatus = ticket.Status,
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return Ok(ticket);
    }

    public class CreateTicketRequest
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "Open";
        public string Priority { get; set; } = "Medium";
        public string Category { get; set; } = "General";
    }


    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Status))
            return BadRequest("Status requerido.");

        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket == null)
            return NotFound("Ticket no existe.");

        // ✅ UserId desde JWT (sub / nameidentifier)
        var userIdStr =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue("sub");

        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized("Token inválido (sin userId).");

        var oldStatus = ticket.Status;

        ticket.Status = req.Status;
        ticket.UpdatedAt = DateTime.UtcNow;

        _db.TicketActivities.Add(new TicketActivity
        {
            TicketId = ticket.Id,
            Type = "StatusChanged",
            Message = $"Estado cambiado de {oldStatus} a {req.Status}",
            FromStatus = oldStatus,
            ToStatus = req.Status,
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return Ok(ticket);
    }

    [HttpGet("{id:int}/timeline")]
    public async Task<IActionResult> GetTimeline(int id)
    {
        var exists = await _db.Tickets.AsNoTracking().AnyAsync(t => t.Id == id);
        if (!exists) return NotFound();

        var timeline = await (from a in _db.TicketActivities.AsNoTracking()
                              join u in _db.Users.AsNoTracking()
                                on a.CreatedById equals u.Id into uu
                              from u in uu.DefaultIfEmpty()
                              where a.TicketId == id
                              orderby a.CreatedAt descending
                              select new
                              {
                                  a.Id,
                                  a.Type,
                                  a.Message,
                                  a.FromStatus,
                                  a.ToStatus,
                                  a.CreatedById,
                                  a.CreatedAt,
                                  CreatedByName = u != null ? u.Nombre : null
                              }).ToListAsync();

        return Ok(timeline);
    }


    public class UpdateStatusRequest
    {
        public string Status { get; set; } = "";
    }

}