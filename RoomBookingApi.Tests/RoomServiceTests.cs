using Xunit;
using NSubstitute;
using RoomBookingApi.Application.Services;
using RoomBookingApi.Application.Interfaces.Repositories;
using RoomBookingApi.Application.Dto;
using RoomBookingApi.Domain.Entities;
using RoomBookingApi.Application.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RoomBookingApi.Tests
{
    public class RoomServiceTests
    {
        private readonly IUnitOfWork _uow;
        private readonly IRoomRepository _roomRepo;
        private readonly ILogger<RoomService> _logger;
        private readonly RoomService _service;

        public RoomServiceTests()
        {
            // Создаём моки зависимостей
            _roomRepo = Substitute.For<IRoomRepository>();
            _logger = Substitute.For<ILogger<RoomService>>();
            _uow = Substitute.For<IUnitOfWork>();
            _uow.Rooms.Returns(_roomRepo);

            // Создаём экземпляр тестируемого сервиса
            _service = new RoomService(_logger, _uow);
        }

        /// <summary>
        /// Тест: получение комнаты по ID, когда она существует.
        /// Проверяет, что возвращается корректный DTO с правильными данными.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldReturnRoomDto_WhenRoomExists()
        {
            // Arrange
            int roomId = 1;
            var room = new Room { Id = roomId, RoomName = "Conf Room", Capacity = 10 };
            _roomRepo.GetByIdAsync(roomId).Returns(room);

            // Act
            var result = await _service.GetByIdAsync(roomId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(roomId, result.Id);
            Assert.Equal("Conf Room", result.Name);
            await _roomRepo.Received(1).GetByIdAsync(roomId);
        }

        /// <summary>
        /// Тест: получение списка комнат с пагинацией и фильтрацией.
        /// Проверяет, что возвращается PagedResult с правильным количеством записей и метаданными.
        /// </summary>
        [Fact]
        public async Task GetAllAsync_ShouldReturnPagedRooms()
        {
            // Arrange
            var filter = new RoomFilterDto { MinCapacity = 5 };
            var rooms = new List<Room>
            {
                new Room { Id = 1, RoomName = "Room A", Capacity = 10 },
                new Room { Id = 2, RoomName = "Room B", Capacity = 8 }
            };
            _roomRepo.GetPagedWithFiltersAsync(filter, 1, 10).Returns((2, rooms));

            // Act
            var result = await _service.GetAllAsync(filter, 1, 10);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
            await _roomRepo.Received(1).GetPagedWithFiltersAsync(filter, 1, 10);
        }

        /// <summary>
        /// Тест: создание новой комнаты.
        /// Проверяет, что комната добавляется в репозиторий, сохраняется и возвращается корректный DTO.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldAddRoomAndReturnDto()
        {
            // Arrange
            var request = new CreateRoomRequestDto
            {
                Name = "New Room",
                Capacity = 20,
                Location = "Floor 3",
                PricePerHour = 150m
            };
            _roomRepo.AddAsync(Arg.Any<Room>()).Returns(Task.CompletedTask);
            _uow.SaveChangesAsync().Returns(1);

            // Act
            var result = await _service.CreateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Name, result.Name);
            await _roomRepo.Received(1).AddAsync(Arg.Any<Room>());
            await _uow.Received(1).SaveChangesAsync();
        }

        /// <summary>
        /// Тест: обновление существующей комнаты.
        /// Проверяет, что данные обновляются, изменения сохраняются и возвращается обновлённый DTO.
        /// </summary>
        [Fact]
        public async Task UpdateByIdAsync_ShouldUpdateRoomAndReturnDto()
        {
            // Arrange
            int roomId = 1;
            var existing = new Room { Id = roomId, RoomName = "Old", Capacity = 5 };
            var request = new UpdateRoomRequestDto
            {
                Name = "New Name",
                Capacity = 10,
                Location = "Floor 2",
                PricePerHour = 200m,
                IsActive = true
            };
            _roomRepo.GetByIdAsync(roomId).Returns(existing);
            _uow.SaveChangesAsync().Returns(1);

            // Act
            var result = await _service.UpdateByIdAsync(roomId, request);

            // Assert
            Assert.Equal(request.Name, result.Name);
            await _roomRepo.Received(1).UpdateAsync(Arg.Any<Room>());
            await _uow.Received(1).SaveChangesAsync();
        }

        /// <summary>
        /// Тест: удаление комнаты, которая существует.
        /// Проверяет, что метод DeleteByIdAsync вызывается и изменения сохраняются.
        /// </summary>
        [Fact]
        public async Task DeleteByIdAsync_ShouldDeleteRoom_WhenRoomExists()
        {
            // Arrange
            int roomId = 1;
            _roomRepo.DeleteByIdAsync(roomId).Returns(Task.CompletedTask);
            _uow.SaveChangesAsync().Returns(1);

            // Act
            await _service.DeleteByIdAsync(roomId);

            // Assert
            await _roomRepo.Received(1).DeleteByIdAsync(roomId);
            await _uow.Received(1).SaveChangesAsync();
        }

        /// <summary>
        /// Тест: удаление комнаты, которая не существует.
        /// Проверяет, что выбрасывается исключение NotFoundException.
        /// </summary>
        [Fact]
        public async Task DeleteByIdAsync_ShouldThrowNotFoundException_WhenRoomDoesNotExist()
        {
            // Arrange
            int roomId = 999;
            _roomRepo.When(x => x.DeleteByIdAsync(roomId))
                     .Do(x => throw new NotFoundException(nameof(Room), roomId));

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteByIdAsync(roomId));
        }
    }
}