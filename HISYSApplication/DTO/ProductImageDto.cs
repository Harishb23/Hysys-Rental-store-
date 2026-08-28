namespace HISYSApplication.DTO
{
    public class ProductImageDto
    {
        public byte[] ImageData { get; set; } = Array.Empty<byte>();

        public string ContentType { get; set; } = string.Empty;
    }
}
