namespace RoomBookingApi.Application.Interfaces.Services
{
    public interface IAvailabilityChecker
    {
        /// <summary>
        /// Проверяет, свободна ли комната в указанный период.
        /// Учитываются активные брони - в статусах Pending и Confirmed (отменённые игнорируются).
        /// </summary>
        /// <param name="roomId">Идентификатор комнаты.</param>
        /// <param name="start">Начало интервала (UTC).</param>
        /// <param name="end">Конец интервала (UTC).</param>
        /// <returns>true — если комната доступна, иначе false.</returns>
        Task<bool> IsAvailableAsync(int roomId, DateTime start, DateTime end);
    }
}