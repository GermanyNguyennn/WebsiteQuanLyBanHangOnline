using System.Net.Mail;
using System.Net;

namespace WebsiteQuanLyBanHangOnline.Services.EmailTemplates
{
    public class EmailSender
    {
        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var fromAddress = new MailAddress("manhducnguyen23092003@gmail.com", "Shop Của Nguyễn Mạnh Đức"); // Đổi địa chỉ gửi
            var toAddress = new MailAddress(toEmail);

            const string fromPassword = "ltbnwvfppfbnihzl"; // Mật khẩu ứng dụng Gmail (App Password)

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true // ❗ Quan trọng để nội dung HTML hiển thị đúng
            })
            {
                await smtp.SendMailAsync(message);
            }
        }
    }
}
