using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RoomBookingApi.Application.Dto;
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
        private readonly IEmailCodeRepository _codeRepo;
        private readonly IAvailabilityChecker _availabilityChecker;
        private readonly PricingService _pricingService;
        private readonly ILogger<BookingService> _logger;
        private readonly UserManager<UserApplication> _userManager;
        private readonly ITelegramNotifier _telegramNotifier;
        private readonly BookingService _service;

        public BookingServiceTests()
        {
            _bookingRepo = Substitute.For<IBookingRepository>();
            _roomRepo = Substitute.For<IRoomRepository>();
            _codeRepo = Substitute.For<IEmailCodeRepository>();
            _availabilityChecker = Substitute.For<IAvailabilityChecker>();
            _logger = Substitute.For<ILogger<BookingService>>();
            _userManager = Substitute.For<UserManager<UserApplication>>(
                Substitute.For<IUserStore<UserApplication>>(),
                null, null, null, null, null, null, null, null);
            _telegramNotifier = Substitute.For<ITelegramNotifier>();

            _uow = Substitute.For<IUnitOfWork>();
            _uow.Bookings.Returns(_bookingRepo);
            _uow.Rooms.Returns(_roomRepo);
            _uow.Codes.Returns(_codeRepo);

            var strategies = new IPricingStrategy[]
            {
                new WeekdayPricingStrategy(),
                new WeekendPricingStrategy()
            };
            _pricingService = new PricingService(strategies);

            _service = new BookingService(_uow, _availabilityChecker, _pricingService, _logger, _userManager, _telegramNotifier);
        }

        /// <summary>
        /// Тест: создание брони при доступности комнаты.
        /// Проверяет, что бронь создаётся с правильными данными, сохраняется и отправляется уведомление в Telegram.
        /// </summary>
        [Fact]
        public async Task CreateBookingAsync_ShouldCreateBooking_WhenAvailable()
        {
            // Arrange
            int roomId = 1, userId = 10;
            var start = DateTime.UtcNow.AddHours(1);
            var end = start.AddHours(2);
            var room = new Room { Id = roomId, PricePerHour = 50m };
            _roomRepo.GetByIdAsync(roomId).Returns(room);
            _availabilityChecker.IsAvailableAsync(roomId, start, end).Returns(true);
            _bookingRepo.AddAsync(Arg.Any<Booking>()).Returns(Task.CompletedTask);
            _uow.SaveChangesAsync().Returns(1);

            // Act
            var result = await _service.CreateBookingAsync(roomId, userId, start, end);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(roomId, result.RoomId);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(Status.Pending, result.Status);
            await _bookingRepo.Received(1).AddAsync(Arg.Any<Booking>());
            await _uow.Received(1).SaveChangesAsync();
            await _telegramNotifier.Received(1).NotifyAsync(Arg.Is<string>(msg => msg.Contains("New booking!")));
        }

        /// <summary>
        /// Тест: отмена брони владельцем.
        /// Проверяет, что статус меняется на Cancelled, бронь обновляется, и отправляется уведомление об отмене.
        /// </summary>
        [Fact]
        public async Task CancelBookingAsync_ShouldCancel_WhenUserIsOwner()
        {
            // Arrange
            int bookingId = 5, userId = 10;
            var booking = new Booking
            {
                Id = bookingId,
                UserId = userId,
                StartTime = DateTime.UtcNow.AddHours(3),
                Status = Status.Pending
            };
            _bookingRepo.GetByIdAsync(bookingId).Returns(booking);
            _uow.SaveChangesAsync().Returns(1);

            // Act
            await _service.CancelBookingAsync(bookingId, userId, false);

            // Assert
            Assert.Equal(Status.Cancelled, booking.Status);
            await _bookingRepo.Received(1).UpdateAsync(booking);
            await _uow.Received(1).SaveChangesAsync();
            await _telegramNotifier.Received(1).NotifyAsync(Arg.Is<string>(msg => msg.Contains("cancelled")));
        }

        /// <summary>
        /// Тест: подтверждение брони администратором.
        /// Проверяет, что статус меняется на Confirmed, бронь обновляется, и отправляется уведомление о подтверждении.
        /// </summary>
        [Fact]
        public async Task ConfirmBookingAsync_ShouldConfirm_WhenPending()
        {
            // Arrange
            int bookingId = 5;
            var booking = new Booking { Id = bookingId, Status = Status.Pending };
            _bookingRepo.GetByIdAsync(bookingId).Returns(booking);
            _uow.SaveChangesAsync().Returns(1);

            // Act
            await _service.ConfirmBookingAsync(bookingId);

            // Assert
            Assert.Equal(Status.Confirmed, booking.Status);
            await _bookingRepo.Received(1).UpdateAsync(booking);
            await _uow.Received(1).SaveChangesAsync();
            await _telegramNotifier.Received(1).NotifyAsync(Arg.Is<string>(msg => msg.Contains("confirmed")));
        }

        /// <summary>
        /// Тест: получение списка своих броней с пагинацией.
        /// Проверяет, что возвращается корректное количество записей и метаданные страницы.
        /// </summary>
        [Fact]
        public async Task GetMyBookingsAsync_ShouldReturnPagedUserBookings()
        {
            // Arrange
            int userId = 10;
            var bookings = new List<Booking>
            {
                new Booking { Id = 1, UserId = userId, Room = new Room { RoomName = "Room 1" } },
                new Booking { Id = 2, UserId = userId, Room = new Room { RoomName = "Room 2" } }
            };
            _bookingRepo.CountByUserIdAsync(userId).Returns(2);
            _bookingRepo.GetPagedByUserIdAsync(userId, 1, 10).Returns(bookings);

            // Act
            var result = await _service.GetMyBookingsAsync(userId, 1, 10);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
            await _bookingRepo.Received(1).GetPagedByUserIdAsync(userId, 1, 10);
            await _telegramNotifier.DidNotReceive().NotifyAsync(Arg.Any<string>());
        }

        /// <summary>
        /// Тест: получение списка броней для комнаты (только для админа).
        /// Проверяет фильтрацию и пагинацию.
        /// </summary>
        [Fact]
        public async Task GetRoomBookingsAsync_ShouldReturnPagedRoomBookings()
        {
            // Arrange
            int roomId = 1;
            var filter = new BookingFilterDto();
            var bookings = new List<Booking>
            {
                new Booking { Id = 1, RoomId = roomId, Room = new Room { RoomName = "Room 1" } },
                new Booking { Id = 2, RoomId = roomId, Room = new Room { RoomName = "Room 1" } }
            };
            _bookingRepo.GetPagedByRoomIdWithFiltersAsync(roomId, filter, 1, 10)
                        .Returns((2, bookings));

            // Act
            var result = await _service.GetRoomBookingsAsync(roomId, filter, 1, 10);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
            await _bookingRepo.Received(1).GetPagedByRoomIdWithFiltersAsync(roomId, filter, 1, 10);
            await _telegramNotifier.DidNotReceive().NotifyAsync(Arg.Any<string>());
        }

        /// <summary>
        /// Тест: получение деталей брони по ID, когда пользователь является владельцем.
        /// Проверяет, что возвращается корректный DTO.
        /// </summary>
        [Fact]
        public async Task GetBookingByIdAsync_ShouldReturnBooking_WhenUserIsOwner()
        {
            // Arrange
            int bookingId = 5, userId = 10;
            var booking = new Booking
            {
                Id = bookingId,
                UserId = userId,
                Room = new Room { RoomName = "Room 1" }
            };
            _bookingRepo.GetByIdWithIncludesAsync(bookingId).Returns(booking);

            // Act
            var result = await _service.GetBookingByIdAsync(bookingId, userId, false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(bookingId, result.Id);
            await _bookingRepo.Received(1).GetByIdWithIncludesAsync(bookingId);
            await _telegramNotifier.DidNotReceive().NotifyAsync(Arg.Any<string>());
        }
    }
}