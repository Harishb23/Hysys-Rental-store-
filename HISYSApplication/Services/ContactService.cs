using HISYSApplication.DTO;
using HISYSApplication.Repositories.Interface;
using HISYSApplication.Services.Interface;

namespace HISYSApplication.Services
{
    public class ContactService : IContactService
    {
        private readonly IContactRepository _contactRepository;

        public ContactService(IContactRepository contactRepository)
        {
            _contactRepository = contactRepository;
        }

        public async Task<int> SubmitContactFormAsync(ContactSubmissionRequestDto submission)
        {
            return await _contactRepository.AddContactSubmissionAsync(submission);
        }

        public async Task<List<ContactSubmissionResponseDto>> GetAllSubmissionsAsync(bool? unreadOnly = null)
        {
            return await _contactRepository.GetAllSubmissionsAsync(unreadOnly);
        }

        public async Task<ContactSubmissionResponseDto?> GetSubmissionByIdAsync(int id)
        {
            return await _contactRepository.GetSubmissionByIdAsync(id);
        }

        public async Task<bool> MarkAsReadAsync(int id, bool isRead)
        {
            return await _contactRepository.MarkAsReadAsync(id, isRead);
        }

        public async Task<bool> DeleteSubmissionAsync(int id)
        {
            return await _contactRepository.DeleteSubmissionAsync(id);
        }

        public async Task<int> GetUnreadCountAsync()
        {
            return await _contactRepository.GetUnreadCountAsync();
        }
    }
}
