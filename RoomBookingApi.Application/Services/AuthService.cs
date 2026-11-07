using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using RoomBookingApi.Application.Dto;
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
        private readonly IEmailConfirmOrChange _emailConfirmOrChange;
        public AuthService(UserManager<UserApplication> userManager, ILogger<AuthService> logger,IUnitOfWork unitOfWork, 
            IJwtService jwtService, IEmailSender emailSenderService,IEmailConfirmOrChange emailConfirmOrChange)
        {
            _userManager = userManager;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
            _emailSenderService = emailSenderService;
            _emailConfirmOrChange = emailConfirmOrChange;
        }

        public async Task<bool> ConfirmEmailAsync(string email, string code)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return false;

            return await _emailConfirmOrChange.TryConfirmEmailAsync(user.Id, code);
        }

        public async Task<bool> RequestEmailChangeAsync(int userId, string newEmail)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            if (string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
                return false;

            var existing = await _userManager.FindByEmailAsync(newEmail);
            if (existing != null)
                return false;

            if (await _unitOfWork.Codes.HasUnusedValidCodeAsync(userId))
                return false;

            await _emailSenderService.SendAsync(newEmail, userId, targetEmail: newEmail);
            _logger.LogInformation("User {UserId} requested email change to {NewEmail}", userId, newEmail);
            return true;
        }

        public async Task<bool> ConfirmEmailChangeAsync(int userId, string code)
        {
            return await _emailConfirmOrChange.TryChangeEmailAsync(userId, code);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return new AuthResponseDto { Success = false ,Error = "Invalid email or password." };

            if (await _userManager.IsLockedOutAsync(user))
                return new AuthResponseDto { Success = false, Error = "Account is locked out. Try again later." };

            if (!await _userManager.IsEmailConfirmedAsync(user))
                return new AuthResponseDto { Success = false, Error = "Email not confirmed." };

            var result = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!result)
            {
                await _userManager.AccessFailedAsync(user);

                if (await _userManager.IsLockedOutAsync(user))
                    return new AuthResponseDto { Success = false, Error = "Too many failed attempts. Account is locked out." };

                return new AuthResponseDto { Success = false, Error = "Invalid email or password." };
            }

            await _userManager.ResetAccessFailedCountAsync(user);

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

        public async Task<bool> ResendConfirmationCodeAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return false;

            if (await _userManager.IsEmailConfirmedAsync(user))
                return false;

            if (await _unitOfWork.Codes.HasUnusedValidCodeAsync(user.Id))
                return false;

            await _emailSenderService.SendAsync(user.Email, user.Id);
            return true;
        }
    }
}
