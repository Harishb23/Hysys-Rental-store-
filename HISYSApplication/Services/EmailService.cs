using HISYSApplication.Services.Interface;
using Resend;
using System.Text;

namespace HISYSApplication.Services
{
    public class EmailService : IEmailService
    {
        private readonly IResend _resend;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IResend resend, IConfiguration configuration, ILogger<EmailService> logger)
        {
            _resend = resend;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendContactNotificationAsync(string name, string email, string? phone, string? subject, string message)
        {
            var adminEmail = _configuration["ADMIN_EMAIL"] 
                ?? _configuration["Resend:AdminEmail"];

            var fromEmail = _configuration["RESEND_FROM_EMAIL"] 
                ?? _configuration["Resend:FromEmail"] 
                ?? "onboarding@resend.dev";

            var fromName = _configuration["RESEND_FROM_NAME"] 
                ?? _configuration["Resend:FromName"] 
                ?? "HYSYS Notifications";

            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                _logger.LogWarning("ADMIN_EMAIL is not configured. Skipping email dispatch via Resend.");
                return false;
            }

            try
            {
                var emailSubject = !string.IsNullOrWhiteSpace(subject)
                    ? $"[HYSYS Inquiry] {subject} - from {name}"
                    : $"[HYSYS Inquiry] New Contact Message from {name}";

                var htmlBuilder = new StringBuilder();
                htmlBuilder.Append("<div style=\"font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; background: #0f172a; color: #f8fafc; border-radius: 12px; overflow: hidden; border: 1px solid #1e293b;\">");
                htmlBuilder.Append("<div style=\"background: linear-gradient(135deg, #0066ff, #00f2fe); padding: 24px 30px;\">");
                htmlBuilder.Append("<h2 style=\"margin: 0; color: #ffffff; font-size: 22px;\">New Customer Inquiry</h2>");
                htmlBuilder.Append("<p style=\"margin: 4px 0 0 0; color: rgba(255,255,255,0.85); font-size: 13px;\">HYSYS AV & Production Management</p>");
                htmlBuilder.Append("</div>");
                htmlBuilder.Append("<div style=\"padding: 30px;\">");
                htmlBuilder.Append("<table style=\"width: 100%; border-collapse: collapse; margin-bottom: 24px;\">");
                htmlBuilder.Append($"<tr><td style=\"padding: 8px 0; color: #94a3b8; width: 140px; font-weight: 600;\">Customer Name:</td><td style=\"padding: 8px 0; color: #f8fafc; font-weight: 700;\">{System.Web.HttpUtility.HtmlEncode(name)}</td></tr>");
                htmlBuilder.Append($"<tr><td style=\"padding: 8px 0; color: #94a3b8; font-weight: 600;\">Email Address:</td><td style=\"padding: 8px 0; color: #38bdf8;\"><a href=\"mailto:{System.Web.HttpUtility.HtmlEncode(email)}\" style=\"color: #38bdf8; text-decoration: none;\">{System.Web.HttpUtility.HtmlEncode(email)}</a></td></tr>");
                htmlBuilder.Append($"<tr><td style=\"padding: 8px 0; color: #94a3b8; font-weight: 600;\">Phone Number:</td><td style=\"padding: 8px 0; color: #f8fafc;\">{(string.IsNullOrWhiteSpace(phone) ? "Not Provided" : System.Web.HttpUtility.HtmlEncode(phone))}</td></tr>");
                htmlBuilder.Append($"<tr><td style=\"padding: 8px 0; color: #94a3b8; font-weight: 600;\">Subject:</td><td style=\"padding: 8px 0; color: #f8fafc;\">{(string.IsNullOrWhiteSpace(subject) ? "General Inquiry" : System.Web.HttpUtility.HtmlEncode(subject))}</td></tr>");
                htmlBuilder.Append("</table>");
                htmlBuilder.Append("<div style=\"background: #1e293b; padding: 20px; border-radius: 8px; border-left: 4px solid #00f2fe;\">");
                htmlBuilder.Append("<h4 style=\"margin: 0 0 10px 0; color: #94a3b8; font-size: 12px; text-transform: uppercase; letter-spacing: 0.05em;\">Message Content</h4>");
                htmlBuilder.Append($"<p style=\"margin: 0; color: #ffffff; white-space: pre-wrap; line-height: 1.6;\">{System.Web.HttpUtility.HtmlEncode(message)}</p>");
                htmlBuilder.Append("</div>");
                htmlBuilder.Append("<div style=\"margin-top: 24px; padding-top: 16px; border-top: 1px solid #1e293b; font-size: 12px; color: #64748b; text-align: center;\">");
                htmlBuilder.Append($"Received on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC • Reply directly to this email to contact {System.Web.HttpUtility.HtmlEncode(name)}.");
                htmlBuilder.Append("</div>");
                htmlBuilder.Append("</div>");
                htmlBuilder.Append("</div>");

                var emailMessage = new EmailMessage
                {
                    From = $"{fromName} <{fromEmail}>",
                    To = { adminEmail },
                    Subject = emailSubject,
                    HtmlBody = htmlBuilder.ToString()
                };

                if (!string.IsNullOrWhiteSpace(email))
                {
                    emailMessage.ReplyTo.Add(email);
                }

                _logger.LogInformation("Attempting to send contact email notification to {AdminEmail} via Resend HTTPS API...", adminEmail);

                var response = await _resend.EmailSendAsync(emailMessage);

                if (response != null && response.Success)
                {
                    _logger.LogInformation("Contact notification email sent successfully via Resend. Message ID: {MessageId}", response.Content);
                    return true;
                }
                else
                {
                    _logger.LogError("Resend API was unable to deliver email notification.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send contact notification email via Resend API.");
                return false;
            }
        }
    }
}
