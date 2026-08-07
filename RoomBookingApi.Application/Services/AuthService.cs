using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RoomBookingApi.Application.Dto;
using RoomBookingApi.Application.Exceptions;
using RoomBookingApi.Application.Interfaces.Repositories;
using RoomBookingApi.Application.Interfaces.Services;
using RoomBookingApi.Domain.Entities;

namespace RoomBookingApi.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<UserApplication> _userManager;
        private readonly ILogger<AuthService> _logger;
        private readonly IJwtService _jwtService;
        private readonly IEmailSender _emailSenderService;
        private readonly IUnitOfWork _unitOfWork;
        public AuthService(UserManager<UserApplication> userManager, ILogger<AuthService> logger,IUnitOfWork unitOfWork, 
            IJwtService jwtService, IEmailSender emailSenderService)
        {
            _userManager = userManager;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
            _emailSenderService = emailSenderService;
        }

        public async Task<bool> ConfirmEmailAsync(int userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) 
                return false;

            var foundCode = await _unitOfWork.Codes.GetByUserIdAndCodeAsync(userId, code);
            if (foundCode == null)
                return false;

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var updateResult = await _userManager.ConfirmEmailAsync(user, token);
            if (!updateResult.Succeeded)
            {
                string errorList = string.Join("\n", updateResult.Errors.Select(e => e.Description));
                _logger.LogError("Error update user. Message:{errorList}", errorList);
                return false;
            }
            foundCode.IsUsed = true;
            try
            {
                await _unitOfWork.Codes.UpdateAsync(foundCode);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling update code.");
                throw;
            }

            return true;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return new AuthResponseDto { Success = false ,Error = $"User with Email: {request.Email} not found."};

            if (!await _userManager.IsEmailConfirmedAsync(user))
                return new AuthResponseDto { Success = false, Error = "Email not confirmed." };

            var result = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!result)
                return new AuthResponseDto { Success = false, Error = "Password is wrong." };

            var roles = await _userManager.GetRolesAsync(user);
            var roleEntities = roles.Select(x => new RoleApplication { Name = x }).ToList();

            var token = _jwtService.GenerateJwtToken(user,roleEntities);
            return new AuthResponseDto { Success = true, Token = token };
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user != null)
                return new AuthResponseDto { Success = false, Error = $"User with Email: {request.Email} exists." };

            var newUser = new UserApplication
            {
                Email = request.Email,
                UserName = request.Name
            };

            var createResult = await _userManager.CreateAsync(newUser, request.Password);
            if (!createResult.Succeeded)
            {
                _logger.LogError("Error creation user.");
                string errorList = string.Join("\n", createResult.Errors.Select(e => e.Description));
                return new AuthResponseDto { Success = false, Error = $"Error creation user. Message: {errorList}"};
            }

            var resultAddToRole = await _userManager.AddToRoleAsync(newUser, "User");
            if (!resultAddToRole.Succeeded)
            {
                _logger.LogError("Error add user to role User.");
                string errorList = string.Join("\n", resultAddToRole.Errors.Select(e => e.Description));
                return new AuthResponseDto { Success = false, Error = $"Error add to role user. Message: {errorList}" };
            }

            await _emailSenderService.SendAsync(newUser.Email, newUser.Id);
            _logger.LogInformation("User has successfully registered. User Id: {userId}. User Email: {userEmail}. User Name: {userName}",newUser.Id,newUser.Email,newUser.UserName);
            return new AuthResponseDto { Success = true };
        }

        public async Task<bool> ResendConfirmationCodeAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            if (await _userManager.IsEmailConfirmedAsync(user))
                return false;

            if (await _unitOfWork.Codes.HasUnusedValidCodeAsync(user.Id))
                return false;

            await _emailSenderService.SendAsync(user.Email, userId);
            return true;
        }
    }
}
