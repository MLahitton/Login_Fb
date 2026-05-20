using Application.Interfaces;
using Infrastructure.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var smtpClient = new SmtpClient();

        await smtpClient.ConnectAsync(
            _settings.SmtpServer,
            _settings.Port,
            SecureSocketOptions.StartTls);

        await smtpClient.AuthenticateAsync(_settings.SenderEmail, _settings.Password);
        await smtpClient.SendAsync(message);
        await smtpClient.DisconnectAsync(true);
    }

    public Task SendVerificationCodeAsync(string toEmail, string fullName, string code)
    {
        var subject = "Confirma tu cuenta";

        var body = $"""
        <h2>Hola {fullName}</h2>
        <p>Gracias por registrarte.</p>
        <p>Tu código de confirmación es:</p>
        <h1 style="letter-spacing: 4px;">{code}</h1>
        <p>Este código vence en 10 minutos.</p>
        """;

        return SendEmailAsync(toEmail, subject, body);
    }

    public Task SendPasswordResetCodeAsync(string toEmail, string fullName, string code)
    {
        var subject = "Recuperación de contraseña";

        var body = $"""
        <h2>Hola {fullName}</h2>
        <p>Recibimos una solicitud para recuperar tu contraseña.</p>
        <p>Tu código de recuperación es:</p>
        <h1 style="letter-spacing: 4px;">{code}</h1>
        <p>Este código vence en 10 minutos.</p>
        <p>Si tú no solicitaste esto, ignora este mensaje.</p>
        """;

        return SendEmailAsync(toEmail, subject, body);
    }
}