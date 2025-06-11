namespace WebsiteQuanLyBanHangOnline.Services.EmailTemplates
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
    }
}
