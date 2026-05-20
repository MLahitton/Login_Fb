namespace Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody);

    Task SendVerificationCodeAsync(string toEmail, string fullName, string code);

    Task SendPasswordResetCodeAsync(string toEmail, string fullName, string code);
}