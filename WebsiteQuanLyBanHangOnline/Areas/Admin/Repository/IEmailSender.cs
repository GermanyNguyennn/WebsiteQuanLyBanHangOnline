﻿namespace WebsiteQuanLyBanHangOnline.Areas.Admin.Repository
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
    }
}
