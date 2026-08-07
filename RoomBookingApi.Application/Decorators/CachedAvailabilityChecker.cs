using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using RoomBookingApi.Application.Interfaces.Services;

namespace RoomBookingApi.Application.Decorators
{
    public class CachedAvailabilityChecker : IAvailabilityChecker
    {
        private readonly IAvailabilityChecker _availabilityChecker;
        private readonly IMemoryCache _memoryCache;
        public CachedAvailabilityChecker(IAvailabilityChecker availabilityChecker,IMemoryCache memoryCache)
        {
            _availabilityChecker = availabilityChecker;
            _memoryCache = memoryCache;
        }
        public async Task<bool> IsAvailableAsync(int roomId, DateTime start, DateTime end)
        {
            var key = new ReportCacheKey(roomId, start, end);

            if (_memoryCache.TryGetValue(key,out bool result))
            {
                return result;
            }

            var isAvailable = await _availabilityChecker.IsAvailableAsync(roomId, start, end);
            _memoryCache.Set(key,isAvailable,TimeSpan.FromMinutes(5));

            return isAvailable;
        }
    }

    public record class ReportCacheKey(int Id, DateTime From, DateTime To);
}
