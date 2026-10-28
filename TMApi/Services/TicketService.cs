using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TMApi.Data;
using TMApi.Models;
using System.Linq;

namespace TMApi.Services
{
    public class TicketService : ITicketService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TicketService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        public TicketService(AppDbContext context, ILogger<TicketService> logger, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<List<Ticket>> GetTicketsAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Fetching all tickets for user {UserId}", userId);

                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                    throw new UnauthorizedAccessException("User not found.");

                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                var isAgent = await _userManager.IsInRoleAsync(user, "Agent");
                var isRequester = await _userManager.IsInRoleAsync(user, "user");


                if (!isAdmin && !isAgent && !isRequester)
                    throw new UnauthorizedAccessException("Invalid user role.");

                var query = _context.Tickets.AsNoTracking();

                if (!isAdmin && !isAgent)
                    query = query.Where(t => t.CreatedByUserId == userId);

                var tickets = await query.ToListAsync();

                _logger.LogInformation("Retrieving {TicketCount} tickets", tickets.Count);
                
                return tickets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving tickets.");

                throw;
            }

        }

        public async Task<Ticket?> GetTicketAsync(int id, string userId)
        {
            try
            {
                _logger.LogInformation("Retrieving ticket with ID {TicketId} for user {UserId}.", id, userId);

                var user = await _userManager.FindByIdAsync(userId);
                
                if (user == null)
                    throw new UnauthorizedAccessException("User not found.");

                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                var isAgent = await _userManager.IsInRoleAsync(user, "Agent");
                var isRequester = await _userManager.IsInRoleAsync(user, "user");

                if (!isAdmin && !isAgent && !isRequester)
                    throw new UnauthorizedAccessException("Invalid user role.");

                var query = _context.Tickets
                .AsNoTracking()
                .Where(t => t.Id == id);

                if (!isAdmin && !isAgent)
                    query = query.Where(t => t.CreatedByUserId == userId);

                var ticket = await query.FirstOrDefaultAsync();

                if (ticket == null)
                {
                    _logger.LogWarning("Ticket with ID {TicketId} was not found for user {UserId}.", id, userId);
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

        public async Task<Ticket?> AdoptTicketAsync(int id, string userId)
        {
            try
            {
                _logger.LogInformation("User {UserId} is attempting to adopt ticket with ID {TicketId}.", userId, id);

                var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);

                if (ticket == null)
                {
                    _logger.LogWarning("Ticket with ID {TicketId} was not found.", id);
                    return null;
                }

               if(ticket.AssignedUserId != null)
               {
                    _logger.LogWarning("Ticket with ID {TicketId} is already assigned to user {AssignedUserId}.", id, ticket.AssignedUserId);
                    return null;
               }

                ticket.AssignedUserId = userId;
                ticket.Status = TicketStatus.InProgress;
                ticket.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Ticket with ID {TicketId} successfully adopted by user {UserId}.", id, userId);

                return ticket;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adopting ticket with ID {TicketId} by user {UserId}.", id, userId);
                throw;
            }
        }

        public async Task<Ticket?> AssignTicketAsync(int id, string userId)
        {
            try
            {
                _logger.LogInformation("Assigning ticket with ID {TicketId} to user {UserId}.", id, userId);

                //gets the ticket by id
                var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
                if (ticket == null)
                {
                    _logger.LogWarning("Ticket with ID {TicketId} was not found.", id);
                    return null;
                }

                //get users with agent role
                var agent = await _userManager.FindByIdAsync(userId);
                //check if user is an agent
                if (agent == null || !await _userManager.IsInRoleAsync(agent, "Agent"))
                {
                    _logger.LogWarning("User with ID {UserId} was not found or is not an agent.", userId);
                    return null;
                }

                ticket.AssignedUserId = agent.Id;
                ticket.Status = TicketStatus.InProgress;
                ticket.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Ticket with ID {TicketId} successfully assigned to user {UserId}.", id, userId);
                
                return ticket;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while assigning ticket with ID {TicketId} to user {UserId}.", id, userId);
                throw;
            }
        }

        public async Task<IList<AgentDto>> GetAgentsAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all users with the 'Agent' role.");
                var agents = await _userManager.GetUsersInRoleAsync("Agent");
                _logger.LogInformation("Retrieved {AgentCount} agents.", agents.Count);
                return agents.Select(agent => new AgentDto
                {
                    Id = agent.Id,
                    UserName = agent.UserName,
                    FullName = agent.FullName
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving agents.");
                throw;
            }
        }

        public async Task<TicketComment> AddCommentAsync(int ticketId, string userId, string content, bool isInternal)
        {
            try
            {
                _logger.LogInformation("Adding a comment to ticket with ID {TicketId} by user {UserId}.", ticketId, userId);

                var ticket = _context.Tickets.FirstOrDefault(t => t.Id == ticketId);

                if (ticket == null)
                {
                    _logger.LogWarning("Ticket with ID {TicketId} was not found.", ticketId);
                    throw new KeyNotFoundException($"Ticket with ID {ticketId} not found.");
                }

                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} was not found.", userId);
                    throw new UnauthorizedAccessException($"User with ID {userId} not found.");
                }

                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                var isAgent = await _userManager.IsInRoleAsync(user, "Agent");
                var isRequester = await _userManager.IsInRoleAsync(user,"Requester");

                //Only admin and agent may create internal comments.
                if (isInternal && !isAdmin && !isAgent)
                    throw new UnauthorizedAccessException("You are not authorized to create internal notes.");
                
                
                if(isRequester && !isAdmin && !isAgent && ticket.CreatedByUserId !=userId)
                    throw new UnauthorizedAccessException("You are not authorized to create comments on this ticket.");


                //Agent may only comment on tickets assigned to them.
                if (isAgent && !isAdmin && ticket.AssignedUserId != userId)
                    throw new UnauthorizedAccessException("You are not authorized to create comments on this ticket.");

                var comment = new TicketComment
                {
                    Content = content,
                    IsInternal = isInternal,
                    CreatedAt = DateTime.UtcNow,
                    TicketId = ticketId,
                    UserId = userId
                };

                _context.TicketComments.Add(comment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Comment added to ticket with ID {TicketId} by user {UserId}.", ticketId, userId);

                return comment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding a comment to ticket with ID {TicketId} by user {UserId}.", ticketId, userId);
                throw;
            }
        }

        public async Task<IList<TicketComment>> GetCommentsAsync(int ticketId, string userId)
        {
            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == ticketId);

            if (ticket == null)
            {
                _logger.LogWarning("Ticket with ID {TicketId} was not found.", ticketId);
                throw new KeyNotFoundException($"Ticket with ID {ticketId} not found.");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if(user == null) 
                throw new UnauthorizedAccessException($"User with ID {userId} not found.");

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            var isAgent = await _userManager.IsInRoleAsync(user, "Agent");
            var isRequester = await _userManager.IsInRoleAsync(user, "Requester");


            if(!isAdmin && !isAgent && !isRequester)
                throw new UnauthorizedAccessException("You are not authorized to view comments on this ticket.");

            if(isRequester && !isAdmin && !isAgent && ticket.CreatedByUserId != userId)
                    throw new UnauthorizedAccessException("You are not authorized to view comments on this ticket.");

            var comments =  _context.TicketComments
                .AsNoTracking()
                .Where(c => c.TicketId == ticketId);

            //Filter out internal comments for non-admin and non-agent users
            if(!isAdmin && !isAgent)
            {
                comments = comments.Where(c => !c.IsInternal);
            }


            return await comments
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();




        }
    } 
}
