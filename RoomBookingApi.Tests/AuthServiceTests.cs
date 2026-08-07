using Xunit;
using NSubstitute;
using RoomBookingApi.Application.Services;
using RoomBookingApi.Application.Interfaces.Repositories;
using RoomBookingApi.Application.Interfaces.Services;
using RoomBookingApi.Application.Dto;
using RoomBookingApi.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RoomBookingApi.Tests
{
    public class AuthServiceTests
    {
        private readonly IUnitOfWork _uow;
        private readonly IEmailCodeRepository _codeRepo;
        private readonly UserManager<UserApplication> _userManager;
        private readonly IJwtService _jwtService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AuthService> _logger;
        private readonly AuthService _service;

        public AuthServiceTests()
        {
            _codeRepo = Substitute.For<IEmailCodeRepository>();
            _jwtService = Substitute.For<IJwtService>();
            _emailSender = Substitute.For<IEmailSender>();
            _logger = Substitute.For<ILogger<AuthService>>();

            _uow = Substitute.For<IUnitOfWork>();
            _uow.Codes.Returns(_codeRepo);

            var store = Substitute.For<IUserStore<UserApplication>>();
            _userManager = Substitute.For<UserManager<UserApplication>>(
                store, null, null, null, null, null, null, null, null);

            _service = new AuthService(_userManager, _logger, _uow, _jwtService, _emailSender);
        }

        /// <summary>
        /// Тест: регистрация нового пользователя с валидными данными.
        /// Проверяет, что пользователь создаётся, добавляется в роль "User" и отправляется письмо с кодом.
        /// </summary>
        [Fact]
        public async Task RegisterAsync_ShouldCreateUser_WhenValid()
        {
            // Arrange
            var request = new RegisterRequestDto
            {
                Name = "Test",
                Email = "test@example.com",
                Password = "Pass123"
            };
            _userManager.FindByEmailAsync(request.Email).Returns((UserApplication)null);
            _userManager.CreateAsync(Arg.Any<UserApplication>(), request.Password)
                        .Returns(IdentityResult.Success);
            _userManager.AddToRoleAsync(Arg.Any<UserApplication>(), "User")
                        .Returns(IdentityResult.Success);
            _emailSender.SendAsync(request.Email, Arg.Any<int>()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.RegisterAsync(request);

            // Assert
            Assert.True(result.Success);
            await _userManager.Received(1).CreateAsync(Arg.Any<UserApplication>(), request.Password);
            await _emailSender.Received(1).SendAsync(request.Email, Arg.Any<int>());
        }

        /// <summary>
        /// Тест: логин с корректными учётными данными.
        /// Проверяет, что возвращается JWT-токен и пользователь подтверждён.
        /// </summary>
        [Fact]
        public async Task LoginAsync_ShouldReturnToken_WhenCredentialsValid()
        {
            // Arrange
            var request = new LoginRequestDto { Email = "test@example.com", Password = "Pass123" };
            var user = new UserApplication { Id = 1, Email = request.Email };
            _userManager.FindByEmailAsync(request.Email).Returns(user);
            _userManager.IsEmailConfirmedAsync(user).Returns(true);
            _userManager.CheckPasswordAsync(user, request.Password).Returns(true);
            _userManager.GetRolesAsync(user).Returns(new List<string> { "User" });
            _jwtService.GenerateJwtToken(user, Arg.Any<IList<RoleApplication>>())
                       .Returns("jwt_token");

            // Act
            var result = await _service.LoginAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("jwt_token", result.Token);
        }

        /// <summary>
        /// Тест: подтверждение email по валидному коду.
        /// Проверяет, что код помечается использованным, а email пользователя подтверждается.
        /// </summary>
        [Fact]
        public async Task ConfirmEmailAsync_ShouldConfirm_WhenCodeValid()
        {
            // Arrange
            int userId = 1;
            string code = "123456";
            var user = new UserApplication { Id = userId };
            var foundCode = new EmailConfirmationCode
            {
                UserId = userId,
                Code = code,
                IsUsed = false,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            };
            _userManager.FindByIdAsync(userId.ToString()).Returns(user);
            _codeRepo.GetByUserIdAndCodeAsync(userId, code).Returns(foundCode);
            _userManager.GenerateEmailConfirmationTokenAsync(user).Returns("token");
            _userManager.ConfirmEmailAsync(user, "token").Returns(IdentityResult.Success);
            _uow.SaveChangesAsync().Returns(1);

            // Act
            var result = await _service.ConfirmEmailAsync(userId, code);

            // Assert
            Assert.True(result);
            Assert.True(foundCode.IsUsed);
            await _codeRepo.Received(1).UpdateAsync(foundCode);
            await _uow.Received(1).SaveChangesAsync();
        }

        /// <summary>
        /// Тест: повторная отправка кода подтверждения, когда пользователь не подтверждён и нет активного кода.
        /// Проверяет, что письмо отправляется повторно.
        /// </summary>
        [Fact]
        public async Task ResendConfirmationCodeAsync_ShouldSendEmail_WhenNotConfirmedAndNoActiveCode()
        {
            // Arrange
            int userId = 1;
            var user = new UserApplication { Id = userId, Email = "test@example.com" };
            _userManager.FindByIdAsync(userId.ToString()).Returns(user);
            _userManager.IsEmailConfirmedAsync(user).Returns(false);
            _codeRepo.HasUnusedValidCodeAsync(userId).Returns(false);
            _emailSender.SendAsync(user.Email, userId).Returns(Task.CompletedTask);

            // Act
            var result = await _service.ResendConfirmationCodeAsync(userId);

            // Assert
            Assert.True(result);
            await _emailSender.Received(1).SendAsync(user.Email, userId);
        }
    }
}