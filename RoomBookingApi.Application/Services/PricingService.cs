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
            decimal totalPrice = 0;
            var current = start;

            while (current < end)
            {
                var nextHour = current.AddHours(1);
                if (nextHour > end) 
                    nextHour = end;

                var hoursInSegment = (decimal)(nextHour - current).TotalHours;

                var strategy = _strategies.First(s => s.AppliesTo(current, nextHour));

                totalPrice += strategy.CalculatePrice(room, current, nextHour);

                current = nextHour;
            }

            return totalPrice;
        }
    }
}
