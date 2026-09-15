using Microsoft.EntityFrameworkCore;
using TMApi.Data;
using TMApi.Models;

namespace TMApi.Services
{
    public class TicketService : ITicketService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TicketService> _logger;
        public TicketService(AppDbContext context, ILogger<TicketService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Ticket>> GetTicketsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all tickets");

                var tickets = await _context.Tickets
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation("Retrieving {TicketCount} tickets", tickets.Count);
                return tickets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving tickets.");

                throw;
            }

        }

        public async Task<Ticket?> GetTicketAsync(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving ticket with ID {TicketId}.", id);

                var ticket = await _context.Tickets
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (ticket == null)
                {
                    _logger.LogWarning("Ticket with ID {TicketId} was not found.", id);
                }

                return ticket;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving ticket {TicketId}.", id);

                throw;
            }
        }

        public async Task<Ticket> CreateTicketAsync(CreateTicketDto dto, string userId)
        {
            try
            {
                _logger.LogInformation("Creating a new ticket for user {UserId}.", userId);

                var ticket = new Ticket
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    CreatedByUserId = userId,
                    Status = TicketStatus.Open,
                    Priority = TicketPriority.Medium,
                    Category = dto.Category,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Tickets.Add(ticket);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Ticket with ID {TicketId} created successfully for user {UserId}.", ticket.Id, userId);

                return ticket;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a new ticket for user {UserId}.", userId);

                throw;
            }
        }

        public async Task<Ticket?> UpdateTicketAsync(int id, UpdateTicketDto dto)
        {
            try
            {
                _logger.LogInformation("Updating ticket with ID {TicketId}.", id);

                var ticket = await _context.Tickets.FindAsync(id);

                if (ticket == null)
                {
                    _logger.LogWarning("Ticket with ID {TicketId} was not found.", id);
                    return null;
                }

                ticket.Title = dto.Title;
                ticket.Description = dto.Description;
                ticket.Category = dto.Category;
                ticket.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Ticket with ID {TicketId} updated successfully.", id);

                return ticket;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating ticket with ID {TicketId}.", id);

                throw;
            }
        }

        public async Task<bool> DeleteTicketAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting ticket with ID {TicketId}.", id);

                var ticket = await _context.Tickets.FindAsync(id);

                if (ticket == null)
                {
                    _logger.LogWarning("Ticket with ID {TicketId} was not found.", id);
                    return false;
                }

                _context.Tickets.Remove(ticket);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Ticket with ID {TicketId} deleted successfully.", id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting ticket with ID {TicketId}.", id);
               
                throw;
            }
        }
    } 
}
