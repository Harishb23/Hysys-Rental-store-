using HISYSApplication.DTO;

namespace HISYSApplication.Services.Interface
{
    public interface IContactService
    {
        Task<int> SubmitContactFormAsync(ContactSubmissionRequestDto submission);
        Task<List<ContactSubmissionResponseDto>> GetAllSubmissionsAsync(bool? unreadOnly = null);
        Task<ContactSubmissionResponseDto?> GetSubmissionByIdAsync(int id);
        Task<bool> MarkAsReadAsync(int id, bool isRead);
        Task<bool> DeleteSubmissionAsync(int id);
        Task<int> GetUnreadCountAsync();
    }
}
