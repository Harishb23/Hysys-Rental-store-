using HISYSApplication.DTO;

namespace HISYSApplication.Repositories.Interface
{
    public interface IAuthRepository
    {
        Task<(int Id, string Username, string PasswordHash, string FullName, string Email, string Role)?> GetAdminByUsernameAsync(string username);
        Task<AdminUserDto?> GetAdminByIdAsync(int id);
        Task<string?> GetPasswordHashByIdAsync(int adminId);
        Task<bool> UpdatePasswordAsync(int adminId, string newPasswordHash);
    }
}
