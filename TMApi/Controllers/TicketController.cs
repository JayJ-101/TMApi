using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TMApi.Data;
using TMApi.Models;
using TMApi.Services;

namespace TMApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] 
    [Authorize]
    public class TicketController    : ControllerBase
    {
        private readonly ITicketService _ticketService;

        public TicketController(ITicketService ticketService)
        {
             _ticketService = ticketService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Ticket>>> GetTickets()
        {
            var tickets = await _ticketService.GetTicketsAsync();

            return Ok(tickets);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTicket(int id)
        {
            var ticket = await _ticketService.GetTicketAsync(id);

            if (ticket == null)
                return NotFound();

            return Ok(ticket);
        }

        [HttpPost]
        public async Task<ActionResult<Ticket>> CreateTicket(CreateTicketDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if(string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var ticket = await _ticketService.CreateTicketAsync(dto, userId);

            return CreatedAtAction(
                nameof(GetTicket),
                new { id = ticket.Id },
                ticket);
        }

        
        
        [HttpPut("{id}")]
        public async Task<ActionResult<Ticket>> UpdateTicket(int id, UpdateTicketDto dto)
        {
            var ticket = await _ticketService.UpdateTicketAsync(
                 id,
                 dto);

            if (ticket == null)
                return NotFound();

            return Ok(ticket);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            var deleted = await _ticketService.DeleteTicketAsync(id);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
