using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using PropertyManagement.Web.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    public SmtpEmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        var smtpSection = _config.GetSection("Smtp");
        // Check the flag before sending
        if (!bool.TryParse(smtpSection["Enabled"], out var enabled) || !enabled)
            return;

        using var client = new SmtpClient(smtpSection["Host"], int.Parse(smtpSection["Port"]))
        {
            Credentials = new NetworkCredential(smtpSection["Username"], smtpSection["Password"]),
            EnableSsl = bool.Parse(smtpSection["EnableSsl"])
        };
        var mail = new MailMessage(smtpSection["From"], to, subject, body);
        await client.SendMailAsync(mail);
    }
}