using System.Net.Mail;
using System.Net;

namespace WebsiteQuanLyBanHangOnline.Areas.Admin.Repository
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("manhducnguyen23092003@gmail.com", "ltbnwvfppfbnihzl")
            };

            return client.SendMailAsync(
                new MailMessage(from: "manhducnguyen23092003@gmail.com",
                                to: email,
                                subject,
                                message
                                ));
        }
    }
}
