using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoomBookingApi.Application.Interfaces.Strategies;
using RoomBookingApi.Application.Strategies;
using RoomBookingApi.Domain.Entities;

namespace RoomBookingApi.Application.Services
{
    public class PricingService
    {
        private readonly IEnumerable<IPricingStrategy> _strategies;

        public PricingService(IEnumerable<IPricingStrategy> strategies)
        {
            _strategies = strategies;
        }

        public decimal CalculatePrice(Room room,DateTime start, DateTime end)
        {
            var isWeekend = start.DayOfWeek == DayOfWeek.Saturday || start.DayOfWeek == DayOfWeek.Sunday;

            if (isWeekend)
            {
                return _strategies.OfType<WeekendPricingStrategy>().First().CalculatePrice(room,start,end);
            }

            return _strategies.OfType<WeekdayPricingStrategy>().First().CalculatePrice(room, start, end);
        }
    }
}
