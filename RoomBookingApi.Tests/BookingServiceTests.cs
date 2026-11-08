using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RoomBookingApi.Application.Dto;
using RoomBookingApi.Application.Exceptions;
using RoomBookingApi.Application.Interfaces.Repositories;
using RoomBookingApi.Application.Interfaces.Services;
using RoomBookingApi.Application.Interfaces.Strategies;
using RoomBookingApi.Application.Services;
using RoomBookingApi.Application.Strategies;
using RoomBookingApi.Domain.Entities;
using RoomBookingApi.Domain.Enums;
using Xunit;

namespace RoomBookingApi.Tests
{
    public class BookingServiceTests
    {
        private readonly IUnitOfWork _uow;
        private readonly IBookingRepository _bookingRepo;
        private readonly IRoomRepository _roomRepo;
        private readonly IAvailabilityChecker _availabilityChecker;
        private readonly ITelegramNotifier _telegramNotifier;
        private readonly BookingService _sut;

        public BookingServiceTests()
        {
            _uow = Substitute.For<IUnitOfWork>();
            _bookingRepo = Substitute.For<IBookingRepository>();
            _roomRepo = Substitute.For<IRoomRepository>();
            _availabilityChecker = Substitute.For<IAvailabilityChecker>();
            _telegramNotifier = Substitute.For<ITelegramNotifier>();
            var logger = Substitute.For<ILogger<BookingService>>();

            _uow.Bookings.Returns(_bookingRepo);
            _uow.Rooms.Returns(_roomRepo);

            var pricing = new PricingService(new IPricingStrategy[]
            {
                new WeekdayPricingStrategy(),
                new WeekendPricingStrategy()
            });

            _sut = new BookingService(_uow, _availabilityChecker, pricing, logger, _telegramNotifier);
        }

        private void SetupTransactionSuccess()
        {
            _uow.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            _uow.RollbackAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        }

