using Backend.Api.Data;
using Backend.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Controllers;

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
    public async Task<IActionResult> Create(Ticket ticket)
    {
        if (string.IsNullOrWhiteSpace(ticket.TicketId))
            ticket.TicketId = $"TCK-{DateTime.UtcNow:yyyyMMddHHmmss}";

        ticket.CreatedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(); // <-- aquí ya existe ticket.Id

        _db.TicketActivities.Add(new TicketActivity
        {
            TicketId = ticket.Id, // <-- ahora sí correcto
            Type = "Created",
            Message = "Ticket creado",
            ToStatus = ticket.Status,
            CreatedById = ticket.RequesterId,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return Ok(ticket);
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Status))
            return BadRequest("Status requerido.");

        // 1️⃣ Validar que el ticket exista
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket == null)
            return NotFound("Ticket no existe.");

        // 2️⃣ Validar que el usuario exista  ✅ AQUÍ VA
        var userExists = await _db.Users.AsNoTracking()
            .AnyAsync(u => u.Id == req.UserId);

        if (!userExists)
            return BadRequest("UserId no existe.");

        // 3️⃣ Guardar estado anterior
        var oldStatus = ticket.Status;

        // 4️⃣ Actualizar ticket
        ticket.Status = req.Status;
        ticket.UpdatedAt = DateTime.UtcNow;

        // 5️⃣ Registrar actividad con usuario
        _db.TicketActivities.Add(new TicketActivity
        {
            TicketId = ticket.Id,
            Type = "StatusChanged",
            Message = $"Estado cambiado de {oldStatus} a {req.Status}",
            FromStatus = oldStatus,
            ToStatus = req.Status,
            CreatedById = req.UserId,
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
        public int UserId { get; set; }
    }

}
