using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

    }
}
