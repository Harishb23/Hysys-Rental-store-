namespace HISYSApplication.Services.Interface
{
    public interface IEmailService
    {
        Task<bool> SendContactNotificationAsync(string name, string email, string? phone, string? subject, string message);
    }
}
