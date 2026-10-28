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
    public class TicketController : ControllerBase
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

            if (string.IsNullOrEmpty(userId))
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

        
        [HttpPost("{id}/adopt")]
        public async Task<ActionResult<Ticket>> AdoptTicket(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            var ticket = await _ticketService.AdoptTicketAsync(id, userId);
            if (ticket == null)
                return NotFound();
            return Ok(ticket);
        }

        [HttpGet("agents")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IList<AgentDto>>> GetAgents()
        {
            var agents = await _ticketService.GetAgentsAsync();

            return Ok(agents);
        }

        [HttpPost("{id}/assign")]
        [Authorize(Roles ="Admin")]
        public async Task<ActionResult<Ticket>> AssignTicket(int id, [FromBody] AssignTicketDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.AgentUserId))
            {
                return BadRequest("Agent user ID is required.");
            }

            var ticket = await _ticketService.AssignTicketAsync(id, dto.AgentUserId);

            if (ticket == null)
                return NotFound();
            return Ok(ticket);
        }


        #region Ticket Comments
        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            var comment = await _ticketService.AddCommentAsync(id, userId, dto.Content, dto.IsInternal);

            if (comment == null)
                return NotFound();

            return Ok(comment);
        }

        [HttpGet("{id}/comments")]
        public async Task<IActionResult> GetComments(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var comments = await _ticketService.GetCommentsAsync(id, userId );
            
            if (comments == null)
                return NotFound();
            
            return Ok(comments);
        }
        #endregion
    }
}
