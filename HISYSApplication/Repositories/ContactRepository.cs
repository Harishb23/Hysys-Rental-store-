using HISYSApplication.DTO;
using HISYSApplication.Repositories.Interface;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HISYSApplication.Repositories
{
    public class ContactRepository : IContactRepository
    {
        private readonly IConfiguration _configuration;

        public ContactRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string ConnectionString =>
            _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        public async Task<int> AddContactSubmissionAsync(ContactSubmissionRequestDto submission)
        {
            using var connection = new SqlConnection(ConnectionString);
            const string query = @"
                INSERT INTO ContactSubmissions (Name, Email, Phone, Subject, Message, IsRead, CreatedAt)
                VALUES (@Name, @Email, @Phone, @Subject, @Message, 0, GETUTCDATE());
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = submission.Name;
            command.Parameters.Add("@Email", SqlDbType.NVarChar, 150).Value = submission.Email;
            command.Parameters.Add("@Phone", SqlDbType.NVarChar, 50).Value = (object?)submission.Phone ?? DBNull.Value;
            command.Parameters.Add("@Subject", SqlDbType.NVarChar, 200).Value = (object?)submission.Subject ?? DBNull.Value;
            command.Parameters.Add("@Message", SqlDbType.NVarChar, -1).Value = submission.Message;

            await connection.OpenAsync();
            var id = (int)(await command.ExecuteScalarAsync() ?? 0);
            return id;
        }

        public async Task<List<ContactSubmissionResponseDto>> GetAllSubmissionsAsync(bool? unreadOnly = null)
        {
            var results = new List<ContactSubmissionResponseDto>();
            using var connection = new SqlConnection(ConnectionString);

            string query = @"
                SELECT Id, Name, Email, Phone, Subject, Message, IsRead, CreatedAt
                FROM ContactSubmissions ";

            if (unreadOnly.HasValue && unreadOnly.Value)
            {
                query += " WHERE IsRead = 0 ";
            }

            query += " ORDER BY Id DESC";

            using var command = new SqlCommand(query, connection);
            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new ContactSubmissionResponseDto
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
                    Subject = reader.IsDBNull(reader.GetOrdinal("Subject")) ? null : reader.GetString(reader.GetOrdinal("Subject")),
                    Message = reader.GetString(reader.GetOrdinal("Message")),
                    IsRead = reader.GetBoolean(reader.GetOrdinal("IsRead")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                });
            }

            return results;
        }

        public async Task<ContactSubmissionResponseDto?> GetSubmissionByIdAsync(int id)
        {
            using var connection = new SqlConnection(ConnectionString);
            const string query = @"
                SELECT Id, Name, Email, Phone, Subject, Message, IsRead, CreatedAt
                FROM ContactSubmissions
                WHERE Id = @Id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new ContactSubmissionResponseDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
                Subject = reader.IsDBNull(reader.GetOrdinal("Subject")) ? null : reader.GetString(reader.GetOrdinal("Subject")),
                Message = reader.GetString(reader.GetOrdinal("Message")),
                IsRead = reader.GetBoolean(reader.GetOrdinal("IsRead")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            };
        }

        public async Task<bool> MarkAsReadAsync(int id, bool isRead)
        {
            using var connection = new SqlConnection(ConnectionString);
            const string query = @"
                UPDATE ContactSubmissions
                SET IsRead = @IsRead
                WHERE Id = @Id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            command.Parameters.Add("@IsRead", SqlDbType.Bit).Value = isRead;

            await connection.OpenAsync();
            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteSubmissionAsync(int id)
        {
            using var connection = new SqlConnection(ConnectionString);
            const string query = @"
                DELETE FROM ContactSubmissions
                WHERE Id = @Id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await connection.OpenAsync();
            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<int> GetUnreadCountAsync()
        {
            using var connection = new SqlConnection(ConnectionString);
            const string query = "SELECT COUNT(*) FROM ContactSubmissions WHERE IsRead = 0";

            using var command = new SqlCommand(query, connection);
            await connection.OpenAsync();
            return (int)(await command.ExecuteScalarAsync() ?? 0);
        }
    }
}
