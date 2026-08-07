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
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequestDto request)
        {
            var result = await _authService.RegisterAsync(request);
            if (!result.Success)
                return BadRequest(new { Error = result.Error });

            return Created();
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request);
            if (!result.Success)
                return BadRequest(new { Error = result.Error });

            return Ok(new { Token = result.Token });
        }

        [HttpPost("confirm-email")]
        [Authorize]
        public async Task<IActionResult> ConfirmEmailAsync([FromBody] ConfirmEmailRequestDto request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _authService.ConfirmEmailAsync(userId, request.Code);
            if (!result)
                return BadRequest(new { Error = "Invalid user or code." });

            return Ok(new { Message = "Email confirmed successfully." });
        }

        [HttpPost("resend-code")]
        [Authorize]
        public async Task<IActionResult> ResendEmailCodeAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _authService.ResendConfirmationCodeAsync(userId);
            if (!result)
                return BadRequest(new { Error = "Cannot resend code. User not found, already confirmed, or has active code." });

            return Ok(new { Message = "Code resent successfully." });
        }
    }
}
