using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RoomBookingApi.Application.Dto;
using RoomBookingApi.Application.Exceptions;
using RoomBookingApi.Application.Interfaces.Repositories;
using RoomBookingApi.Application.Interfaces.Services;
using RoomBookingApi.Domain.Entities;
using RoomBookingApi.Domain.Enums;

namespace RoomBookingApi.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAvailabilityChecker _availabilityChecker;
        private readonly PricingService _pricingService;
        private readonly ILogger<BookingService> _logger;
        private readonly UserManager<UserApplication> _userManager;
        private readonly ITelegramNotifier _telegaNorifier;

        public BookingService(IUnitOfWork unitOfWork, IAvailabilityChecker availabilityChecker, PricingService pricingService
            ,ILogger<BookingService> logger,UserManager<UserApplication> userManager,ITelegramNotifier telegramNotifier)
        {
            _unitOfWork = unitOfWork;
            _availabilityChecker = availabilityChecker;
            _pricingService = pricingService;
            _logger = logger;   
            _userManager = userManager;
            _telegaNorifier = telegramNotifier;
        }

        public async Task<BookingResponseDto> CreateBookingAsync(int roomId,int userId,DateTime start,DateTime end)
        {
            var isAvailable = await _availabilityChecker.IsAvailableAsync(roomId, start, end);
            if (!isAvailable)
                throw new BookingConflictException();

            var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
            if (room == null)
                throw new NotFoundException(nameof(Room), roomId);

            var totalPrice = _pricingService.CalculatePrice(room,start,end);

            var booking = new Booking
            {
                RoomId = roomId,
                UserId = userId,
                StartTime = start,
                EndTime = end,
                CreatedAt = DateTime.UtcNow,
                TotalPrice = totalPrice,
                Status = Status.Pending,
                Version = 0
            };

            try
            {
                await _unitOfWork.Bookings.AddAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                await _telegaNorifier.NotifyAsync(
                    $"New booking!\n" +
                    $"Room: {roomId}\n" +
                    $"User: {userId}\n" +
                    $"From: {start:dd.MM.yyyy HH:mm}\n" +
                    $"To: {end:dd.MM.yyyy HH:mm}");

                _logger.LogInformation("User:{userId} add booking in room:{roomId}. Booking id:{bookingId}.", userId, roomId, booking.Id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Race condition. Exception message: {exceptionMessage}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Catch exception during add and save booking. Exception message: {exceptionMessage}", ex.Message);
                throw new BookingConflictException(ex);
            }

            return new BookingResponseDto
            {
                CreatedAt = booking.CreatedAt,
                EndTime = booking.EndTime,
                Id = booking.Id,
                RoomId = booking.RoomId,
                RoomName = booking.Room?.RoomName ?? "Unknown",
                StartTime = booking.StartTime,
                Status = booking.Status,
                TotalPrice = booking.TotalPrice,
                UserId = booking.UserId,
                UserName = booking.User?.UserName ?? "Unknown"
            };
        }

        public async Task CancelBookingAsync(int bookingId, int userId, bool isAdmin)
        {
            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
                throw new NotFoundException(nameof(Booking), bookingId);

            if (!isAdmin && booking.UserId != userId)
                throw new UnauthorizedAccessException("You cannot cancel this booking.");

            if (booking.Status == Status.Cancelled)
                throw new BookingConflictException("Booking is already cancelled.");

            if (booking.StartTime <= DateTime.UtcNow)
                throw new BookingConflictException("Cannot cancel a booking that has already started.");

            booking.Status = Status.Cancelled;

            try
            {
                await _unitOfWork.Bookings.UpdateAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                await _telegaNorifier.NotifyAsync($"Booking {bookingId} cancelled by admin.");

                _logger.LogInformation("Booking {BookingId} cancelled by user {UserId}", bookingId, userId);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict while cancelling booking {BookingId}", bookingId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling booking {BookingId}", bookingId);
                throw new BookingConflictException("Error while cancelling booking.", ex);
            }
        }

        public async Task ConfirmBookingAsync(int bookingId)
        {
            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
                throw new NotFoundException(nameof(Booking), bookingId);

            if (booking.Status == Status.Confirmed)
                throw new BookingConflictException("Booking is already confirmed.");

            if (booking.Status == Status.Cancelled)
                throw new BookingConflictException("Cannot confirm a cancelled booking.");

            booking.Status = Status.Confirmed;

            try
            {
                await _unitOfWork.Bookings.UpdateAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                await _telegaNorifier.NotifyAsync($"Booking {bookingId} confirmed by admin");

                _logger.LogInformation("Booking {BookingId} confirmed by admin", bookingId);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict while confirming booking {BookingId}", bookingId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming booking {BookingId}", bookingId);
                throw new BookingConflictException("Error while confirming booking.", ex);
            }
        }

        public async Task<PagedResult<BookingResponseDto>> GetMyBookingsAsync(int userId, int page, int pageSize)
        {
            var totalCount = await _unitOfWork.Bookings.CountByUserIdAsync(userId);

            if (totalCount == 0)
            {
                return new PagedResult<BookingResponseDto>
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                    Items = new List<BookingResponseDto>()
                };
            }

            var bookings = await _unitOfWork.Bookings.GetPagedByUserIdAsync(userId, page, pageSize);

            var items = bookings.Select(b => new BookingResponseDto
            {
                Id = b.Id,
                RoomId = b.RoomId,
                RoomName = b.Room?.RoomName ?? "Unknown",
                UserId = b.UserId,
                UserName = b.User?.UserName ?? "Unknown",
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                TotalPrice = b.TotalPrice,
                Status = b.Status,
                CreatedAt = b.CreatedAt
            }).ToList();

            return new PagedResult<BookingResponseDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items
            };

        }

        public async Task<PagedResult<BookingResponseDto>> GetRoomBookingsAsync(int roomId, BookingFilterDto filter, int page, int pageSize)
        {
            var (totalCount, bookings) = await _unitOfWork.Bookings
                .GetPagedByRoomIdWithFiltersAsync(roomId, filter, page, pageSize);

            var items = bookings.Select(b => new BookingResponseDto
            {
                Id = b.Id,
                RoomId = b.RoomId,
                RoomName = b.Room?.RoomName ?? "Unknown",
                UserId = b.UserId,
                UserName = b.User?.UserName ?? "Unknown",
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                TotalPrice = b.TotalPrice,
                Status = b.Status,
                CreatedAt = b.CreatedAt
            }).ToList();

            return new PagedResult<BookingResponseDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items
            };
        }

        public async Task<BookingResponseDto> GetBookingByIdAsync(int bookingId, int userId, bool isAdmin)
        {
            var booking = await _unitOfWork.Bookings.GetByIdWithIncludesAsync(bookingId);
            if (booking == null)
                throw new NotFoundException(nameof(Booking), bookingId);

            if (!isAdmin && booking.UserId != userId)
                throw new UnauthorizedAccessException("You do not have permission to view this booking.");

            return new BookingResponseDto
            {
                Id = booking.Id,
                RoomId = booking.RoomId,
                RoomName = booking.Room?.RoomName ?? "Unknown",
                UserId = booking.UserId,
                UserName = booking.User?.UserName ?? "Unknown",
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                TotalPrice = booking.TotalPrice,
                Status = booking.Status,
                CreatedAt = booking.CreatedAt
            };
        }
    }
}
