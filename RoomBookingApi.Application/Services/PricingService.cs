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

        public decimal CalculatePrice(Room room, DateTime start, DateTime end)
        {
            decimal totalPrice = 0m;
            var current = start;

            while (current < end)
            {
                var nextFiveMinute = current.AddMinutes(5);
                if (nextFiveMinute > end)
                    nextFiveMinute = end;

                var hoursInSegment = (decimal)(nextFiveMinute - current).TotalHours;

                var strategy = _strategies.First(s => s.AppliesTo(current, nextFiveMinute));

                totalPrice += strategy.CalculatePrice(room, current, nextFiveMinute);

                current = nextFiveMinute;
            }

            return totalPrice;
        }
    }
}
