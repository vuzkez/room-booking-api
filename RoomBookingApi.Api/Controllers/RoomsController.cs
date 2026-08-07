using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomBookingApi.Application.Dto;
using RoomBookingApi.Application.Interfaces.Services;

namespace RoomBookingApi.Api.Controllers
{
    [ApiController]
    [Route("api/rooms")]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomsController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var result = await _roomService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAsync([FromBody] CreateRoomRequestDto request)
        {
            var result = await _roomService.CreateAsync(request);
            return Ok(result);
        }

        [HttpPut]
        [Authorize(Roles = "Admin")]
        [Route("{id}")]
        public async Task<IActionResult> UpdateByIdAsync(int id,[FromBody] UpdateRoomRequestDto request)
        {
            var result = await _roomService.UpdateByIdAsync(id, request);
            return Ok(result);
        }

        [HttpDelete]
        [Authorize(Roles = "Admin")]
        [Route("{id}")]
        public async Task<IActionResult> DeleteByIdAsync(int id)
        {
            await _roomService.DeleteByIdAsync(id);
            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync(
            [FromQuery]int? minCapacity,
            [FromQuery]string? location,
            [FromQuery]bool? IsActive = null,
            [FromQuery]int page = 1,
            [FromQuery]int pageSize = 10)
        {
            var filter = new RoomFilterDto {
                IsActive = IsActive,
                Location = location,
                MinCapacity = minCapacity
            };

            var result = await _roomService.GetAllAsync(filter, page, pageSize);
            return Ok(result);
        }
    }
}
