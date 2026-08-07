using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using RoomBookingApi.Application.Exceptions;
using RoomBookingApi.Application.Interfaces.Repositories;
using RoomBookingApi.Application.Interfaces.Services;

namespace RoomBookingApi.Application.Services
{
    public class SmtpEmailSenderService : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly IUnitOfWork _uow;
        private readonly IEmailCodeGenerator _emailCodeService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SmtpEmailSenderService> _logger;
        public SmtpEmailSenderService(IConfiguration config,IUnitOfWork unitOfWork, 
            IEmailCodeGenerator emailCodeService,IWebHostEnvironment env,ILogger<SmtpEmailSenderService> logger)
        {
            _config = config;
            _uow = unitOfWork;
            _emailCodeService = emailCodeService;
            _env = env;
            _logger = logger;
        }
        public async Task SendAsync(string toEmail,int userId)
        {
            MimeMessage emailMessage = new MimeMessage();

            var senderEmail = _config["EmailSetting:SenderEmail"];
            var senderName = _config["EmailSetting:SenderName"];
            var server = _config["EmailSetting:Server"];
            var port = _config["EmailSetting:Port"];
            var password = _config["EmailSetting:Password"];
            if (password == null 
                || port == null
                || senderEmail == null
                || senderName == null
                || server == null)
            {
                _logger.LogWarning("SenderEmail,senderName,server,port or password are null. Check appsettings.json");
                List<string?> settings = new List<string?>()
                {
                    senderEmail,senderName,port,password,server
                };
                throw new EmailSettingsNullException(settings);
            }

            emailMessage.From.Add(new MailboxAddress(senderName, senderEmail));
            emailMessage.To.Add(MailboxAddress.Parse(toEmail));
            emailMessage.Subject = "Message from RoomBookingApi";

            var templatePath = Path.Combine(_env.ContentRootPath, "Email", "ConfirmationCode", "ConfirmationCode.html");
            if (!File.Exists(templatePath))
            {
                _logger.LogWarning($"{templatePath} is not exists.");
                throw new FileNotFoundException(templatePath);
            }
            var baseMessage = await File.ReadAllTextAsync(templatePath);
            var generatedCode = _emailCodeService.Generate(userId);
            var body = baseMessage.Replace("123456", generatedCode.Code);

            emailMessage.Body = new TextPart("html") { Text = body };

            try
            {
                await _uow.Codes.AddAsync(generatedCode);
                await _uow.SaveChangesAsync();

                using (var smtp = new SmtpClient())
                {
                    await smtp.ConnectAsync(server, Int32.Parse(port), SecureSocketOptions.StartTls);
                    await smtp.AuthenticateAsync(senderEmail, password);
                    await smtp.SendAsync(emailMessage);
                    await smtp.DisconnectAsync(true);
                }
                _logger.LogInformation("Email successful sent to User with Id: {userId} and to Email: {toEmail}",userId,toEmail);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Exception from database. Message: {exMessage}", ex.Message);
                throw;
            }
            catch (CommandException ex)
            {
                _logger.LogError(ex, "Command exception from smtpclient. Message: {exMessage}",ex.Message);
                throw;
            }
            catch (ProtocolException ex)
            {
                _logger.LogError(ex, "Protocol exception from smtpclient. Message: {exMessage}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception on method:{nameofMethod}. Message: {exMessage}",nameof(SmtpEmailSenderService.SendAsync), ex.Message);
                throw;
            }
        }
    }
}
