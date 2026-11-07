using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoomBookingApi.Application.Interfaces.Strategies;
using RoomBookingApi.Domain.Entities;

namespace RoomBookingApi.Application.Strategies
{
    public class WeekdayPricingStrategy : IPricingStrategy
    {
        public bool AppliesTo(DateTime start, DateTime end)
        {
            return start.DayOfWeek != DayOfWeek.Saturday && start.DayOfWeek != DayOfWeek.Sunday;
        }

        public decimal CalculatePrice(Room room, DateTime start, DateTime end)
        {
            var hours = (decimal)(end - start).TotalHours;
            return room.PricePerHour * hours;
        }
    }
}
