using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using TMApi.Controllers;
using TMApi.Models;
using TMApi.Services;
using Xunit;

namespace TMApi.Tests
{
    public class TicketControllerTest
    {
        private readonly Mock<ITicketService> _ticketServiceMock;
        private readonly TicketController _ticketController;


        public TicketControllerTest()
        {
            _ticketServiceMock = new Mock<ITicketService>();
            _ticketController = new TicketController(_ticketServiceMock.Object);
        }

        [Fact]
        public async Task GetTickets_WhenTicketsExist_ShouldReturnOk()
        {
            // Arrange
            var tickets = new List<Ticket>
            {
                new Ticket
                {
                    Id = 1,
                    Title = "Computer issue",
                    Description = "Computer will not start.",
                    Category = TicketCategory.Hardware,
                    Status = TicketStatus.Open,
                    Priority = TicketPriority.Medium,
                    CreatedByUserId = "user-123"
                },
                new Ticket
                {
                    Id = 2,
                    Title = "Email issue",
                    Description = "Unable to send email.",
                    Category = TicketCategory.Email,
                    Status = TicketStatus.Open,
                    Priority = TicketPriority.Medium,
                    CreatedByUserId = "user-124"
                }
            };
            _ticketServiceMock
                    .Setup(x => x.GetTicketsAsync())
                    .ReturnsAsync(tickets);

            // Act
            var result = await _ticketController.GetTickets();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedTickets = Assert.IsType<List<Ticket>>(okResult.Value);

            Assert.Equal(2, returnedTickets.Count);
        }

        [Fact]
        public async Task GetTickets_WhenNoTicketsExist_ShouldReturnOk()
        {
            //Arrange 
            var tickets = new List<Ticket>();

            _ticketServiceMock
                .Setup(x => x.GetTicketsAsync())
                .ReturnsAsync(tickets);

            //Sct 
            var result = await _ticketController.GetTickets();

            //Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnTickets = Assert.IsType<List<Ticket>>(okResult.Value);

            Assert.Empty(returnTickets);
        }

        [Fact]
        public async Task GetTicket_WhenTicketExists_ReturnOk()
        {
            //Arrange 
            var ticket = new Ticket
            {
                Id = 1,
                Title = "Computer issue",
                Description = "Computer will not start.",
                Category = TicketCategory.Hardware,
                Status = TicketStatus.Open,
                Priority = TicketPriority.Medium,
                CreatedByUserId = "user-123"
            };

            _ticketServiceMock
                .Setup(x => x.GetTicketAsync(1))
                .ReturnsAsync(ticket);

            //Act
            var result = await _ticketController.GetTicket(1);

            //Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedTicket = Assert.IsType<Ticket>(okResult.Value);

            Assert.Equal(ticket.Id, returnedTicket.Id);
            Assert.Equal(ticket.Title, returnedTicket.Title);
        }

        [Fact]
        public async Task GetTicket_WhenTicketDoesNotExist_ReturnNotFound()
        {
            //Arrange
            _ticketServiceMock
                .Setup(x => x.GetTicketAsync(9999))
                .ReturnsAsync((Ticket?)null);

            //Act
            var result = await _ticketController.GetTicket(9999);

            //Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreateTicket_WhenUserIsAuthenticated_ReturnCreated()
        {
            //Arrange
            var dto = new CreateTicketDto
            {
                Title = "Computer issue",
                Description = "Computer will not start.",
                Category = TicketCategory.Hardware
            };

            var ticket = new Ticket
            {
                Id = 1,
                Title = dto.Title,
                Description = dto.Description,
                Category = dto.Category,
                Status = TicketStatus.Open,
                Priority = TicketPriority.Medium,
                CreatedByUserId = "user-123"

            };

            _ticketServiceMock
            .Setup(x => x.CreateTicketAsync(dto, "user-123"))
            .ReturnsAsync(ticket);

            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, "user-123")
            };

            var identity = new ClaimsIdentity(claims, "TestAuthentication");

            _ticketController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };

            //Act
            var result = await _ticketController.CreateTicket(dto);

            //Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedTicket = Assert.IsType<Ticket>(createdResult.Value);

