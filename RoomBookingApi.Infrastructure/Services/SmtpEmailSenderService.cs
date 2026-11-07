using System;
using System.IO;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using RoomBookingApi.Application.Interfaces.Repositories;
using RoomBookingApi.Application.Interfaces.Services;
using RoomBookingApi.Application.Options;

namespace RoomBookingApi.Infrastructure.Services
{
    public class SmtpEmailSenderService : IEmailSender
    {
        private readonly EmailSettings _settings;
        private readonly IUnitOfWork _uow;
        private readonly IEmailCodeGenerator _emailCodeService;
        private readonly IHostEnvironment _env;
        private readonly ILogger<SmtpEmailSenderService> _logger;

        public SmtpEmailSenderService(
            IOptions<EmailSettings> options,
            IUnitOfWork unitOfWork,
            IEmailCodeGenerator emailCodeService,
            IHostEnvironment env,
            ILogger<SmtpEmailSenderService> logger)
        {
            _settings = options.Value;
            _uow = unitOfWork;
            _emailCodeService = emailCodeService;
            _env = env;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, int userId, string? targetEmail = null)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            emailMessage.To.Add(MailboxAddress.Parse(toEmail));
            emailMessage.Subject = "Message from RoomBookingApi";

            var templatePath = Path.Combine(_env.ContentRootPath, "Email", "ConfirmationCode", "ConfirmationCode.html");
            if (!File.Exists(templatePath))
            {
                _logger.LogWarning("{templatePath} does not exist.", templatePath);
                throw new FileNotFoundException(templatePath);
            }

            var baseMessage = await File.ReadAllTextAsync(templatePath);
            var generatedCode = _emailCodeService.Generate(userId, targetEmail);
            var body = baseMessage.Replace("123456", generatedCode.Code);
            emailMessage.Body = new TextPart("html") { Text = body };

            try
            {
                await _uow.Codes.AddAsync(generatedCode);
                await _uow.SaveChangesAsync();

                using (var smtp = new SmtpClient())
                {
                    await smtp.ConnectAsync(_settings.Server, int.Parse(_settings.Port), SecureSocketOptions.StartTls);
                    await smtp.AuthenticateAsync(_settings.SenderEmail, _settings.Password);
                    await smtp.SendAsync(emailMessage);
                    await smtp.DisconnectAsync(true);
                }
                _logger.LogInformation("Email successfully sent to User Id: {userId} at {toEmail}", userId, toEmail);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database exception while sending email.");
                throw;
            }
            catch (CommandException ex)
            {
                _logger.LogError(ex, "Command exception from SmtpClient.");
                throw;
            }
            catch (ProtocolException ex)
            {
                _logger.LogError(ex, "Protocol exception from SmtpClient.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in {methodName}.", nameof(SmtpEmailSenderService.SendAsync));
                throw;
            }
        }
    }
}