using HISYSApplication.Utils;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HISYSApplication.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeDatabaseAsync(IConfiguration configuration, ILogger logger)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                logger.LogWarning("DefaultConnection connection string is not configured.");
                return;
            }

            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // 1. Create Admins table
                const string createAdminsQuery = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Admins')
                    BEGIN
                        CREATE TABLE Admins (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            Username NVARCHAR(100) NOT NULL UNIQUE,
                            PasswordHash NVARCHAR(500) NOT NULL,
                            FullName NVARCHAR(200) NOT NULL,
                            Email NVARCHAR(200) NOT NULL,
                            Role NVARCHAR(50) NOT NULL DEFAULT 'Admin',
                            CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                        );
                    END";

                using (var cmd = new SqlCommand(createAdminsQuery, connection))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // 2. Create / Migrate Products table
                const string createProductsQuery = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
                    BEGIN
                        CREATE TABLE Products (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            Name NVARCHAR(200) NOT NULL,
                            Description NVARCHAR(MAX) NULL,
                            Price DECIMAL(18,2) NOT NULL DEFAULT 0,
                            Category NVARCHAR(100) NOT NULL DEFAULT 'General',
                            StockStatus NVARCHAR(50) NOT NULL DEFAULT 'In Stock',
                            ImageData VARBINARY(MAX) NULL,
                            ImageContentType NVARCHAR(100) NULL,
                            CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            UpdatedAt DATETIME2 NULL
                        );
                    END
                    ELSE
                    BEGIN
                        IF COL_LENGTH('Products', 'Category') IS NULL
                            ALTER TABLE Products ADD Category NVARCHAR(100) NOT NULL DEFAULT 'General';
                        IF COL_LENGTH('Products', 'StockStatus') IS NULL
                            ALTER TABLE Products ADD StockStatus NVARCHAR(50) NOT NULL DEFAULT 'In Stock';
                        IF COL_LENGTH('Products', 'CreatedAt') IS NULL
                            ALTER TABLE Products ADD CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE();
                        IF COL_LENGTH('Products', 'UpdatedAt') IS NULL
                            ALTER TABLE Products ADD UpdatedAt DATETIME2 NULL;
                    END";

                using (var cmd = new SqlCommand(createProductsQuery, connection))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // 3. Create ContactSubmissions table
                const string createContactQuery = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ContactSubmissions')
                    BEGIN
                        CREATE TABLE ContactSubmissions (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            Name NVARCHAR(100) NOT NULL,
                            Email NVARCHAR(150) NOT NULL,
                            Phone NVARCHAR(50) NULL,
                            Subject NVARCHAR(200) NULL,
                            Message NVARCHAR(MAX) NOT NULL,
                            IsRead BIT NOT NULL DEFAULT 0,
                            CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                        );
                    END";

                using (var cmd = new SqlCommand(createContactQuery, connection))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // 4. Seed Default Admin if table is empty
                const string checkAdminQuery = "SELECT COUNT(*) FROM Admins WHERE Username = @Username";
                using (var cmd = new SqlCommand(checkAdminQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@Username", "admin");
                    int adminCount = (int)(await cmd.ExecuteScalarAsync() ?? 0);

                    if (adminCount == 0)
                    {
                        string defaultHash = PasswordHasher.HashPassword("Admin@123");
                        const string insertAdminQuery = @"
                            INSERT INTO Admins (Username, PasswordHash, FullName, Email, Role, CreatedAt)
                            VALUES (@Username, @PasswordHash, @FullName, @Email, @Role, GETUTCDATE())";

                        using var insertCmd = new SqlCommand(insertAdminQuery, connection);
                        insertCmd.Parameters.AddWithValue("@Username", "admin");
                        insertCmd.Parameters.AddWithValue("@PasswordHash", defaultHash);
                        insertCmd.Parameters.AddWithValue("@FullName", "Hysys Admin");
                        insertCmd.Parameters.AddWithValue("@Email", "admin@hysys.com");
                        insertCmd.Parameters.AddWithValue("@Role", "Admin");

                        await insertCmd.ExecuteNonQueryAsync();
                        logger.LogInformation("Seeded default admin account: admin / Admin@123");
                    }
                }

                logger.LogInformation("Database tables and seed verification completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during database initialization/migration.");
            }
        }
    }
}
