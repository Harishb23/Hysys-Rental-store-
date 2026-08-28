using HISYSApplication.DTO;
using HISYSApplication.Repositories.Interface;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HISYSApplication.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IConfiguration _configuration;

        public AuthRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string ConnectionString =>
            _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        public async Task<(int Id, string Username, string PasswordHash, string FullName, string Email, string Role)?> GetAdminByUsernameAsync(string username)
        {
            using var connection = new SqlConnection(ConnectionString);
            const string query = @"
                SELECT Id, Username, PasswordHash, FullName, Email, Role
                FROM Admins
                WHERE LOWER(Username) = LOWER(@Username)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add("@Username", SqlDbType.NVarChar, 100).Value = username.Trim();

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return (
                reader.GetInt32(reader.GetOrdinal("Id")),
                reader.GetString(reader.GetOrdinal("Username")),
                reader.GetString(reader.GetOrdinal("PasswordHash")),
                reader.GetString(reader.GetOrdinal("FullName")),
                reader.GetString(reader.GetOrdinal("Email")),
                reader.GetString(reader.GetOrdinal("Role"))
            );
        }

        public async Task<AdminUserDto?> GetAdminByIdAsync(int id)
        {
            using var connection = new SqlConnection(ConnectionString);
            const string query = @"
                SELECT Id, Username, FullName, Email, Role, CreatedAt
                FROM Admins
                WHERE Id = @Id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new AdminUserDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Username = reader.GetString(reader.GetOrdinal("Username")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                Role = reader.GetString(reader.GetOrdinal("Role")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            };
        }

        public async Task<string?> GetPasswordHashByIdAsync(int adminId)
        {
            using var connection = new SqlConnection(ConnectionString);
            const string query = @"
                SELECT PasswordHash
                FROM Admins
                WHERE Id = @Id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = adminId;

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result?.ToString();
        }

        public async Task<bool> UpdatePasswordAsync(int adminId, string newPasswordHash)
        {
            using var connection = new SqlConnection(ConnectionString);
            const string query = @"
                UPDATE Admins
                SET PasswordHash = @PasswordHash
                WHERE Id = @Id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = adminId;
            command.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 500).Value = newPasswordHash;

            await connection.OpenAsync();
            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}
