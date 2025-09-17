using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.Shared.Settings;

namespace NFIT.Infrastructure.Services;

public class EmailService:IEmailService
{
    private readonly EmailSettings _emailSettings;

    public EmailService(IOptions<EmailSettings> options)
    {
        _emailSettings = options.Value;
    }

    public async Task SendEmailAsync(IEnumerable<string> toEmails, string subject, string body)
    {
        using var smtp = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
        {
            Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.Password),
            EnableSsl = true
        };

        var message = new MailMessage
        {
            From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        foreach (var email in toEmails.Distinct()) // təkrarları aradan qaldırır
        {

            message.To.Add(email.Trim());
        }

        await smtp.SendMailAsync(message);
    }
}
