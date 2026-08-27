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

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);
            
            if (!result.Succeeded)
                return BadRequest(result.Errors);
            
            return Ok("user registered successfully");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var token = await _authService.LoginAsync(dto);

            if (token == null)
                return Unauthorized("Invalid username or password");
            
            return Ok(new { Token = token });
        }   

    }
}
