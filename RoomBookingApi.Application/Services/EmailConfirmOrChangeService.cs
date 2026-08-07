using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public EmailConfirmOrChangeService(IUnitOfWork unitOfWork, UserManager<UserApplication> userManager,ILogger<EmailConfirmOrChangeService> logger)
        {
            _uow = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }
        public async Task<bool> TryConfirmOrChangeEmail(int userId, string code,string? newEmail = null)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            var oldEmail = user.Email;
            var userCode = await _uow.Codes.GetByUserIdAndCodeAsync(userId,code);
            if (userCode == null || userCode.ExpiresAt < DateTime.UtcNow || userCode.IsUsed)
                return false;

            try
            {
                if (newEmail == null)
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var result = await _userManager.ConfirmEmailAsync(user, token);
                    if (result.Succeeded)
                    {
                        userCode.IsUsed = true;
                        await _uow.Codes.UpdateAsync(userCode);
                        await _uow.SaveChangesAsync();
                        _logger.LogInformation("User with Id: {userId} confirm email.", userId);
                        return true;
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            _logger.LogWarning("Error Identity: {Code} - {Description}", error.Code, error.Description);
                        }
                    }
                }
                else
                {
                    var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
                    var result = await _userManager.ChangeEmailAsync(user, newEmail, token);
                    if (result.Succeeded)
                    {
                        userCode.IsUsed = true;
                        await _uow.Codes.UpdateAsync(userCode);
                        await _uow.SaveChangesAsync();
                        _logger.LogInformation("User with Id: {userId} change email from: {oldEmail} to: {newEmail}.", userId, oldEmail, newEmail);
                        return true;
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            _logger.LogWarning("Error Identity: {Code} - {Description}", error.Code, error.Description);
                        }
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Exception form database. Message: {exMessage}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception on method:{nameofMethod}. Message: {exMessage}", nameof(EmailConfirmOrChangeService.TryConfirmOrChangeEmail), ex.Message);
                throw;
            }
            return false;
        }
    }
}
