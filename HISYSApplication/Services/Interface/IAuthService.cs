using HISYSApplication.DTO;

namespace HISYSApplication.Services.Interface
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> AuthenticateAsync(LoginRequestDto request);
        Task<AdminUserDto?> GetCurrentAdminAsync(int adminId);
        Task<(bool Success, string Message)> ChangePasswordAsync(int adminId, ChangePasswordRequestDto request);
    }
}