            Assert.Equal(ticket.Id, returnedTicket.Id);
            Assert.Equal(ticket.Title, returnedTicket.Title);
            Assert.Equal("GetTicket", createdResult.ActionName);
        }


        [Fact]
        public async Task CreateTicket_WhenUserIsNotAuthenticated_ReturnUnauthorized()
        {
            //Arrange
            var dto = new CreateTicketDto
            {
                Title = "Test",
                Description = "Test",
                Category = TicketCategory.General
            };

            _ticketController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };

            //Act
            var result = await _ticketController.CreateTicket(dto);

            //Assert
            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task UpdateTicket_WhenTicketExists_ReturnOk()
        {
            //Arrange
            var dto = new UpdateTicketDto()
            {
                Title = "Update computer issue",
                Description = "Update description",
                Category = TicketCategory.General
            };


            var ticket = new Ticket
            {
                Id = 1,
                Title = dto.Title,
                Description = dto.Description,
                Category = dto.Category,
                Status = TicketStatus.Open,
                Priority = TicketPriority.Medium,
                CreatedByUserId = "user-123"
            };

            _ticketServiceMock
                .Setup(x => x.UpdateTicketAsync(1, dto))
                .ReturnsAsync(ticket);

            //Act
            var result = await _ticketController.UpdateTicket(1, dto);

            //Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedTicket = Assert.IsType<Ticket>(okResult.Value);

            Assert.Equal(ticket.Id, returnedTicket.Id);
            Assert.Equal(ticket.Title, returnedTicket.Title);
            Assert.Equal(ticket.Description, returnedTicket.Description);
            Assert.Equal(ticket.Category, returnedTicket.Category);
        }

        [Fact]
        public async Task UpdateTicket_WhenTicketDoesNotExist_ReturnNotFound()
        {
            //Arrange 
            var dto = new UpdateTicketDto
            {
                Title = "Updated title",
                Description = "Updated description",
                Category = TicketCategory.General

            };

            _ticketServiceMock
                .Setup(x => x.UpdateTicketAsync(999, dto))
                .ReturnsAsync((Ticket?)null);

            //Act 
            var result = await _ticketController.UpdateTicket(999, dto);

            //Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task DeleteTicket_WhenTicketExists_ReturnOk()
        {
            //Arrange
            _ticketServiceMock
                .Setup(x => x.DeleteTicketAsync(1))
                .ReturnsAsync(true);

            //Act
            var result = await _ticketController.DeleteTicket(1);

            //Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteTicket_WhenTicketDoesNotExist_ReturnNotFound()
        {
            //Arrange
            _ticketServiceMock
                .Setup(x => x.DeleteTicketAsync(999))
                .ReturnsAsync(false);

            //Act
            var result = await _ticketController.DeleteTicket(999);

            //Assert
            Assert.IsType<NotFoundResult>(result);
        }


        [Fact]
        public async Task AdoptTicket_WhenAuthenticated_ReturnOk()
        {
            //Arrange
            var ticket = new Ticket
            {
                Id = 1,
                Title = "Laptop issue",
                Description = "Laptop will not start",
                CreatedByUserId = "requester-1",
                AssignedUserId = "agent-1",
                Status = TicketStatus.InProgress
            };

            _ticketServiceMock
                .Setup(x => x.AdoptTicketAsync(1, "agent-1"))
                .ReturnsAsync(ticket);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "agent-1")
            };

            _ticketController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims))
                }
            };

            //Act
            var result = await _ticketController.AdoptTicket(1);

            //Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedTicket = Assert.IsType<Ticket>(okResult.Value);

            Assert.Equal(1, returnedTicket.Id);
            Assert.Equal("agent-1", returnedTicket.AssignedUserId);
            Assert.Equal(TicketStatus.InProgress, returnedTicket.Status);
        }

        [Fact]
        public async Task AdoptTicket_WhenNotAuthenticated_ReturnUnauthorized()
        {
            //Arrange
            _ticketController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };
            //Act
            var result = await _ticketController.AdoptTicket(1);
            //Assert
            Assert.IsType<UnauthorizedResult>(result.Result);

            _ticketServiceMock.Verify(
                x => x.AdoptTicketAsync(
                    It.IsAny<int>(),
                    It.IsAny<string>()),
                Times.Never);
        }


        [Fact]
        public async Task AdoptTicket_WhenTicketDoesNotExist_ReturnNotFound()
        {
            //Arrange
            _ticketServiceMock
                .Setup(x => x.AdoptTicketAsync(999, "agent-1"))
                .ReturnsAsync((Ticket?)null);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "agent-1")
            };
            _ticketController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims))
                }
            };
            //Act
            var result = await _ticketController.AdoptTicket(999);
            //Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }
    }

}
