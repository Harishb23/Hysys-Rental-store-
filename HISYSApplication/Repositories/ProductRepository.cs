using HISYSApplication.DTO;
using HISYSApplication.Repositories.Interface;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HISYSApplication.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly IConfiguration _configuration;

        public ProductRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string ConnectionString =>
            _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        public async Task<int> AddProductAsync(
            ProductRequestDto product,
            byte[] imageBytes,
            string contentType)
        {
            using var connection = new SqlConnection(ConnectionString);

            const string query = @"
                INSERT INTO Products
                (
                    Name,
                    Description,
                    Category,
                    StockStatus,
                    ImageData,
                    ImageContentType,
                    Price,
                    CreatedAt
                )
                VALUES
                (
                    @Name,
                    @Description,
                    @Category,
                    @StockStatus,
                    @ImageData,
                    @ImageContentType,
                    @Price,
                    GETUTCDATE()
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var command = new SqlCommand(query, connection);

            command.Parameters.Add("@Name", SqlDbType.NVarChar, 200).Value = product.Name;
            command.Parameters.Add("@Description", SqlDbType.NVarChar, -1).Value = (object?)product.Description ?? DBNull.Value;
            command.Parameters.Add("@Category", SqlDbType.NVarChar, 100).Value = string.IsNullOrWhiteSpace(product.Category) ? "General" : product.Category;
            command.Parameters.Add("@StockStatus", SqlDbType.NVarChar, 50).Value = string.IsNullOrWhiteSpace(product.StockStatus) ? "In Stock" : product.StockStatus;
            command.Parameters.Add("@ImageData", SqlDbType.VarBinary, -1).Value = imageBytes;
            command.Parameters.Add("@ImageContentType", SqlDbType.NVarChar, 100).Value = contentType;

            var priceParameter = command.Parameters.Add("@Price", SqlDbType.Decimal);
            priceParameter.Precision = 18;
            priceParameter.Scale = 2;
            priceParameter.Value = product.Price;

            await connection.OpenAsync();
            var id = (int)(await command.ExecuteScalarAsync() ?? 0);
            return id;
        }

        public async Task<List<ProductResponseDto>> GetAllProductsAsync(string? category = null, string? search = null)
        {
            var products = new List<ProductResponseDto>();
            using var connection = new SqlConnection(ConnectionString);

            string query = @"
                SELECT
                    Id,
                    Name,
                    Description,
                    Category,
                    StockStatus,
                    Price,
                    CreatedAt
                FROM Products
                WHERE 1 = 1 ";

            if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query += " AND LOWER(Category) = LOWER(@Category) ";
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query += " AND (LOWER(Name) LIKE LOWER(@Search) OR LOWER(Description) LIKE LOWER(@Search)) ";
            }

            query += " ORDER BY Id DESC";

            using var command = new SqlCommand(query, connection);

            if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                command.Parameters.Add("@Category", SqlDbType.NVarChar, 100).Value = category.Trim();
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                command.Parameters.Add("@Search", SqlDbType.NVarChar, 200).Value = $"%{search.Trim()}%";
            }

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32(reader.GetOrdinal("Id"));

                int catOrdinal = -1;
                try { catOrdinal = reader.GetOrdinal("Category"); } catch { }

                int stockOrdinal = -1;
                try { stockOrdinal = reader.GetOrdinal("StockStatus"); } catch { }

                int createdOrdinal = -1;
                try { createdOrdinal = reader.GetOrdinal("CreatedAt"); } catch { }

                string cat = catOrdinal >= 0 && !reader.IsDBNull(catOrdinal) ? reader.GetString(catOrdinal) : "General";
                string stock = stockOrdinal >= 0 && !reader.IsDBNull(stockOrdinal) ? reader.GetString(stockOrdinal) : "In Stock";
                DateTime? created = createdOrdinal >= 0 && !reader.IsDBNull(createdOrdinal) ? reader.GetDateTime(createdOrdinal) : null;

                products.Add(new ProductResponseDto
                {
                    Id = id,
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
                    Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                    Category = cat,
                    StockStatus = stock,
                    ImageUrl = $"/api/products/{id}/image",
                    CreatedAt = created
                });
            }

            return products;
        }

        public async Task<ProductResponseDto?> GetProductAsync(int id)
        {
            using var connection = new SqlConnection(ConnectionString);

            const string query = @"
                SELECT
                    Id,
                    Name,
                    Description,
                    Category,
                    StockStatus,
                    Price,
                    CreatedAt
                FROM Products
                WHERE Id = @Id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            int catOrdinal = -1;
            try { catOrdinal = reader.GetOrdinal("Category"); } catch { }

            int stockOrdinal = -1;
            try { stockOrdinal = reader.GetOrdinal("StockStatus"); } catch { }

            int createdOrdinal = -1;
            try { createdOrdinal = reader.GetOrdinal("CreatedAt"); } catch { }

            string cat = catOrdinal >= 0 && !reader.IsDBNull(catOrdinal) ? reader.GetString(catOrdinal) : "General";
            string stock = stockOrdinal >= 0 && !reader.IsDBNull(stockOrdinal) ? reader.GetString(stockOrdinal) : "In Stock";
            DateTime? created = createdOrdinal >= 0 && !reader.IsDBNull(createdOrdinal) ? reader.GetDateTime(createdOrdinal) : null;

            return new ProductResponseDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                Category = cat,
                StockStatus = stock,
                ImageUrl = $"/api/products/{id}/image",
                CreatedAt = created
            };
        }

        public async Task<ProductImageDto?> GetProductImageAsync(int id)
        {
            using var connection = new SqlConnection(ConnectionString);

            const string query = @"
                SELECT
                    ImageData,
                    ImageContentType
                FROM Products
                WHERE Id = @Id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            if (reader.IsDBNull(reader.GetOrdinal("ImageData")))
            {
                return null;
            }

            return new ProductImageDto
            {
                ImageData = (byte[])reader["ImageData"],
                ContentType = reader["ImageContentType"]?.ToString() ?? "application/octet-stream"
            };
        }

        public async Task<bool> UpdateProductAsync(int id, ProductUpdateDto product, byte[]? newImageBytes, string? newContentType)
        {
            using var connection = new SqlConnection(ConnectionString);

            string query;
            if (newImageBytes != null && newImageBytes.Length > 0 && !string.IsNullOrWhiteSpace(newContentType))
            {
                query = @"
                    UPDATE Products
                    SET
                        Name = @Name,
                        Description = @Description,
                        Price = @Price,
                        Category = @Category,
                        StockStatus = @StockStatus,
                        ImageData = @ImageData,
                        ImageContentType = @ImageContentType,
                        UpdatedAt = GETUTCDATE()
                    WHERE Id = @Id";
            }
            else
            {
                query = @"
                    UPDATE Products
                    SET
                        Name = @Name,
                        Description = @Description,
                        Price = @Price,
                        Category = @Category,
                        StockStatus = @StockStatus,
                        UpdatedAt = GETUTCDATE()
                    WHERE Id = @Id";
            }

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            command.Parameters.Add("@Name", SqlDbType.NVarChar, 200).Value = product.Name;
            command.Parameters.Add("@Description", SqlDbType.NVarChar, -1).Value = (object?)product.Description ?? DBNull.Value;
            command.Parameters.Add("@Category", SqlDbType.NVarChar, 100).Value = string.IsNullOrWhiteSpace(product.Category) ? "General" : product.Category;
            command.Parameters.Add("@StockStatus", SqlDbType.NVarChar, 50).Value = string.IsNullOrWhiteSpace(product.StockStatus) ? "In Stock" : product.StockStatus;

            var priceParam = command.Parameters.Add("@Price", SqlDbType.Decimal);
            priceParam.Precision = 18;
            priceParam.Scale = 2;
            priceParam.Value = product.Price;

            if (newImageBytes != null && newImageBytes.Length > 0 && !string.IsNullOrWhiteSpace(newContentType))
            {
                command.Parameters.Add("@ImageData", SqlDbType.VarBinary, -1).Value = newImageBytes;
                command.Parameters.Add("@ImageContentType", SqlDbType.NVarChar, 100).Value = newContentType;
            }

            await connection.OpenAsync();
            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            using var connection = new SqlConnection(ConnectionString);
            const string query = "DELETE FROM Products WHERE Id = @Id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await connection.OpenAsync();
            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
    }
}