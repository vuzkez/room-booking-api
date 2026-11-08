using System.Globalization;
using NSubstitute;
using RoomBookingApi.Application.Interfaces.Strategies;
using RoomBookingApi.Application.Services;
using RoomBookingApi.Application.Strategies;
using RoomBookingApi.Domain.Entities;
using Xunit;

namespace RoomBookingApi.Tests
{
    public class PricingServiceTests
    {
        private readonly PricingService _sut = new(new IPricingStrategy[]
        {
            new WeekdayPricingStrategy(),
            new WeekendPricingStrategy()
        });

        private static DateTime Utc(string value) =>
            DateTime.SpecifyKind(DateTime.Parse(value, CultureInfo.InvariantCulture), DateTimeKind.Utc);

        private static Room RoomWithPrice(decimal pricePerHour) => new() { PricePerHour = pricePerHour };

        [Theory]
        [InlineData("2026-11-09 10:00", "2026-11-09 12:00", 200.0)] 
        [InlineData("2026-11-09 10:00", "2026-11-09 11:00", 100.0)]    
        [InlineData("2026-11-09 10:00", "2026-11-09 10:30", 50.0)]    
        [InlineData("2026-11-09 10:00", "2026-11-09 10:05", 8.33)]   
        [InlineData("2026-11-09 10:00", "2026-11-09 17:15", 725.0)]   
        [InlineData("2026-11-14 10:00", "2026-11-14 12:00", 400.0)]  
        [InlineData("2026-11-15 10:00", "2026-11-15 11:00", 200.0)]    
        [InlineData("2026-11-09 00:00", "2026-11-10 00:00", 2400.0)]   
        public void CalculatePrice_WithinOneDay_UsesTariffOfThatDay(string start, string end, double expected)
        {
            var price = _sut.CalculatePrice(RoomWithPrice(100m), Utc(start), Utc(end));

            Assert.Equal((decimal)expected, price);
        }

        [Theory]
        [InlineData("2026-11-13 23:00", "2026-11-14 01:00", 300.0)]
        [InlineData("2026-11-15 23:00", "2026-11-16 01:00", 300.0)]
        [InlineData("2026-11-14 23:00", "2026-11-15 01:00", 400.0)]  
        public void CalculatePrice_CrossingMidnightOnWholeHour_SplitsByDay(string start, string end, double expected)
        {
            var price = _sut.CalculatePrice(RoomWithPrice(100m), Utc(start), Utc(end));

            Assert.Equal((decimal)expected, price);
        }

        [Theory]
        [InlineData("2026-11-13 23:30", "2026-11-14 00:30", 100.0)] 
        [InlineData("2026-11-13 23:32", "2026-11-14 00:02", 50.0)] 
        public void CalculatePrice_CrossingMidnightOffTheHour_UsesTariffOfSegmentStart(string start, string end, double expected)
        {
            var price = _sut.CalculatePrice(RoomWithPrice(100m), Utc(start), Utc(end));

            Assert.Equal((decimal)expected, price);
        }

        [Fact]
        public void CalculatePrice_UsesRoomPrice()
        {
            var price = _sut.CalculatePrice(RoomWithPrice(33.33m), Utc("2026-11-09 10:00"), Utc("2026-11-09 13:00"));

            Assert.Equal(99.99m, price);
        }

        [Fact]
        public void CalculatePrice_WhenEndNotAfterStart_ReturnsZero()
        {
            var moment = Utc("2026-11-09 10:00");

            Assert.Equal(0m, _sut.CalculatePrice(RoomWithPrice(100m), moment, moment));
            Assert.Equal(0m, _sut.CalculatePrice(RoomWithPrice(100m), moment, moment.AddHours(-1)));
        }

        [Fact]
        public void CalculatePrice_WhenNoStrategyApplies_Throws()
        {
            var sut = new PricingService(Array.Empty<IPricingStrategy>());

            Assert.Throws<InvalidOperationException>(() =>
                sut.CalculatePrice(RoomWithPrice(100m), Utc("2026-11-09 10:00"), Utc("2026-11-09 11:00")));
        }

        [Fact]
        public void CalculatePrice_UsesOnlyStrategyThatApplies()
        {
            var notApplicable = Substitute.For<IPricingStrategy>();
            notApplicable.AppliesTo(Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(false);

            var applicable = Substitute.For<IPricingStrategy>();
            applicable.AppliesTo(Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(true);
            applicable.CalculatePrice(Arg.Any<Room>(), Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(42m);

            var sut = new PricingService(new[] { notApplicable, applicable });

            var price = sut.CalculatePrice(RoomWithPrice(100m), Utc("2026-11-09 10:00"), Utc("2026-11-09 11:00"));

            Assert.Equal(42m, price);
            notApplicable.DidNotReceive().CalculatePrice(Arg.Any<Room>(), Arg.Any<DateTime>(), Arg.Any<DateTime>());
        }
    }
}