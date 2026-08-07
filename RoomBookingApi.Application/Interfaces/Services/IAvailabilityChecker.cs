using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoomBookingApi.Domain.Entities;

namespace RoomBookingApi.Application.Interfaces.Services
{
    public interface IAvailabilityChecker
    {
        /// <summary>
        /// Проверяет, свободна ли комната в указанный период с учётом подтверждённых броней.
        /// </summary>
        /// <param name="roomId">Идентификатор комнаты.</param>
        /// <param name="start">Начало интервала (UTC).</param>
        /// <param name="end">Конец интервала (UTC).</param>
        /// <returns>true — если комната доступна, иначе false.</returns>
        Task<bool> IsAvailableAsync(int roomId, DateTime start, DateTime end);
    }
}
