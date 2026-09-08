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
                {
                    _logger.LogInformation("Registration succeeded for user: {Username}.", dto.Username);
                    await _userManager.AddToRoleAsync(user, "user");
                    return result;
                }
                else
                {
                    _logger.LogWarning("Registration failed for user: {Username}. Errors: {Errors}",
                        dto.Username,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                    return result;
                }
                
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

                var roles = await _userManager.GetRolesAsync(user);
                await _userManager.ResetAccessFailedCountAsync(user);

                // Generate a token 
                var claims = new List<Claim>()
                {
                    new Claim(ClaimTypes.NameIdentifier,user.Id),

                    new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),

                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
                };
                
                //Add roles 
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

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


        public async Task<IdentityResult> ChangeUserRoleAsync(string userId, string role)
        {
            try
            {
                _logger.LogInformation("Attempting to change role for user ID: {UserId} to role: {Role}.", userId, role);
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Change role failed: User not found with ID: {UserId}", userId);
                    return IdentityResult.Failed(new IdentityError { Description = "User not found." });
                }
                
               if (!await _userManager.IsInRoleAsync(user, role))
                {
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    if (currentRoles.Any())
                    {
                        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        if (!removeResult.Succeeded)
                        {
                            _logger.LogWarning("Failed to remove user ID: {UserId} from current roles: {Roles}. Errors: {Errors}",
                                userId,
                                string.Join(", ", currentRoles),
                                string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                            return removeResult;
                        }
                    }
                }

               _logger.LogInformation("Role changed succeffully for user ID: {UserId}, New role: {Role}.", userId, role);
                return await _userManager.AddToRoleAsync(user, role);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while changing role for user ID: {UserId}.", userId);
                return IdentityResult.Failed(new IdentityError { Description = "An unexpected error occurred while changing the user's role." });
            }
        }


        public async Task<IEnumerable<UserListDto>> GetUsersAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving users for administration.");

                var users = _userManager.Users.ToList();

                var userList = new List<UserListDto>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);

                    userList.Add(new UserListDto
                    {
                        Id = user.Id,
                        Username = user.UserName,
                        Email = user.Email,
                        FullName = user.FullName,
                        IsActive = user.IsActive,
                        Roles = roles
                    });
                }

                return userList;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while retrieving users.");

                throw;
            }
        }

    }   
}
