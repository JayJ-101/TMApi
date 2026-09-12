using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query.Internal;
using TMApi.Models;
using TMApi.Services;

namespace TMApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            _logger.LogInformation("Register request received for user: {Username}", dto.Username);

            //password and confirm password validation
            if (dto.Password != dto.ConfirmPassword)
            {
                _logger.LogWarning("Password and Confirm Password do not match for user: {Username}", dto.Username);

                return BadRequest(new { Error = "Password and Confirm Password do not match" });
            }

            var result = await _authService.RegisterAsync(dto);

            if (!result.Succeeded)
            {
                _logger.LogWarning("User registration failed for user: {Username}. Errors: {Errors}",
                    dto.Username, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
            }

            _logger.LogInformation("User registered successfully: {Username}", dto.Username);
            return Ok(new { Message = "User registered successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            _logger.LogInformation("Login request received for user: {Username}", dto.Username);

            var token = await _authService.LoginAsync(dto);

            if (token == null)
            {
                _logger.LogWarning("Login failed for user: {Username}. Invalid token", dto.Username);
                return Unauthorized("Invalid username or password");
            }

            _logger.LogInformation("Login successful for user: {Username}", dto.Username);
            return Ok(new { Token = token });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("user/{userId}/role")]
        public async Task<IActionResult> ChangeUserRole(string userId, [FromBody] string role)
        {
            _logger.LogInformation("Change role request received for userId: {UserId} to role: {Role}", userId, role);
            var result = await _authService.ChangeUserRoleAsync(userId, role);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Change role failed for userId: {UserId}. Errors: {Errors}",
                    userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
            }
            _logger.LogInformation("Role changed successfully for userId: {UserId} to role: {Role}", userId, role);
            return Ok(new { Message = "User role changed successfully" });
        }


        [Authorize(Roles = "Admin")]
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            _logger.LogInformation("Get users request received.");
            var users = await _authService.GetUsersAsync();
            _logger.LogInformation("Retrieved {Count} users.", users.Count());
            return Ok(users);
        }


        [Authorize(Roles = "Admin")]
        [HttpPost("user/{userId}/status")]
        public async Task<IActionResult> ChangeUserStatus(string userId, [FromBody] bool isActive)
        {
            _logger.LogInformation("Change status request received for userId: {UserId} to isActive: {IsActive}", userId, isActive);
            var result = await _authService.ChangeUserStatusAsync(userId, isActive);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Change status failed for userId: {UserId}. Errors: {Errors}",
                    userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
            }
            _logger.LogInformation("Status changed successfully for userId: {UserId} to isActive: {IsActive}", userId, isActive);
            return Ok(new { Message = "User status changed successfully" });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            _logger.LogInformation("Forgot password request received for email: {Email}", dto.Email);
            var result = await _authService.ForgotPasswordAsync(dto.Email);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Forgot password failed for email: {Email}. Errors: {Errors}",
                    dto.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
            }
            _logger.LogInformation("Forgot password email sent successfully to: {Email}", dto.Email);
            return Ok(new { Message = "Password reset email sent successfully" });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            _logger.LogInformation("Reset password request received for email: {Email}", dto.Email);
            var result = await _authService.ResetPasswordAsync(dto);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Reset password failed for email: {Email}. Errors: {Errors}",
                    dto.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
            }
            _logger.LogInformation("Password reset successfully for email: {Email}", dto.Email);
            return Ok(new { Message = "Password reset successfully" });
        }

    }
}
