using HISYSApplication.DTO;
using HISYSApplication.Repositories.Interface;
using Microsoft.Data.SqlClient;
using System.Data;

public class ProductRepository : IProductRepository
{
    private readonly IConfiguration _configuration;

    public ProductRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<int> AddProductAsync(
        ProductRequestDto product,
        byte[] imageBytes,
        string contentType)
    {
        var connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        using var connection =
            new SqlConnection(connectionString);

        const string query = @"
            INSERT INTO Products
            (
                Name,
                Description,
                ImageData,
                ImageContentType,
                Price
            )
            VALUES
            (
                @Name,
                @Description,
                @ImageData,
                @ImageContentType,
                @Price
            );

            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        using var command = new SqlCommand(query, connection);

        command.Parameters.Add(
            "@Name",
            SqlDbType.NVarChar,
            200).Value = product.Name;

        command.Parameters.Add(
            "@Description",
            SqlDbType.NVarChar).Value =
            (object?)product.Description ?? DBNull.Value;

        command.Parameters.Add(
            "@ImageData",
            SqlDbType.VarBinary,
            -1).Value = imageBytes;

        command.Parameters.Add(
            "@ImageContentType",
            SqlDbType.NVarChar,
            100).Value = contentType;

        var priceParameter =
            command.Parameters.Add(
                "@Price",
                SqlDbType.Decimal);

        priceParameter.Precision = 18;
        priceParameter.Scale = 2;
        priceParameter.Value = product.Price;

        await connection.OpenAsync();

        return (int)await command.ExecuteScalarAsync();
    }

    public async Task<List<ProductResponseDto>> GetAllProductsAsync()
    {
        var products = new List<ProductResponseDto>();

        var connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        using var connection =
            new SqlConnection(connectionString);

        const string query = @"
        SELECT
            Id,
            Name,
            Description,
            Price
        FROM Products
        ORDER BY Id DESC";

        using var command =
            new SqlCommand(query, connection);

        await connection.OpenAsync();

        using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var id = reader.GetInt32(
                reader.GetOrdinal("Id"));

            products.Add(new ProductResponseDto
            {
                Id = id,

                Name = reader.GetString(
                    reader.GetOrdinal("Name")),

                Description = reader.IsDBNull(
                    reader.GetOrdinal("Description"))
                    ? string.Empty
                    : reader.GetString(
                        reader.GetOrdinal("Description")),

                Price = reader.GetDecimal(
                    reader.GetOrdinal("Price")),

                ImageUrl = $"/api/products/{id}/image"
            });
        }

        return products;
    }

    public async Task<ProductResponseDto?> GetProductAsync(int id)
    {
        var connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        using var connection =
            new SqlConnection(connectionString);

        const string query = @"
        SELECT
            Id,
            Name,
            Description,
            Price
        FROM Products
        WHERE Id = @Id";

        using var command = new SqlCommand(query, connection);

        command.Parameters.Add(
            "@Id",
            SqlDbType.Int).Value = id;

        await connection.OpenAsync();

        using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new ProductResponseDto
        {
            Id = reader.GetInt32(
                reader.GetOrdinal("Id")),

            Name = reader.GetString(
                reader.GetOrdinal("Name")),

            Description = reader.IsDBNull(
                reader.GetOrdinal("Description"))
                ? string.Empty
                : reader.GetString(
                    reader.GetOrdinal("Description")),

            Price = reader.GetDecimal(
                reader.GetOrdinal("Price")),

            ImageUrl = $"/api/products/{id}/image"
        };
    }

    public async Task<ProductImageDto?> GetProductImageAsync(int id)
    {
        var connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        using var connection =
            new SqlConnection(connectionString);

        const string query = @"
        SELECT
            ImageData,
            ImageContentType
        FROM Products
        WHERE Id = @Id";

        using var command = new SqlCommand(query, connection);

        command.Parameters.Add(
            "@Id",
            SqlDbType.Int).Value = id;

        await connection.OpenAsync();

        using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        if (reader.IsDBNull(
            reader.GetOrdinal("ImageData")))
        {
            return null;
        }

        return new ProductImageDto
        {
            ImageData = (byte[])reader["ImageData"],

            ContentType =
                reader["ImageContentType"]?.ToString()
                ?? "application/octet-stream"
        };
    }
}