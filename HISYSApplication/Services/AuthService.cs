using HISYSApplication.DTO;
using HISYSApplication.Repositories.Interface;
using HISYSApplication.Services.Interface;
using HISYSApplication.Utils;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HISYSApplication.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IAuthRepository authRepository, IConfiguration configuration)
        {
            _authRepository = authRepository;
            _configuration = configuration;
        }

        public async Task<LoginResponseDto?> AuthenticateAsync(LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return null;
            }

            var admin = await _authRepository.GetAdminByUsernameAsync(request.Username.Trim());
            if (admin == null)
            {
                return null;
            }

            bool isPasswordValid = PasswordHasher.VerifyPassword(request.Password, admin.Value.PasswordHash);
            if (!isPasswordValid)
            {
                return null;
            }

            // Generate JWT Token
            var secretKey = _configuration["Jwt:SecretKey"] ?? "DefaultFallbackSecretKeyForHysys2026!#$";
            var issuer = _configuration["Jwt:Issuer"] ?? "HysysApi";
            var audience = _configuration["Jwt:Audience"] ?? "HysysAdminApp";
            var expirationHours = int.TryParse(_configuration["Jwt:ExpirationHours"], out int h) ? h : 24;

            var expiresAt = DateTime.UtcNow.AddHours(expirationHours);
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, admin.Value.Id.ToString()),
                    new Claim(ClaimTypes.Name, admin.Value.Username),
                    new Claim(ClaimTypes.GivenName, admin.Value.FullName),
                    new Claim(ClaimTypes.Email, admin.Value.Email),
                    new Claim(ClaimTypes.Role, admin.Value.Role)
                }),
                Expires = expiresAt,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return new LoginResponseDto
            {
                Token = tokenString,
                Username = admin.Value.Username,
                FullName = admin.Value.FullName,
                Role = admin.Value.Role,
                ExpiresAt = expiresAt
            };
        }

        public async Task<AdminUserDto?> GetCurrentAdminAsync(int adminId)
        {
            return await _authRepository.GetAdminByIdAsync(adminId);
        }

        public async Task<(bool Success, string Message)> ChangePasswordAsync(int adminId, ChangePasswordRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return (false, "Current password and new password are required.");
            }

            if (request.NewPassword != request.ConfirmNewPassword)
            {
                return (false, "New password and confirmation password do not match.");
            }

            if (request.NewPassword.Length < 6)
            {
                return (false, "New password must be at least 6 characters.");
            }

            var currentHash = await _authRepository.GetPasswordHashByIdAsync(adminId);
            if (string.IsNullOrEmpty(currentHash))
            {
                return (false, "Admin account not found.");
            }

            bool isCurrentValid = PasswordHasher.VerifyPassword(request.CurrentPassword, currentHash);
            if (!isCurrentValid)
            {
                return (false, "The current password you entered is incorrect.");
            }

            var newHash = PasswordHasher.HashPassword(request.NewPassword);
            var updated = await _authRepository.UpdatePasswordAsync(adminId, newHash);

            if (!updated)
            {
                return (false, "Failed to update password in database.");
            }

            return (true, "Password has been changed successfully.");
        }
    }
}
