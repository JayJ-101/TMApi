using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
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
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

        public TicketServiceTests()
        {
            _loggerMock = new Mock<ILogger<TicketService>>();

            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
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
            var service = new TicketService(context, _loggerMock.Object, _userManagerMock.Object);
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

            var user = new ApplicationUser
            {
                Id = "user-123",
                UserName = "testuser"
            };

            _userManagerMock
                .Setup(x => x.FindByIdAsync("user-123"))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.IsInRoleAsync(user, "Admin"))
                .ReturnsAsync(false);

            _userManagerMock
                .Setup(x => x.IsInRoleAsync(user, "Agent"))
                .ReturnsAsync(false);

            _userManagerMock
                .Setup(x => x.IsInRoleAsync(user, "user"))
                .ReturnsAsync(true);

            var service = new TicketService(context, _loggerMock.Object, _userManagerMock.Object);

            //Act 
            var result = await service.GetTicketAsync(ticket.Id, "user-123");

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

            var user = new ApplicationUser
            {
                Id = "user-123",
                UserName = "testuser"
            };

            _userManagerMock
                .Setup(x => x.FindByIdAsync("user-123"))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.IsInRoleAsync(user, "Admin"))
                .ReturnsAsync(false);

            _userManagerMock
                .Setup(x => x.IsInRoleAsync(user, "Agent"))
                .ReturnsAsync(false);

            _userManagerMock
                .Setup(x => x.IsInRoleAsync(user, "user"))
                .ReturnsAsync(true);

            var service = new TicketService(context, _loggerMock.Object, _userManagerMock.Object);

            //Act
            var result = await service.GetTicketAsync(999, "user-123");

            //Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetTicketsAsync_Requester_ReturnAllTickets()
        {
            // Arrange
            var context = CreateInMemoryDbContext();

            var user = new ApplicationUser
            {
                Id = "user1",
                UserName = "requester1"
            };

            _userManagerMock
                .Setup(x => x.FindByIdAsync("user1"))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x=>x.IsInRoleAsync(user,"Admin"))
                .ReturnsAsync(false);

            _userManagerMock
                .Setup(x=>x.IsInRoleAsync(user,"Agent"))
                .ReturnsAsync(false);

            _userManagerMock
                .Setup(x=>x.IsInRoleAsync(user,"user"))
                .ReturnsAsync(true);

            var service = new TicketService(context, _loggerMock.Object, _userManagerMock.Object);

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
            var result = await service.GetTicketsAsync("user1");

            // Assert
            Assert.Single(result);
            Assert.Equal("user1", result[0].CreatedByUserId);
        }


        [Fact]
        public async Task UpdateTicketAsync_WhenTicketExists_ReturnTicket()
        {
            //Arrange
            await using var context = CreateInMemoryDbContext();

            var service = new TicketService(context, _loggerMock.Object, _userManagerMock.Object);

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
            Assert.Equal("Update description", result.Description);
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

            var service = new TicketService(context, _loggerMock.Object, _userManagerMock.Object);

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

            var service = new TicketService(context, _loggerMock.Object, _userManagerMock.Object);

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

            var service = new TicketService(context, _loggerMock.Object, _userManagerMock.Object);

            //Act
            var result = await service.DeleteTicketAsync(999);

            //Assert
            Assert.False(result);
        }

        [Fact]
        public async Task WhenTicketIsUnassigned_ShouldAssignAndSetInProgress()
        {
            //Arrange 
            await using var context = CreateInMemoryDbContext();

            var ticket = new Ticket
            {
                Id = 1,
                Title = "Laptop issue",
                Description = "Laptop will not start",
                CreatedByUserId = "requester-1",
                AssignedUserId = null,
                Status = TicketStatus.Open
            };

            context.Tickets.Add(ticket);

            await context.SaveChangesAsync();

            var service = new TicketService(
                context,
                _loggerMock.Object,
                _userManagerMock.Object);

            // Act
            var result = await service.AdoptTicketAsync(1, "agent-1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("agent-1", result.AssignedUserId);
            Assert.Equal(TicketStatus.InProgress, result.Status);
            Assert.NotNull(result.UpdatedAt);
        }

        [Fact]
        public async Task AdoptTicketAsync_WhenTicketDoesNotExist_ReturnNull()
        {
            //Arrange
            await using var context = CreateInMemoryDbContext();

            var service = new TicketService(context, _loggerMock.Object, _userManagerMock.Object);

            //Act
            var result = await service.AdoptTicketAsync(999, "agent-1");

            //Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AdoptTicketAsync_WhenTicketIsAlreadyAssigned_ReturnNull()
        {
            //Arrange
            await using var context = CreateInMemoryDbContext();

            var ticket = new Ticket
            {
                Id = 1,
                Title = "Laptop issue",
                Description = "Laptop will not start",
                CreatedByUserId = "requester-1",
                AssignedUserId = "agent-2",
                Status = TicketStatus.InProgress
            };

            context.Tickets.Add(ticket);

            await context.SaveChangesAsync();

            var service = new TicketService(context, _loggerMock.Object, _userManagerMock.Object);

            //Act
            var result = await service.AdoptTicketAsync(1, "agent-1");

            //Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AssignTicketAsync_WhenUserIsAgent_AssignTicket()
        {
            // Arrange
            await using var context = CreateInMemoryDbContext();

            var ticket = new Ticket
            {
                Id = 1,
                Title = "Laptop issue",
                Description = "Laptop will not start",
                CreatedByUserId = "requester-1",
                Status = TicketStatus.Open
            };

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var agent = new ApplicationUser
            {
                Id = "agent-1",
                UserName = "agent1@example.com"
            };

            _userManagerMock
                .Setup(x => x.FindByIdAsync("agent-1"))
                .ReturnsAsync(agent);

            _userManagerMock
                .Setup(x => x.IsInRoleAsync(agent, "Agent"))
                .ReturnsAsync(true);

            var service = new TicketService(
                context, _loggerMock.Object
                , _userManagerMock.Object);

            // Act
            var result = await service.AssignTicketAsync(1, "agent-1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("agent-1", result.AssignedUserId);
            Assert.Equal(TicketStatus.InProgress, result.Status);
            Assert.NotNull(result.UpdatedAt);

            _userManagerMock.Verify(x => x.FindByIdAsync("agent-1"), Times.Once);
            _userManagerMock.Verify(x => x.IsInRoleAsync(agent, "Agent"), Times.Once);
        }

        [Fact]
        public async Task AssignTicketAsync_WhenUserIsNotAgent_ReturnNull()
        {
            // Arrange
            await using var context = CreateInMemoryDbContext();
            var ticket = new Ticket
            {
                Id = 1,
                Title = "Laptop issue",
                Description = "Laptop will not start",
                CreatedByUserId = "requester-1",
                Status = TicketStatus.Open
            };
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();
            var user = new ApplicationUser
            {
                Id = "user-1",
                UserName = "requester1@example.com"
            };


            _userManagerMock
                .Setup(x => x.FindByIdAsync("requester-1"))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.IsInRoleAsync(user, "Agent"))
                .ReturnsAsync(false);


            var service = new TicketService(context, _loggerMock.Object, _userManagerMock.Object);


            //Act
            var result = await service.AssignTicketAsync(1, "requester-1");

            //Assert
            Assert.Null(result);

            var unchangedTicket = await context.Tickets.FindAsync(1);

            Assert.NotNull(unchangedTicket);
            Assert.Null(unchangedTicket.AssignedUserId);
            Assert.Equal(TicketStatus.Open, unchangedTicket.Status);
        }


        [Fact]
        public async Task AssignTicketAsync_WhenUserDoesNotExist_ReturnsNull()
        {
            // Arrange
            await using var context = CreateInMemoryDbContext();

            var ticket = new Ticket { 
                Id = 1, 
                Title = "Laptop issue",
                Description = "Laptop will not start", 
                CreatedByUserId = "requester-1",
                Status = TicketStatus.Open 
            };

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            _userManagerMock
                .Setup(x => x.FindByIdAsync("unknown-user"))
                .ReturnsAsync((ApplicationUser?)null);

            var service = new TicketService(
                context,
                _loggerMock.Object,
                _userManagerMock.Object);

            // Act
            var result = await service.AssignTicketAsync(1, "unknown-user");

            // Assert
            Assert.Null(result);

            var unchangedTicket = await context.Tickets.FindAsync(1);

            Assert.NotNull(unchangedTicket);
            Assert.Null(unchangedTicket.AssignedUserId);
            Assert.Equal(TicketStatus.Open, unchangedTicket.Status);

            _userManagerMock.Verify(
                x => x.IsInRoleAsync(
                    It.IsAny<ApplicationUser>(),
                    "Agent"),
                Times.Never);
        }


        [Fact]
        public async Task GetAgentsAsync_ShouldReturnAgents()
        {
            // Arrange
            var agents = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = "agent-1",
                    UserName = "john@company.com",
                    FullName = "John Smith"
                },
                new ApplicationUser
                {
                    Id = "agent-2",
                    UserName = "mary@company.com",
                    FullName = "Mary Jones"
                }
            };

            _userManagerMock
                .Setup(x => x.GetUsersInRoleAsync("Agent"))
                .ReturnsAsync(agents);

            await using var context = CreateInMemoryDbContext();

            var service = new TicketService(
                context,
                _loggerMock.Object,
                _userManagerMock.Object);

            // Act
            var result = await service.GetAgentsAsync();

            // Assert
            Assert.Equal(2, result.Count);

            var firstAgent = result.Single(agent => agent.Id == "agent-1");
            Assert.Equal("john@company.com", firstAgent.UserName);
            Assert.Equal("John Smith", firstAgent.FullName);

            var secondAgent = result.Single(agent => agent.Id == "agent-2");
            Assert.Equal("mary@company.com", secondAgent.UserName);
            Assert.Equal("Mary Jones", secondAgent.FullName);

            _userManagerMock.Verify(
                x => x.GetUsersInRoleAsync("Agent"),
                Times.Once);
        }

        [Fact]
        public async Task GetAgentsAsync_WhenNoAgentsExist_ReturnsEmptyList()
        {
            // Arrange
            _userManagerMock
                .Setup(x => x.GetUsersInRoleAsync("Agent"))
                .ReturnsAsync(new List<ApplicationUser>());

            await using var context = CreateInMemoryDbContext();

            var service = new TicketService(
                context,
                _loggerMock.Object,
                _userManagerMock.Object);

            // Act
            var result = await service.GetAgentsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            _userManagerMock.Verify(
                x => x.GetUsersInRoleAsync("Agent"),
                Times.Once);
        }




    }

    
}
