using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using project.utils.dto;
namespace project.utils.services;

public class emailService(IConfiguration configuration)
{
    public virtual async Task SendEmailAsync(emailSendDto data, CancellationToken cancellationToken = default)
    {
        var username = configuration["Email:UserName"] ?? throw new InvalidOperationException("Configura Email:UserName.");
        var host = configuration["Email:Host"] ?? throw new InvalidOperationException("Configura Email:Host.");
        var port = configuration.GetValue<int>("Email:Port", 587);
        using var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(configuration["Email:From"] ?? username));
        message.To.Add(MailboxAddress.Parse(data.email));
        message.Subject = data.subject;
        message.Body = new BodyBuilder { HtmlBody = data.message }.ToMessageBody();
        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(host, port, port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, cancellationToken);
        await smtp.AuthenticateAsync(username, configuration["Email:Password"] ?? throw new InvalidOperationException("Configura Email:Password."), cancellationToken);
        await smtp.SendAsync(message, cancellationToken);
        await smtp.DisconnectAsync(true, cancellationToken);
    }
}
