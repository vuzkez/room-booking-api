using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RoomBookingApi.Application.Dto;
using Microsoft.AspNetCore.Identity.Data;
using RoomBookingApi.Application.Interfaces.Repositories;
using RoomBookingApi.Application.Interfaces.Services;
using RoomBookingApi.Application.Services;
using RoomBookingApi.Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;

namespace RoomBookingApi.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [EnableRateLimiting("RegisterLimit")]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequestDto request)
        {
            var result = await _authService.RegisterAsync(request);
            if (!result.Success)
                return BadRequest(new { Error = result.Error });

            return Created();
        }

        [HttpPost("login")]
        [EnableRateLimiting("LoginLimit")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request);
            if (!result.Success)
                return Unauthorized(new { Error = result.Error });

            return Ok(new { Token = result.Token });
        }

        [HttpPost("confirm-email")]
        [EnableRateLimiting("ConfirmEmailLimit")]
        public async Task<IActionResult> ConfirmEmailAsync([FromBody] ConfirmEmailRequestDto request)
        {
            var result = await _authService.ConfirmEmailAsync(request.Email, request.Code);
            if (!result)
                return BadRequest(new { Error = "Invalid user or code." });

            return Ok(new { Message = "Email confirmed successfully." });
        }

        [HttpPost("resend-code")]
        [EnableRateLimiting("ResendCodeLimit")]
        public async Task<IActionResult> ResendEmailCodeAsync([FromBody] ResendCodeRequestDto request)
        {
            var result = await _authService.ResendConfirmationCodeAsync(request.Email);
            if (!result)
                return BadRequest(new { Error = "Cannot resend code. User not found, already confirmed, or has active code." });

            return Ok(new { Message = "Code resent successfully." });
        }

        [HttpPost("request-email-change")]
        [Authorize]
        [EnableRateLimiting("EmailChangeLimit")]
        public async Task<IActionResult> RequestEmailChangeAsync([FromBody] EmailChangeRequestDto request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _authService.RequestEmailChangeAsync(userId, request.NewEmail);
            if (!result)
                return BadRequest(new { Error = "Cannot request email change. Email taken, same as current, or active code exists." });

            return Ok(new { Message = "Confirmation code sent to the new email." });
        }

        [HttpPost("confirm-email-change")]
        [Authorize]
        [EnableRateLimiting("ConfirmEmailLimit")]
        public async Task<IActionResult> ConfirmEmailChangeAsync([FromBody] ConfirmEmailChangeDto request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _authService.ConfirmEmailChangeAsync(userId, request.Code);
            if (!result)
                return BadRequest(new { Error = "Invalid or expired code." });

            return Ok(new { Message = "Email changed successfully." });
        }
    }
}