        private static (DateTime start, DateTime end) FutureSlot(int hoursFromNow = 10)
        {
            var start = DateTime.UtcNow.Date.AddDays(1).AddHours(hoursFromNow);
            return (start, start.AddHours(2));
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldCreateBooking_WhenAvailable()
        {
            SetupTransactionSuccess();
            var (start, end) = FutureSlot();
            var room = new Room
            {
                Id = 1,
                RoomName = "Room A",
                PricePerHour = 100m,
                IsActive = true,
                Capacity = 5
            };

            _roomRepo.GetByIdForUpdateAsync(1).Returns(room);
            _availabilityChecker.IsAvailableAsync(1, start, end).Returns(true);
            _bookingRepo.AddAsync(Arg.Any<Booking>()).Returns(Task.CompletedTask);
            _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            var result = await _sut.CreateBookingAsync(1, 10, start, end);

            Assert.NotNull(result);
            Assert.Equal(1, result.RoomId);
            Assert.Equal(10, result.UserId);
            Assert.Equal(Status.Pending, result.Status);
            Assert.Equal("Room A", result.RoomName);

            await _uow.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
            await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
            await _uow.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            await _bookingRepo.Received(1).AddAsync(Arg.Is<Booking>(b =>
                b.RoomId == 1 &&
                b.UserId == 10 &&
                b.Status == Status.Pending &&
                b.Version == 0));
            await _telegramNotifier.Received(1).NotifyAsync(Arg.Is<string>(s => s.Contains("New booking")));
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldThrow_WhenStartAfterEnd()
        {
            var start = DateTime.UtcNow.Date.AddDays(1).AddHours(12);
            var end = start.AddHours(-1);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _sut.CreateBookingAsync(1, 10, start, end));

            await _uow.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldThrow_WhenStartInPast()
        {
            var start = DateTime.UtcNow.AddHours(-1);
            var end = start.AddHours(2);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _sut.CreateBookingAsync(1, 10, start, end));
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldThrow_WhenDurationExceeds24Hours()
        {
            var start = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
            var end = start.AddHours(25);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _sut.CreateBookingAsync(1, 10, start, end));
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldRollbackAndThrow_WhenRoomNotFound()
        {
            SetupTransactionSuccess();
            var (start, end) = FutureSlot();
            _roomRepo.GetByIdForUpdateAsync(99).Returns((Room?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.CreateBookingAsync(99, 10, start, end));

            await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
            await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
            await _telegramNotifier.DidNotReceive().NotifyAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldRollbackAndThrow_WhenRoomInactive()
        {
            SetupTransactionSuccess();
            var (start, end) = FutureSlot();
            var room = new Room { Id = 1, IsActive = false, PricePerHour = 100m };
            _roomRepo.GetByIdForUpdateAsync(1).Returns(room);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _sut.CreateBookingAsync(1, 10, start, end));

            await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldRollbackAndThrow_WhenNotAvailable()
        {
            SetupTransactionSuccess();
            var (start, end) = FutureSlot();
            var room = new Room { Id = 1, RoomName = "R", PricePerHour = 100m, IsActive = true };
            _roomRepo.GetByIdForUpdateAsync(1).Returns(room);
            _availabilityChecker.IsAvailableAsync(1, start, end).Returns(false);

            await Assert.ThrowsAsync<BookingConflictException>(() =>
                _sut.CreateBookingAsync(1, 10, start, end));

            await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
            await _telegramNotifier.DidNotReceive().NotifyAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task CancelBookingAsync_ShouldCancel_WhenOwnerCancels()
        {
            var booking = new Booking
            {
                Id = 5,
                UserId = 10,
                Status = Status.Pending,
                Version = 0,
                StartTime = DateTime.UtcNow.AddHours(3),
                EndTime = DateTime.UtcNow.AddHours(5)
            };
            _bookingRepo.GetByIdAsync(5).Returns(booking);
            _bookingRepo.UpdateAsync(booking).Returns(Task.CompletedTask);
            _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            await _sut.CancelBookingAsync(5, 10, isAdmin: false);

            Assert.Equal(Status.Cancelled, booking.Status);
            Assert.Equal(1, booking.Version);
            await _bookingRepo.Received(1).UpdateAsync(booking);
            await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
            await _telegramNotifier.Received(1).NotifyAsync(
                Arg.Is<string>(s => s.Contains("cancelled") && s.Contains("owner")));
        }

        [Fact]
        public async Task CancelBookingAsync_ShouldAllowAdmin()
        {
            var booking = new Booking
            {
                Id = 5,
                UserId = 10,
                Status = Status.Pending,
                Version = 0,
                StartTime = DateTime.UtcNow.AddHours(3),
                EndTime = DateTime.UtcNow.AddHours(5)
            };
            _bookingRepo.GetByIdAsync(5).Returns(booking);
            _bookingRepo.UpdateAsync(booking).Returns(Task.CompletedTask);
            _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            await _sut.CancelBookingAsync(5, 999, isAdmin: true);

            Assert.Equal(Status.Cancelled, booking.Status);
            await _telegramNotifier.Received(1).NotifyAsync(
                Arg.Is<string>(s => s.Contains("cancelled") && s.Contains("admin")));
        }

        [Fact]
        public async Task CancelBookingAsync_ShouldThrowUnauthorized_WhenNotOwner()
        {
            var booking = new Booking
            {
                Id = 5,
                UserId = 10,
                Status = Status.Pending,
                StartTime = DateTime.UtcNow.AddHours(3)
            };
            _bookingRepo.GetByIdAsync(5).Returns(booking);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _sut.CancelBookingAsync(5, 999, isAdmin: false));
        }

        [Fact]
        public async Task CancelBookingAsync_ShouldThrowConflict_WhenAlreadyCancelled()
        {
            var booking = new Booking
            {
                Id = 5,
                UserId = 10,
                Status = Status.Cancelled,
                StartTime = DateTime.UtcNow.AddHours(3)
            };
            _bookingRepo.GetByIdAsync(5).Returns(booking);

            await Assert.ThrowsAsync<BookingConflictException>(() =>
                _sut.CancelBookingAsync(5, 10, isAdmin: false));
        }

        [Fact]
        public async Task CancelBookingAsync_ShouldThrowConflict_WhenAlreadyStarted()
        {
            var booking = new Booking
            {
                Id = 5,
                UserId = 10,
                Status = Status.Pending,
                StartTime = DateTime.UtcNow.AddHours(-1)
            };
            _bookingRepo.GetByIdAsync(5).Returns(booking);

            await Assert.ThrowsAsync<BookingConflictException>(() =>
                _sut.CancelBookingAsync(5, 10, isAdmin: false));
        }

        [Fact]
        public async Task CancelBookingAsync_ShouldThrowNotFound_WhenMissing()
        {
            _bookingRepo.GetByIdAsync(999).Returns((Booking?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.CancelBookingAsync(999, 10, isAdmin: false));
        }

        [Fact]
        public async Task ConfirmBookingAsync_ShouldConfirm_WhenPending()
        {
            var booking = new Booking { Id = 5, Status = Status.Pending, Version = 0 };
            _bookingRepo.GetByIdAsync(5).Returns(booking);
            _bookingRepo.UpdateAsync(booking).Returns(Task.CompletedTask);
            _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            await _sut.ConfirmBookingAsync(5);

            Assert.Equal(Status.Confirmed, booking.Status);
            Assert.Equal(1, booking.Version);
            await _bookingRepo.Received(1).UpdateAsync(booking);
            await _telegramNotifier.Received(1).NotifyAsync(Arg.Is<string>(s => s.Contains("confirmed")));
        }

        [Fact]
        public async Task ConfirmBookingAsync_ShouldThrowConflict_WhenAlreadyConfirmed()
        {
            var booking = new Booking { Id = 5, Status = Status.Confirmed };
            _bookingRepo.GetByIdAsync(5).Returns(booking);

            await Assert.ThrowsAsync<BookingConflictException>(() =>
                _sut.ConfirmBookingAsync(5));
        }

        [Fact]
        public async Task ConfirmBookingAsync_ShouldThrowConflict_WhenCancelled()
        {
            var booking = new Booking { Id = 5, Status = Status.Cancelled };
            _bookingRepo.GetByIdAsync(5).Returns(booking);

            await Assert.ThrowsAsync<BookingConflictException>(() =>
                _sut.ConfirmBookingAsync(5));
        }

        [Fact]
        public async Task GetMyBookingsAsync_ShouldReturnPagedResult()
        {
            var items = new List<BookingResponseDto>
            {
                new BookingResponseDto { Id = 1, UserId = 10, RoomName = "A" },
                new BookingResponseDto { Id = 2, UserId = 10, RoomName = "B" }
            };
            _bookingRepo.CountByUserIdAsync(10).Returns(2);
            _bookingRepo.GetPagedByUserIdAsync(10, 1, 10).Returns(items);

            var result = await _sut.GetMyBookingsAsync(10, 1, 10);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
            await _bookingRepo.Received(1).GetPagedByUserIdAsync(10, 1, 10);
        }

        [Fact]
        public async Task GetMyBookingsAsync_ShouldReturnEmpty_WhenNoBookings()
        {
            _bookingRepo.CountByUserIdAsync(10).Returns(0);

            var result = await _sut.GetMyBookingsAsync(10, 1, 10);

            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);
            await _bookingRepo.DidNotReceive()
                .GetPagedByUserIdAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>());
        }

        [Fact]
        public async Task GetBookingByIdAsync_ShouldReturnDto_WhenOwner()
        {
            var dto = new BookingResponseDto { Id = 5, UserId = 10, RoomName = "A" };
            _bookingRepo.GetByIdWithIncludesAsync(5).Returns(dto);

            var result = await _sut.GetBookingByIdAsync(5, 10, isAdmin: false);

            Assert.Equal(5, result.Id);
            Assert.Equal(10, result.UserId);
        }

        [Fact]
        public async Task GetBookingByIdAsync_ShouldReturnDto_WhenAdmin()
        {
            var dto = new BookingResponseDto { Id = 5, UserId = 999 };
            _bookingRepo.GetByIdWithIncludesAsync(5).Returns(dto);

            var result = await _sut.GetBookingByIdAsync(5, 10, isAdmin: true);

            Assert.Equal(5, result.Id);
        }

        [Fact]
        public async Task GetBookingByIdAsync_ShouldThrowUnauthorized_WhenNotOwnerOrAdmin()
        {
            var dto = new BookingResponseDto { Id = 5, UserId = 999 };
            _bookingRepo.GetByIdWithIncludesAsync(5).Returns(dto);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _sut.GetBookingByIdAsync(5, 10, isAdmin: false));
        }

        [Fact]
        public async Task GetBookingByIdAsync_ShouldThrowNotFound_WhenMissing()
        {
            _bookingRepo.GetByIdWithIncludesAsync(999).Returns((BookingResponseDto?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.GetBookingByIdAsync(999, 10, isAdmin: false));
        }

        [Fact]
        public async Task GetRoomBookingsAsync_ShouldReturnPagedResult()
        {
            var filter = new BookingFilterDto();
            var items = new List<BookingResponseDto>
            {
                new BookingResponseDto { Id = 1, RoomId = 1 },
                new BookingResponseDto { Id = 2, RoomId = 1 }
            };
            _bookingRepo.GetPagedByRoomIdWithFiltersAsync(1, filter, 1, 10).Returns((2, items));

            var result = await _sut.GetRoomBookingsAsync(1, filter, 1, 10);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
        }
    }
}