using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RoomBookingApi.Application.Dto;
using RoomBookingApi.Application.Interfaces.Repositories;
using RoomBookingApi.Application.Interfaces.Services;
using RoomBookingApi.Application.Services;
using RoomBookingApi.Domain.Entities;
using Xunit;

namespace RoomBookingApi.Tests
{
    public class AuthServiceTests
    {
        private readonly IUnitOfWork _uow;
        private readonly IEmailCodeRepository _codeRepo;
        private readonly UserManager<UserApplication> _userManager;
        private readonly IJwtService _jwtService;
        private readonly IEmailSender _emailSender;
        private readonly IEmailConfirmOrChange _emailConfirmOrChange;
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _uow = Substitute.For<IUnitOfWork>();
            _codeRepo = Substitute.For<IEmailCodeRepository>();
            _uow.Codes.Returns(_codeRepo);

            _userManager = Substitute.For<UserManager<UserApplication>>(
                Substitute.For<IUserStore<UserApplication>>(),
                null, null, null, null, null, null, null, null);

            _jwtService = Substitute.For<IJwtService>();
            _emailSender = Substitute.For<IEmailSender>();
            _emailConfirmOrChange = Substitute.For<IEmailConfirmOrChange>();
            var logger = Substitute.For<ILogger<AuthService>>();

            _sut = new AuthService(_userManager, logger, _uow, _jwtService, _emailSender, _emailConfirmOrChange);
        }

        [Fact]
        public async Task RegisterAsync_ShouldCreateUserAndSendEmail_WhenValid()
        {
            var request = new RegisterRequestDto
            {
                Name = "Test",
                Email = "test@test.com",
                Password = "Pass123!"
            };
            _userManager.FindByEmailAsync(request.Email).Returns((UserApplication?)null);
            _userManager.CreateAsync(Arg.Any<UserApplication>(), request.Password)
                .Returns(IdentityResult.Success);
            _userManager.AddToRoleAsync(Arg.Any<UserApplication>(), "User")
                .Returns(IdentityResult.Success);
            _emailSender.SendAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string?>())
                .Returns(Task.CompletedTask);

            var result = await _sut.RegisterAsync(request);

