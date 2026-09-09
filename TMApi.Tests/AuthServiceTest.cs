using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TMApi.Models;
using TMApi.Services;

namespace TMApi.Tests
{
    public class AuthServiceTest
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<AuthService>> _loggerMock;

        private readonly AuthService _authService;

        public AuthServiceTest()
        {
            var userStoreMock =
                new Mock<IUserStore<ApplicationUser>>();

            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!);

            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<AuthService>>();

            _authService = new AuthService(
                _userManagerMock.Object,
                _configurationMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task RegisterAsync_ValidUser_ReturnsSuccess()
        {
            // Arrange
            var dto = new RegisterDto
            {
                Username = "john@example.com",
                Email = "john@example.com",
                Password = "Password123"
            };

            _userManagerMock
                .Setup(x => x.CreateAsync(
                    It.IsAny<ApplicationUser>(),
                    dto.Password))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock
                .Setup(x => x.AddToRoleAsync(
                    It.IsAny<ApplicationUser>(),
                    "user"))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.RegisterAsync(dto);

            // Assert
            Assert.True(result.Succeeded);

            _userManagerMock.Verify(
                x => x.CreateAsync(
                    It.Is<ApplicationUser>(u =>
                        u.UserName == dto.Username &&
                        u.Email == dto.Email),
                    dto.Password),
                Times.Once);

            _userManagerMock.Verify(
                x => x.AddToRoleAsync(
                    It.IsAny<ApplicationUser>(),
                    "user"),
                Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_InvalidUser_ReturnsFailure()
        {
            // Arrange
            var dto = new RegisterDto
            {
                Username = "john@example.com",
                Email = "john@example.com",
                Password = "bad"
            };

            var identityError = new IdentityError
            {
                Description = "Password is too weak."
            };

            _userManagerMock
                .Setup(x => x.CreateAsync(
                    It.IsAny<ApplicationUser>(),
                    dto.Password))
                .ReturnsAsync(
                    IdentityResult.Failed(identityError));

            // Act
            var result = await _authService.RegisterAsync(dto);

            // Assert
            Assert.False(result.Succeeded);

            _userManagerMock.Verify(
                x => x.AddToRoleAsync(
                    It.IsAny<ApplicationUser>(),
                    "user"),
                Times.Never);
        }

        [Fact]
        public async Task ChangeUserRoleAsync_UserNotFound_ReturnsFailure()
        {
            // Arrange
            var userId = "does-not-exist";

            _userManagerMock
                .Setup(x => x.FindByIdAsync(userId))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _authService.ChangeUserRoleAsync(
                userId,
                "Agent");

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains(
                result.Errors,
                e => e.Description == "User not found.");
        }

        [Fact]
        public async Task ChangeUserRoleAsync_UserExists_ChangesRole()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "123",
                UserName = "john@example.com",
                Email = "john@example.com"
            };

            _userManagerMock
                .Setup(x => x.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "user" });

            _userManagerMock
                .Setup(x => x.IsInRoleAsync(user, "Agent"))
                .ReturnsAsync(false);

            _userManagerMock
                .Setup(x => x.RemoveFromRolesAsync(
                    user,
                    It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock
                .Setup(x => x.AddToRoleAsync(user, "Agent"))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.ChangeUserRoleAsync(
                user.Id,
                "Agent");

            // Assert
            Assert.True(result.Succeeded);

            _userManagerMock.Verify(
                x => x.RemoveFromRolesAsync(
                    user,
                    It.Is<IEnumerable<string>>(roles =>
                        roles.Contains("user"))),
                Times.Once);

            _userManagerMock.Verify(
                x => x.AddToRoleAsync(
                    user,
                    "Agent"),
                Times.Once);
        }


        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsJwtToken()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "123",
                UserName = "john@example.com",
                Email = "john@example.com"
            };

            var dto = new LoginDto
            {
                Username = "john@example.com",
                Password = "Password123"
            };

            _userManagerMock
                .Setup(x => x.FindByNameAsync(dto.Username))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.CheckPasswordAsync(user, dto.Password))
                .ReturnsAsync(true);

