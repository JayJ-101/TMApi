using Microsoft.AspNetCore.Identity;
using TMApi.Models;

namespace TMApi.Services
{
    public interface IAuthService
    {
        Task<IdentityResult> RegisterAsync(RegisterDto dto);
        Task<string?> LoginAsync(LoginDto dto);
        Task<IdentityResult> ChangeUserRoleAsync(string userId, string role);
        Task<IEnumerable<UserListDto>> GetUsersAsync();
    }
}