            Assert.True(result.Success);
            Assert.Null(result.Error);
            await _userManager.Received(1).CreateAsync(
                Arg.Is<UserApplication>(u => u.Email == request.Email && u.UserName == request.Name),
                request.Password);
            await _userManager.Received(1).AddToRoleAsync(Arg.Any<UserApplication>(), "User");
            await _emailSender.Received(1).SendAsync(request.Email, Arg.Any<int>(), Arg.Any<string?>());
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnError_WhenEmailExists()
        {
            var request = new RegisterRequestDto { Name = "T", Email = "dup@test.com", Password = "Pass123!" };
            _userManager.FindByEmailAsync(request.Email).Returns(new UserApplication { Email = request.Email });

            var result = await _sut.RegisterAsync(request);

            Assert.False(result.Success);
            Assert.NotNull(result.Error);
            Assert.Contains("exists", result.Error!);
            await _userManager.DidNotReceive().CreateAsync(Arg.Any<UserApplication>(), Arg.Any<string>());
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnError_WhenCreateFails()
        {
            var request = new RegisterRequestDto { Name = "T", Email = "x@test.com", Password = "weak" };
            _userManager.FindByEmailAsync(request.Email).Returns((UserApplication?)null);
            _userManager.CreateAsync(Arg.Any<UserApplication>(), request.Password)
                .Returns(IdentityResult.Failed(new IdentityError { Description = "Weak password" }));

            var result = await _sut.RegisterAsync(request);

            Assert.False(result.Success);
            Assert.Contains("Weak password", result.Error!);
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnError_WhenAddToRoleFails()
        {
            var request = new RegisterRequestDto { Name = "T", Email = "x@test.com", Password = "Pass123!" };
            _userManager.FindByEmailAsync(request.Email).Returns((UserApplication?)null);
            _userManager.CreateAsync(Arg.Any<UserApplication>(), request.Password).Returns(IdentityResult.Success);
            _userManager.AddToRoleAsync(Arg.Any<UserApplication>(), "User")
                .Returns(IdentityResult.Failed(new IdentityError { Description = "Role error" }));

            var result = await _sut.RegisterAsync(request);

            Assert.False(result.Success);
            Assert.Contains("Role error", result.Error!);
            await _emailSender.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string?>());
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnToken_WhenCredentialsValid()
        {
            var request = new LoginRequestDto { Email = "test@test.com", Password = "Pass123!" };
            var user = new UserApplication { Id = 1, Email = request.Email };

            _userManager.FindByEmailAsync(request.Email).Returns(user);
            _userManager.IsLockedOutAsync(user).Returns(false);
            _userManager.IsEmailConfirmedAsync(user).Returns(true);
            _userManager.CheckPasswordAsync(user, request.Password).Returns(true);
            _userManager.GetRolesAsync(user).Returns(new List<string> { "User" });
            _jwtService.GenerateJwtToken(user, Arg.Any<IList<RoleApplication>>()).Returns("jwt_token");

            var result = await _sut.LoginAsync(request);

            Assert.True(result.Success);
            Assert.Equal("jwt_token", result.Token);
            Assert.Null(result.Error);
            await _userManager.Received(1).ResetAccessFailedCountAsync(user);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnError_WhenUserNotFound()
        {
            _userManager.FindByEmailAsync(Arg.Any<string>()).Returns((UserApplication?)null);

            var result = await _sut.LoginAsync(new LoginRequestDto { Email = "none@test.com", Password = "x" });

            Assert.False(result.Success);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnError_WhenLockedOut()
        {
            var user = new UserApplication { Id = 1, Email = "test@test.com" };
            _userManager.FindByEmailAsync(Arg.Any<string>()).Returns(user);
            _userManager.IsLockedOutAsync(user).Returns(true);

            var result = await _sut.LoginAsync(new LoginRequestDto { Email = "test@test.com", Password = "x" });

            Assert.False(result.Success);
            Assert.Contains("locked", result.Error!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnError_WhenEmailNotConfirmed()
        {
            var user = new UserApplication { Id = 1, Email = "test@test.com" };
            _userManager.FindByEmailAsync(Arg.Any<string>()).Returns(user);
            _userManager.IsLockedOutAsync(user).Returns(false);
            _userManager.IsEmailConfirmedAsync(user).Returns(false);

            var result = await _sut.LoginAsync(new LoginRequestDto { Email = "test@test.com", Password = "x" });

            Assert.False(result.Success);
            Assert.Contains("Email not confirmed", result.Error!);
        }

        [Fact]
        public async Task LoginAsync_ShouldCallAccessFailed_WhenPasswordWrong()
        {
            var user = new UserApplication { Id = 1, Email = "test@test.com" };
            _userManager.FindByEmailAsync(Arg.Any<string>()).Returns(user);
            _userManager.IsLockedOutAsync(user).Returns(false);
            _userManager.IsEmailConfirmedAsync(user).Returns(true);
            _userManager.CheckPasswordAsync(user, Arg.Any<string>()).Returns(false);
            _userManager.AccessFailedAsync(user).Returns(IdentityResult.Success);

            var result = await _sut.LoginAsync(new LoginRequestDto { Email = "test@test.com", Password = "wrong" });

            Assert.False(result.Success);
            Assert.NotNull(result.Error);
            await _userManager.Received(1).AccessFailedAsync(user);
        }

        [Fact]
        public async Task ConfirmEmailAsync_ShouldDelegateToEmailConfirmOrChange()
        {
            var user = new UserApplication { Id = 1, Email = "test@test.com" };
            _userManager.FindByEmailAsync("test@test.com").Returns(user);
            _emailConfirmOrChange.TryConfirmEmailAsync(1, "123456").Returns(true);

            var result = await _sut.ConfirmEmailAsync("test@test.com", "123456");

            Assert.True(result);
            await _emailConfirmOrChange.Received(1).TryConfirmEmailAsync(1, "123456");
        }

        [Fact]
        public async Task ConfirmEmailAsync_ShouldReturnFalse_WhenUserNotFound()
        {
            _userManager.FindByEmailAsync(Arg.Any<string>()).Returns((UserApplication?)null);

            var result = await _sut.ConfirmEmailAsync("none@test.com", "123456");

            Assert.False(result);
            await _emailConfirmOrChange.DidNotReceive().TryConfirmEmailAsync(Arg.Any<int>(), Arg.Any<string>());
        }

        [Fact]
        public async Task ResendConfirmationCodeAsync_ShouldSend_WhenNotConfirmedAndNoActiveCode()
        {
            var user = new UserApplication { Id = 1, Email = "test@test.com" };
            _userManager.FindByEmailAsync(user.Email).Returns(user);
            _userManager.IsEmailConfirmedAsync(user).Returns(false);
            _codeRepo.HasUnusedValidCodeAsync(user.Id).Returns(false);
            _emailSender.SendAsync(user.Email, user.Id, Arg.Any<string?>()).Returns(Task.CompletedTask);

            var result = await _sut.ResendConfirmationCodeAsync(user.Email);

            Assert.True(result);
            await _emailSender.Received(1).SendAsync(user.Email, user.Id, Arg.Any<string?>());
        }

        [Fact]
        public async Task ResendConfirmationCodeAsync_ShouldReturnFalse_WhenAlreadyConfirmed()
        {
            var user = new UserApplication { Id = 1, Email = "test@test.com" };
            _userManager.FindByEmailAsync(user.Email).Returns(user);
            _userManager.IsEmailConfirmedAsync(user).Returns(true);

            var result = await _sut.ResendConfirmationCodeAsync(user.Email);

            Assert.False(result);
            await _emailSender.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string?>());
        }

        [Fact]
        public async Task ResendConfirmationCodeAsync_ShouldReturnFalse_WhenActiveCodeExists()
        {
            var user = new UserApplication { Id = 1, Email = "test@test.com" };
            _userManager.FindByEmailAsync(user.Email).Returns(user);
            _userManager.IsEmailConfirmedAsync(user).Returns(false);
            _codeRepo.HasUnusedValidCodeAsync(user.Id).Returns(true);

            var result = await _sut.ResendConfirmationCodeAsync(user.Email);

            Assert.False(result);
        }

        [Fact]
        public async Task RequestEmailChangeAsync_ShouldReturnTrue_WhenValid()
        {
            var user = new UserApplication { Id = 1, Email = "old@test.com" };
            _userManager.FindByIdAsync("1").Returns(user);
            _userManager.FindByEmailAsync("new@test.com").Returns((UserApplication?)null);
            _codeRepo.HasUnusedValidCodeAsync(1).Returns(false);
            _emailSender.SendAsync("new@test.com", 1, "new@test.com").Returns(Task.CompletedTask);

            var result = await _sut.RequestEmailChangeAsync(1, "new@test.com");

            Assert.True(result);
            await _emailSender.Received(1).SendAsync("new@test.com", 1, "new@test.com");
        }

        [Fact]
        public async Task RequestEmailChangeAsync_ShouldReturnFalse_WhenSameEmail()
        {
            var user = new UserApplication { Id = 1, Email = "same@test.com" };
            _userManager.FindByIdAsync("1").Returns(user);

            var result = await _sut.RequestEmailChangeAsync(1, "same@test.com");

            Assert.False(result);
        }

        [Fact]
        public async Task RequestEmailChangeAsync_ShouldReturnFalse_WhenEmailTaken()
        {
            var user = new UserApplication { Id = 1, Email = "old@test.com" };
            _userManager.FindByIdAsync("1").Returns(user);
            _userManager.FindByEmailAsync("taken@test.com")
                .Returns(new UserApplication { Email = "taken@test.com" });

            var result = await _sut.RequestEmailChangeAsync(1, "taken@test.com");

            Assert.False(result);
        }

        [Fact]
        public async Task ConfirmEmailChangeAsync_ShouldDelegateToEmailConfirmOrChange()
        {
            _emailConfirmOrChange.TryChangeEmailAsync(1, "123456").Returns(true);

            var result = await _sut.ConfirmEmailChangeAsync(1, "123456");

            Assert.True(result);
            await _emailConfirmOrChange.Received(1).TryChangeEmailAsync(1, "123456");
        }
    }
}