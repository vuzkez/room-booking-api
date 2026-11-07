using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RoomBookingApi.Application.Interfaces.Repositories;
using RoomBookingApi.Application.Interfaces.Services;
using RoomBookingApi.Domain.Entities;

namespace RoomBookingApi.Application.Services
{
    public class EmailConfirmOrChangeService : IEmailConfirmOrChange
    {
        private readonly IUnitOfWork _uow;
        private readonly UserManager<UserApplication> _userManager;
        private readonly ILogger<EmailConfirmOrChangeService> _logger;

        public EmailConfirmOrChangeService(
            IUnitOfWork unitOfWork,
            UserManager<UserApplication> userManager,
            ILogger<EmailConfirmOrChangeService> logger)
        {
            _uow = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<bool> TryConfirmEmailAsync(int userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            var userCode = await _uow.Codes.GetByUserIdAndCodeAsync(userId, code);
            if (userCode == null || userCode.IsUsed || userCode.ExpiresAt < DateTime.UtcNow)
                return false;

            if (userCode.TargetEmail != null)
                return false;

            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var result = await _userManager.ConfirmEmailAsync(user, token);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        _logger.LogWarning("Error Identity: {Code} - {Description}", error.Code, error.Description);
                    }
                    return false;
                }

                userCode.IsUsed = true;
                await _uow.Codes.UpdateAsync(userCode);
                await _uow.SaveChangesAsync();

                _logger.LogInformation("User with Id: {userId} confirm email.", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception on method: {nameofMethod}. Message: {exMessage}", nameof(EmailConfirmOrChangeService.TryConfirmEmailAsync), ex.Message);
                throw;
            }
        }

        public async Task<bool> TryChangeEmailAsync(int userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            var userCode = await _uow.Codes.GetByUserIdAndCodeAsync(userId, code);
            if (userCode == null || userCode.IsUsed || userCode.ExpiresAt < DateTime.UtcNow)
                return false;

            if (userCode.TargetEmail == null)
                return false;

            var oldEmail = user.Email;
            var newEmail = userCode.TargetEmail;

            try
            {
                var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
                var result = await _userManager.ChangeEmailAsync(user, newEmail, token);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        _logger.LogWarning("Error Identity: {Code} - {Description}", error.Code, error.Description);
                    }
                    return false;
                }

                userCode.IsUsed = true;
                await _uow.Codes.UpdateAsync(userCode);
                await _uow.SaveChangesAsync();

                _logger.LogInformation("User with Id: {userId} change email from: {oldEmail} to: {newEmail}.", userId, oldEmail, newEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception on method: {nameofMethod}. Message: {exMessage}", nameof(EmailConfirmOrChangeService.TryChangeEmailAsync), ex.Message);
                throw;
            }
        }
    }
}