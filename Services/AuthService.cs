using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TMApi.Models;

namespace TMApi.Services
{
    public class AuthService :IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(UserManager<ApplicationUser> userManager, 
            IConfiguration configuration, ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }
        public async Task<IdentityResult> RegisterAsync(RegisterDto dto)
        {
            try
            {
                _logger.LogInformation("Attempting to register user: {Username}.", dto.Username);

                var user = new ApplicationUser
                {
                    UserName = dto.Username,
                    Email = dto.Email,
                };
                
                var result = await _userManager.CreateAsync(user, dto.Password);

                if (result.Succeeded)
                    _logger.LogInformation("Registration succeeded for user: {Username}.", dto.Username);
                else
                {
                    _logger.LogWarning("Registration failed for user: {Username}. Errors: {Errors}",
                        dto.Username, 
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
                return result;
            }
            catch (Exception ex)
            {
              _logger.LogError(ex, "An error occurred during registration for user: {Username}.", dto.Username);
                return IdentityResult.Failed(
                    new IdentityError { Description = "An unexpected error occurred during registration." });
            }
        }

        public async Task<string?> LoginAsync(LoginDto dto)
        {
            try
            {
                _logger.LogInformation("Attempting to log in user: {Username}.", dto.Username);

                var user = await _userManager.FindByNameAsync(dto.Username);
                if (user == null)
                {
                    _logger.LogWarning("Login failed: User not found : {Username}", dto.Username);
                    return "Invalid username or password.";
                }

                var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
                if (!isPasswordValid)
                {
                    _logger.LogWarning("Login failed, invalid password for user: {Username}", dto.Username);
                    return "Invalid username or password.";
                }

                await _userManager.ResetAccessFailedCountAsync(user);

                // Generate a token 
                var claims = new List<Claim>()
                {
                    new Claim(ClaimTypes.NameIdentifier,user.Id),

                    new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),

                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
                };

                var key = _configuration["JWT:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");

                var issuer = _configuration["JWT:Issuer"];
                var audience = _configuration["JWT:Audience"];

                var expires = int.Parse(_configuration["JWT:ExpiryMinutes"] ?? "60");

                var signinKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

                var credentials = new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(expires),
                    signingCredentials: credentials
                );

                _logger.LogInformation("User {Username} logged in successfully.",
                    dto.Username);
                return new JwtSecurityTokenHandler().WriteToken(token);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during login for user: {Username}.", dto.Username);
                return "Invalid username or password";
            }

        }
    }   
}
