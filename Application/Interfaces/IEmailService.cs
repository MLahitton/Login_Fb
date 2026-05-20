namespace Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlbody);
    Task SendVerificationCodeAsync(string toEmail, string fullName, string code);
    Task SendPasswordResetcodeAsync(string toEmail, string fullname, string code);
}