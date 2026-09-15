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

    }
}
