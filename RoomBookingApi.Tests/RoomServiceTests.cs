using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RoomBookingApi.Application.Dto;
using RoomBookingApi.Application.Exceptions;
using RoomBookingApi.Application.Interfaces.Repositories;
using RoomBookingApi.Application.Services;
using RoomBookingApi.Domain.Entities;
using Xunit;

namespace RoomBookingApi.Tests
{
    public class RoomServiceTests
    {
        private readonly IUnitOfWork _uow;
        private readonly IRoomRepository _roomRepo;
        private readonly ILogger<RoomService> _logger;
        private readonly RoomService _sut;

        public RoomServiceTests()
        {
            _uow = Substitute.For<IUnitOfWork>();
            _roomRepo = Substitute.For<IRoomRepository>();
            _logger = Substitute.For<ILogger<RoomService>>();
            _uow.Rooms.Returns(_roomRepo);

            _sut = new RoomService(_logger, _uow);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnDto_WhenRoomExists()
        {
            var room = new Room
            {
                Id = 1,
                RoomName = "Conf",
                Capacity = 10,
                Location = "Floor 1",
                PricePerHour = 100m,
                IsActive = true
            };
            _roomRepo.GetByIdAsync(1).Returns(room);

            var result = await _sut.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Conf", result.Name);
            Assert.Equal(10, result.Capacity);
            Assert.Equal("Floor 1", result.Location);
            Assert.Equal(100m, result.PricePerHour);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowNotFound_WhenMissing()
        {
            _roomRepo.GetByIdAsync(999).Returns((Room?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(999));
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPagedRooms()
        {
            var filter = new RoomFilterDto { MinCapacity = 5 };
            var rooms = new List<Room>
            {
                new Room { Id = 1, RoomName = "A", Capacity = 10 },
                new Room { Id = 2, RoomName = "B", Capacity = 8 }
            };
            _roomRepo.GetPagedWithFiltersAsync(filter, 1, 10).Returns((2, rooms));

            var result = await _sut.GetAllAsync(filter, 1, 10);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(1, result.TotalPages);
            await _roomRepo.Received(1).GetPagedWithFiltersAsync(filter, 1, 10);
        }

        [Fact]
        public async Task CreateAsync_ShouldAddAndSaveRoom()
        {
            var request = new CreateRoomRequestDto
            {
                Name = "New Room",
                Capacity = 20,
                Location = "Floor 3",
                PricePerHour = 150m
            };
            _roomRepo.AddAsync(Arg.Any<Room>()).Returns(Task.CompletedTask);
            _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            var result = await _sut.CreateAsync(request);

            Assert.NotNull(result);
            Assert.Equal("New Room", result.Name);
            Assert.Equal(20, result.Capacity);
            Assert.Equal("Floor 3", result.Location);
            Assert.Equal(150m, result.PricePerHour);
            Assert.True(result.IsActive);

            await _roomRepo.Received(1).AddAsync(Arg.Is<Room>(r =>
                r.RoomName == "New Room" &&
                r.Capacity == 20 &&
                r.Location == "Floor 3" &&
                r.PricePerHour == 150m &&
                r.IsActive));
            await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateByIdAsync_ShouldModifyAndReturnDto()
        {
            var existing = new Room
            {
                Id = 1,
                RoomName = "Old",
                Capacity = 5,
                Location = "F1",
                PricePerHour = 100m,
                IsActive = true
            };
            _roomRepo.GetByIdAsync(1).Returns(existing);
            _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            var request = new UpdateRoomRequestDto
            {
                Name = "New",
                Capacity = 10,
                Location = "F2",
                PricePerHour = 200m,
                IsActive = false
            };

            var result = await _sut.UpdateByIdAsync(1, request);

            Assert.Equal("New", result.Name);
            Assert.Equal(10, result.Capacity);
            Assert.Equal("F2", result.Location);
            Assert.Equal(200m, result.PricePerHour);
            Assert.False(result.IsActive);

            await _roomRepo.Received(1).UpdateAsync(existing);
            await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateByIdAsync_ShouldThrowNotFound_WhenMissing()
        {
            _roomRepo.GetByIdAsync(999).Returns((Room?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.UpdateByIdAsync(999, new UpdateRoomRequestDto
                {
                    Name = "X",
                    Capacity = 1,
                    Location = "L",
                    PricePerHour = 100m,
                    IsActive = true
                }));
        }

        [Fact]
        public async Task DeleteByIdAsync_ShouldSoftDeleteAndSave()
        {
            var room = new Room { Id = 1, RoomName = "A", IsActive = true };
            _roomRepo.GetByIdAsync(1).Returns(room);
            _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            await _sut.DeleteByIdAsync(1);

            Assert.False(room.IsActive);
            await _roomRepo.Received(1).UpdateAsync(room);
            await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteByIdAsync_ShouldThrowNotFound_WhenMissing()
        {
            _roomRepo.GetByIdAsync(999).Returns((Room?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteByIdAsync(999));
        }
    }
}