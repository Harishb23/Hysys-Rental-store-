using HISYSApplication.DTO;
using HISYSApplication.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HISYSApplication.Controllers
{
    [ApiController]
    [Route("api/contact")]
    public class ContactController : ControllerBase
    {
        private readonly IContactService _contactService;
        private readonly IEmailService _emailService;
        private readonly ILogger<ContactController> _logger;

        public ContactController(
            IContactService contactService,
            IEmailService emailService,
            ILogger<ContactController> logger)
        {
            _contactService = contactService;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Public endpoint to submit a Contact Us inquiry.
        /// Saves inquiry to database and dispatches email notification to admin via Resend API.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SubmitContactForm([FromBody] ContactSubmissionRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid submission details. Please check the required fields."
                });
            }

            _logger.LogInformation("Processing contact form submission from {Name} ({Email})...", request.Name, request.Email);

            try
            {
                // 1. Save submission to Database
                var submissionId = await _contactService.SubmitContactFormAsync(request);
                _logger.LogInformation("Contact submission successfully saved to database with ID #{SubmissionId}.", submissionId);

                // 2. Send Email Notification to ADMIN_EMAIL via Resend HTTPS API
                var emailSent = await _emailService.SendContactNotificationAsync(
                    request.Name,
                    request.Email,
                    request.Phone,
                    request.Subject,
                    request.Message
                );

                if (emailSent)
                {
                    _logger.LogInformation("Email notification delivered successfully via Resend for submission #{SubmissionId}.", submissionId);
                }
                else
                {
                    _logger.LogWarning("Email notification could not be delivered for submission #{SubmissionId} (check Resend configuration).", submissionId);
                }

                // 3. Return standardized success response
                return Ok(new
                {
                    success = true,
                    message = "Your message has been submitted successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing contact form submission from {Email}.", request.Email);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to submit your message. Please try again later."
                });
            }
        }

        /// <summary>
        /// Admin endpoint to retrieve all Contact Us submissions.
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllSubmissions([FromQuery] bool? unreadOnly = null)
        {
            var submissions = await _contactService.GetAllSubmissionsAsync(unreadOnly);
            return Ok(submissions);
        }

        /// <summary>
        /// Admin endpoint to get unread message count.
        /// </summary>
        [HttpGet("unread-count")]
        [Authorize]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _contactService.GetUnreadCountAsync();
            return Ok(new { unreadCount = count });
        }

        /// <summary>
        /// Admin endpoint to get a specific submission details.
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetSubmission(int id)
        {
            var submission = await _contactService.GetSubmissionByIdAsync(id);
            if (submission == null)
            {
                return NotFound(new { message = "Submission not found." });
            }

            return Ok(submission);
        }

        /// <summary>
        /// Admin endpoint to mark a submission as read or unread.
        /// </summary>
        [HttpPatch("{id}/read")]
        [Authorize]
        public async Task<IActionResult> MarkAsRead(int id, [FromQuery] bool isRead = true)
        {
            var updated = await _contactService.MarkAsReadAsync(id, isRead);
            if (!updated)
            {
                return NotFound(new { message = "Submission not found." });
            }

            return Ok(new { message = isRead ? "Marked as read." : "Marked as unread." });
        }

        /// <summary>
        /// Admin endpoint to delete a submission.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteSubmission(int id)
        {
            var deleted = await _contactService.DeleteSubmissionAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = "Submission not found." });
            }

            return Ok(new { message = "Submission deleted successfully." });
        }
    }
}
