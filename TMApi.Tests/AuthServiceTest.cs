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
        private readonly Mock<IEmailService> _emailServiceMock;


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
            _emailServiceMock = new Mock<IEmailService>();

            _authService = new AuthService(
                _userManagerMock.Object,
                _configurationMock.Object,
                _loggerMock.Object,
                _emailServiceMock.Object);
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

        [Fact]
        public async Task LoginAsync_InactiveUser_ReturnsInvalidCredentials()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "123",
                UserName = "john@example.com",
                Email = "john@example.com",
                IsActive = false
            };

            var loginDto = new LoginDto
            {
                Username = "john@example.com",
                Password = "Sinaye@123"
            };

            _userManagerMock
                .Setup(x => x.FindByNameAsync(loginDto.Username))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.Equal(
                "Invalid username or password.",
                result);


            _userManagerMock.Verify(
                x => x.CheckPasswordAsync(
                    It.IsAny<ApplicationUser>(),
                    It.IsAny<string>()),
                Times.Never);
        }



        [Fact]
        public async Task ForgotPasswordAsync_UserExists_GeneratesTokenAndSendsEmail()
        {
            // Arrange
            var email = "john@example.com";

            var user = new ApplicationUser
            {
                Id = "123",
                UserName = email,
                Email = email,
                IsActive = true
            };

            var token = "test-reset-token";

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync(token);

            // Act
            var result = await _authService.ForgotPasswordAsync(email);

            // Assert
            Assert.True(result.Succeeded);

            _userManagerMock.Verify(
                x => x.FindByEmailAsync(email),
                Times.Once);

            _userManagerMock.Verify(
                x => x.GeneratePasswordResetTokenAsync(user),
                Times.Once);

            _emailServiceMock.Verify(
                x => x.SendEmailAsync(
                    email,
                    "Task Manager Password Reset",
                    It.Is<string>(body => body.Contains(token))),
                Times.Once);
        }

        [Fact]
        public async Task ForgotPasswordAsync_UserNotFound_ReturnsFailure()
        {
            // Arrange
            var email = "unknown@example.com";

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync((ApplicationUser?)null);

            //Act
            var result = await _authService.ForgotPasswordAsync(email);

            //Assert
            Assert.False(result.Succeeded);

            Assert.Contains(
                result.Errors,
                e => e.Description == "User not found.");

            _userManagerMock.Verify(
                 x => x.GeneratePasswordResetTokenAsync(
                     It.IsAny<ApplicationUser>()),
                 Times.Never);

            _emailServiceMock.Verify(
                   x => x.SendEmailAsync(
                       It.IsAny<string>(),
                       It.IsAny<string>(),
                       It.IsAny<string>()),
                   Times.Never);
        }

        [Fact]
        public async Task ForgotPasswordAsync_InactiveUser_ReturnsFailure()
        {
            //Arrange
            var email = "inactive@example.com";

            var user = new ApplicationUser
            {
                Id = "123",
                UserName = email,
                IsActive = false
            };

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(user);

            //Act
            var result = await _authService.ForgotPasswordAsync(email);

            //Assert
            Assert.False(result.Succeeded);

            Assert.Contains(
                result.Errors,
                e => e.Description == "User account is inactive.");

            _userManagerMock.Verify(
                 x => x.GeneratePasswordResetTokenAsync(
                     It.IsAny<ApplicationUser>()),
                 Times.Never);

            _emailServiceMock.Verify(
                   x => x.SendEmailAsync(
                       It.IsAny<string>(),
                       It.IsAny<string>(),
                       It.IsAny<string>()),
                   Times.Never);
        }

        [Fact]
        public async Task ResetPasswordAsync_UserNotFound_ReturnsFailure()
        {
            // Arrange
            var dto = new ResetPasswordDto
            {
                Email = "unknown@example.com",
                Token = "invalid-token",
                NewPassword = "NewPassword123"
            };

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _authService.ResetPasswordAsync(dto);

            // Assert
            Assert.False(result.Succeeded);

            Assert.Contains(
                result.Errors,
                e => e.Description == "User not found.");

            _userManagerMock.Verify(
                x => x.ResetPasswordAsync(
                    It.IsAny<ApplicationUser>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task ResetPasswordAsync_ValidToken_ReturnsSuccess()
        {
            // Arrange
            var dto = new ResetPasswordDto
            {
                Email = "john@example.com",
                Token = "valid-reset-token",
                NewPassword = "NewPassword123"
            };

            var user = new ApplicationUser
            {
                Id = "123",
                UserName = dto.Email,
                Email = dto.Email,
                IsActive = true
            };

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.ResetPasswordAsync(
                    user,
                    dto.Token,
                    dto.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.ResetPasswordAsync(dto);

            // Assert
            Assert.True(result.Succeeded);

            _userManagerMock.Verify(
                x => x.ResetPasswordAsync(
                    user,
                    dto.Token,
                    dto.NewPassword),
                Times.Once);
        }


        [Fact]
        public async Task ResetPasswordAsync_InvalidToken_ReturnsFailure()
        {
            // Arrange
            var dto = new ResetPasswordDto
            {
                Email = "john@example.com",
                Token = "invalid-reset-token",
                NewPassword = "NewPassword123"
            };

            var user = new ApplicationUser
            {
                Id = "123",
                UserName = dto.Email,
                Email = dto.Email,
                IsActive = true
            };

            var identityError = new IdentityError
            {
                Description = "Invalid token."
            };

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.ResetPasswordAsync(
                    user,
                    dto.Token,
                    dto.NewPassword))
                .ReturnsAsync(IdentityResult.Failed(identityError));

            // Act
            var result = await _authService.ResetPasswordAsync(dto);

            // Assert
            Assert.False(result.Succeeded);

            Assert.Contains(
                result.Errors,
                e => e.Description == "Invalid token.");

            _userManagerMock.Verify(
                x => x.ResetPasswordAsync(
                    user,
                    dto.Token,
                    dto.NewPassword),
                Times.Once);
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

