namespace TMApi.Models  
{
    public class UpdateTicketDto
    {
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public TicketCategory Category { get; set; } = TicketCategory.General;
    }
}
