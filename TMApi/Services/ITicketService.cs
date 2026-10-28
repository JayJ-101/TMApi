using TMApi.Models;

namespace TMApi.Services
{
    public interface ITicketService
    {
        Task<List<Ticket>> GetTicketsAsync();

        Task<Ticket?> GetTicketAsync(int id);  

        Task<Ticket> CreateTicketAsync(CreateTicketDto dto, string userId);

        Task<Ticket?> UpdateTicketAsync(int id, UpdateTicketDto dto);

        Task<bool> DeleteTicketAsync(int id);


        Task<Ticket?> AdoptTicketAsync(int id, string userId);
        Task<Ticket?> AssignTicketAsync(int id, string userId);

        Task<IList<AgentDto>> GetAgentsAsync();

        // Comment-related methods
        Task<TicketComment> AddCommentAsync(int ticketId, string userId, string content, bool isInternal);

        Task<IList<TicketComment>> GetCommentsAsync(int ticketId, string userId);
    }
}
 