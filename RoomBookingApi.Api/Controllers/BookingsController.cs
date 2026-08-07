using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RoomBookingApi.Application.Dto;
using RoomBookingApi.Application.Interfaces.Repositories;
using RoomBookingApi.Application.Interfaces.Services;
using RoomBookingApi.Domain.Enums;

namespace RoomBookingApi.Api.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        private bool IsAdmin() => User.IsInRole("Admin");

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var userId = GetUserId();
            var isAdmin = IsAdmin();
            var result = await _bookingService.GetBookingByIdAsync(id, userId, isAdmin);
            return Ok(result);
        }

        [HttpGet]
        [Route("room/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRoomBookingsAsync(int id,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] Status? status,
            [FromQuery] int? userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var filter = new BookingFilterDto
            {
                From = from,
                Status = status,
                To = to,
                UserId = userId,
            };

            var result = await _bookingService.GetRoomBookingsAsync(id,filter,page,pageSize);
            return Ok(result);
        }

        [HttpPost]
        [EnableRateLimiting("BookingLimit")]
        public async Task<IActionResult> CreateBookingAsync([FromBody] CreateBookingRequestDto request)
        {
            var userId = GetUserId();
            var result = await _bookingService.CreateBookingAsync(request.RoomId, userId, request.StartTime, request.EndTime);
            return Ok(result);
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> CancelBookingAsync(int id)
        {
            var userId = GetUserId();
            var isAdmin = IsAdmin();
            await _bookingService.CancelBookingAsync(id, userId, isAdmin);
            return NoContent();
        }

        [HttpPut("{id}/confirm")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ConfirmBookingAsync(int id)
        {
            await _bookingService.ConfirmBookingAsync(id);
            return NoContent();
        }

        [HttpGet]
        [Route("my")]
        public async Task<IActionResult> GetMyBookingAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetUserId();
            var result = await _bookingService.GetMyBookingsAsync(userId, page, pageSize);
            return Ok(result);
        }
    }
}