            _userManagerMock
                .Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "user" });

            _userManagerMock
                .Setup(x => x.ResetAccessFailedCountAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            SetupJwtConfiguration();

            // Act
            var result = await _authService.LoginAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task LoginAsync_InvalidUsername_ReturnsInvalidCredentials()
        {
            // Arrange
            var dto = new LoginDto
            {
                Username = "unknown@example.com",
                Password = "Password123"
            };

            _userManagerMock
                .Setup(x => x.FindByNameAsync(dto.Username))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _authService.LoginAsync(dto);

            // Assert
            Assert.Equal(
                "Invalid username or password.",
                result);
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsInvalidCredentials()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "123",
                UserName = "john@example.com",
                Email = "john@example.com"
            };

            var dto = new LoginDto
            {
                Username = "john@example.com",
                Password = "WrongPassword"
            };

            _userManagerMock
                .Setup(x => x.FindByNameAsync(dto.Username))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.CheckPasswordAsync(user, dto.Password))
                .ReturnsAsync(false);

            // Act
            var result = await _authService.LoginAsync(dto);

            // Assert
            Assert.Equal(
                "Invalid username or password.",
                result);
        }

        [Fact]
        public async Task LoginAsync_UserWithRole_JwtContainsRoleClaim()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "123",
                UserName = "john@example.com",
                Email = "john@example.com"
            };

            var dto = new LoginDto
            {
                Username = "john@example.com",
                Password = "Password123"
            };

            _userManagerMock
                .Setup(x => x.FindByNameAsync(dto.Username))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.CheckPasswordAsync(user, dto.Password))
                .ReturnsAsync(true);

            _userManagerMock
                .Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Agent" });

            _userManagerMock
                .Setup(x => x.ResetAccessFailedCountAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            SetupJwtConfiguration();

            // Act
            var tokenString = await _authService.LoginAsync(dto);

            // Assert
            Assert.NotNull(tokenString);

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenString);

            var roleClaim = token.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Role);

            Assert.NotNull(roleClaim);
            Assert.Equal("Agent", roleClaim.Value);
        }


        [Fact]
        public async Task ChangeUserStatusAsync_UserNotfound_ReturnFailure()
        {
            //Arrange 
            var userId = "does-not-exist";

            _userManagerMock
                .Setup(x => x.FindByIdAsync(userId))
                .ReturnsAsync((ApplicationUser?)null);

            //Act
            var result = await _authService.ChangeUserStatusAsync(userId, false);

            //Assert
            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Description == "User not found.");

        }

        [Fact]
        public async Task ChangeUserStatusAsync_UserExists_ActivateUser()
        {
            var user = new ApplicationUser
            {
                Id = "123",
                UserName = "john@example.com",
                Email ="john@example.com",
                IsActive = false
            };


            _userManagerMock
                .Setup(x => x.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            //Act
            var result = await _authService.ChangeUserStatusAsync(user.Id, true);

            //Assert
            Assert.True(result.Succeeded);
            Assert.True(user.IsActive);

            _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);


        }

        [Fact]
        public async Task ChangeUserStatusAsync_UserExists_DeactivatesUser()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "123",
                UserName = "john@example.com",
                Email = "john@example.com",
                IsActive = true
            };

            _userManagerMock
                .Setup(x => x.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.ChangeUserStatusAsync(
                user.Id,
                false);

            // Assert
            Assert.True(result.Succeeded);
            Assert.False(user.IsActive);

            _userManagerMock.Verify(
                x => x.UpdateAsync(user),
                Times.Once);
        }

        [Fact]
        public async Task ChangeUserStatusAsync_UpdateFails_ReturnsFailure()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "123",
                UserName = "john@example.com",
                Email = "john@example.com",
                IsActive = true
            };

            var identityError = new IdentityError
            {
                Description = "Failed to update user."
            };

            _userManagerMock
                .Setup(x => x.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(
                    IdentityResult.Failed(identityError));

            // Act
            var result = await _authService.ChangeUserStatusAsync(
                user.Id,
                false);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains(
                result.Errors,
                e => e.Description == "Failed to update user.");

            Assert.False(user.IsActive);
        }




        #region Private Methods

        private void SetupJwtConfiguration()
        {
            _configurationMock
                .Setup(x => x["JWT:Key"])
                .Returns(
                    "ThisIsAReallyLongTestKeyForJwtAuthentication123456789");

            _configurationMock
                .Setup(x => x["JWT:Issuer"])
                .Returns("TMApi");

            _configurationMock
                .Setup(x => x["JWT:Audience"])
                .Returns("TMApiUsers");

            _configurationMock
                .Setup(x => x["JWT:ExpiryMinutes"])
                .Returns("60");
        }
        #endregion

    }
}

