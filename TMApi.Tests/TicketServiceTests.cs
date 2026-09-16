using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMApi.Data;
using TMApi.Models;
using TMApi.Services;

namespace TMApi.Tests
{
    public class TicketServiceTests
    {
        private readonly Mock<ILogger<TicketService>> _loggerMock;

        public TicketServiceTests()
        {
            _loggerMock = new Mock<ILogger<TicketService>>();
        }

        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task CreateTicketAsync_ShouldCreateTicket()
        {
            // Arrange
            var context = CreateInMemoryDbContext();
            var service = new TicketService(context, _loggerMock.Object);
            var dto = new CreateTicketDto
            {
                Title = "Computer not working",
                Description = "My computer not starting up.",
                Category = TicketCategory.Hardware
            };

            var userId = "123";

            // Act
            var createdTicket = await service.CreateTicketAsync(dto, userId);

            // Assert
            Assert.NotNull(createdTicket);
            Assert.Equal(dto.Title, createdTicket.Title);

            Assert.Equal(dto.Description, createdTicket.Description);
            Assert.Equal(dto.Category, createdTicket.Category);

            Assert.Equal(TicketStatus.Open, createdTicket.Status);
            Assert.Equal(TicketPriority.Medium, createdTicket.Priority);

            Assert.Equal(userId, createdTicket.CreatedByUserId);

            var savedTicket = await context.Tickets
            .FirstOrDefaultAsync(t => t.Id == createdTicket.Id);

            Assert.NotNull(savedTicket);
        }

        [Fact]
        public async Task GetTicketAsync_WhenTicketExist_ReturnTicket()
        {
            // Arrange
            var context = CreateInMemoryDbContext();
            
            var ticket = new Ticket()
            {
                Title = "Network issue",
                Description = "Internet connection is down",
                Category = TicketCategory.Network,
                Status = TicketStatus.Open,
                Priority = TicketPriority.High,
                CreatedByUserId = "user-123",
                CreatedAt = DateTime.UtcNow,
            };

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var service = new TicketService(context, _loggerMock.Object);

            //Act 
            var result = await service.GetTicketAsync(ticket.Id);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(ticket.Id, result.Id);
            Assert.Equal(ticket.Title, result.Title);
            Assert.Equal(ticket.Description, result.Description);
            Assert.Equal(ticket.Category, result.Category);
            Assert.Equal(ticket.Status, result.Status);
            Assert.Equal(ticket.Priority, result.Priority);
            Assert.Equal(ticket.CreatedByUserId, result.CreatedByUserId);
        }
        
        [Fact]
        public async Task GetTicketAsync_WhenTicketDoesntExist_ReturnNull()
        {
            //Arrange
            await using var context = CreateInMemoryDbContext();

            var service = new TicketService(context,_loggerMock.Object);

            //Act
            var result = await service.GetTicketAsync(999);

            //Assert
            Assert.Null(result);
        }
       
        [Fact]
        public async Task GetTicketsAsync_ShouldReturnAllTickets()
        {
            // Arrange
            var context = CreateInMemoryDbContext();
            var service = new TicketService(context, _loggerMock.Object);
            var tickets = new List<Ticket>
            {
                new Ticket 
                {  
                    Title = "Issue 1",
                    Description = "Description 1",
                    Category = TicketCategory.General,
                    CreatedByUserId = "user1",
                    Priority = TicketPriority.Medium, 
                    Status = TicketStatus.Open, 
                    CreatedAt = DateTime.UtcNow 
                },
                new Ticket 
                { 
                    Title = "Issue 2",
                    Description = "Description 2",
                    Category = TicketCategory.Network, 
                    CreatedByUserId = "user2",
                    Priority = TicketPriority.High, 
                    Status = TicketStatus.Open, 
                    CreatedAt = DateTime.UtcNow 
                }
            };

            context.Tickets.AddRange(tickets);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetTicketsAsync();
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

   
        [Fact]
        public async Task UpdateTicketAsync_WhenTicketExists_ReturnTicket()
        {
            //Arrange
            await using var context = CreateInMemoryDbContext();    

            var service = new TicketService(context, _loggerMock.Object);

            var ticket = new Ticket
            {
                Title = "Original title",
                Description = "Original description",
                Category = TicketCategory.General,
                Status = TicketStatus.Open,
                CreatedByUserId = "user-123",
                CreatedAt = DateTime.UtcNow,
            };

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var dto = new UpdateTicketDto
            {
                Title = "Update title",
                Description = "Update description",
                Category = TicketCategory.Software,
            };

            //Act
            var result = await service.UpdateTicketAsync(ticket.Id, dto);

            //Assert
            Assert.NotNull(result);
            Assert.Equal("Update title", result.Title);
            Assert.Equal("Update description",result.Description);
            Assert.Equal(TicketCategory.Software, result.Category);
            Assert.NotNull(result.UpdatedAt);

            var updatedTicket = await context.Tickets
                .FirstOrDefaultAsync(t => t.Id == ticket.Id);

            Assert.Equal("Update title", updatedTicket.Title);
            Assert.Equal("Update description", updatedTicket.Description);
            Assert.Equal(TicketCategory.Software, updatedTicket.Category);
        }
        
        [Fact]
        public async Task UpdateTicketAsync_WhenTicketDoesNotExist_ReturnNull()
        {
            //Arrange
            await using var context = CreateInMemoryDbContext();
         
            var service = new TicketService(context, _loggerMock.Object);
            
            var dto = new UpdateTicketDto
            {
                Title = "Update title",
                Description = "Update description",
                Category = TicketCategory.Software,
            };
            
            //Act
            var result = await service.UpdateTicketAsync(999, dto);
            
            //Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteTaskAsync_WhenTicketExists_RetuurnTrue()
        {
            //Arrange
            await using var context = CreateInMemoryDbContext();
            
            var service = new TicketService(context, _loggerMock.Object);
            
            var ticket = new Ticket
            {
                Title = "Title",
                Description = "Description",
                Category = TicketCategory.General,
                Status = TicketStatus.Open,
                CreatedByUserId = "user-123",
                CreatedAt = DateTime.UtcNow,
            };
            
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();
            
            //Act
            var result = await service.DeleteTicketAsync(ticket.Id);
            
            //Assert
            Assert.True(result);
            
            var deletedTicket = await context.Tickets
                .FirstOrDefaultAsync(t => t.Id == ticket.Id);
            
            Assert.Null(deletedTicket);
        }

        [Fact]
        public async Task DeleteTaskAsync_WhenTicketDoesNotExist_ReturnFalse()
        {
            //Arrange
            await using var context = CreateInMemoryDbContext();
            
            var service = new TicketService(context, _loggerMock.Object);
            
            //Act
            var result = await service.DeleteTicketAsync(999);
            
            //Assert
            Assert.False(result);
        }
    }
}
