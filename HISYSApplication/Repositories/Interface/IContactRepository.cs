using HISYSApplication.DTO;

namespace HISYSApplication.Repositories.Interface
{
    public interface IContactRepository
    {
        Task<int> AddContactSubmissionAsync(ContactSubmissionRequestDto submission);
        Task<List<ContactSubmissionResponseDto>> GetAllSubmissionsAsync(bool? unreadOnly = null);
        Task<ContactSubmissionResponseDto?> GetSubmissionByIdAsync(int id);
        Task<bool> MarkAsReadAsync(int id, bool isRead);
        Task<bool> DeleteSubmissionAsync(int id);
        Task<int> GetUnreadCountAsync();
    }
}
