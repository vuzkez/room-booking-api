using RoomBookingApi.Application.Dto;
using RoomBookingApi.Domain.Entities;

namespace RoomBookingApi.Application.Interfaces.Repositories
{
    public interface IBookingRepository
    {
        /// <summary>Возвращает бронь по идентификатору без связанных данных.</summary>
        Task<Booking?> GetByIdAsync(int id);

        /// <summary>Добавляет новую бронь в контекст (без сохранения).</summary>
        Task AddAsync(Booking booking);

        /// <summary>Обновляет существующую бронь в контексте (без сохранения).</summary>
        Task UpdateAsync(Booking booking);

        /// <summary>
        /// Возвращает страницу броней пользователя с подгрузкой Room и User.
        /// </summary>
        Task<List<BookingResponseDto>> GetPagedByUserIdAsync(int userId, int page, int pageSize);

        /// <summary>Возвращает общее количество броней пользователя.</summary>
        Task<int> CountByUserIdAsync(int userId);

        /// <summary>
        /// Возвращает пагинированный список броней комнаты с фильтрацией
        /// по периоду, статусу и пользователю.
        /// </summary>
        /// <returns>Кортеж: общее количество и список броней на странице.</returns>
        Task<(int TotalCount, List<BookingResponseDto> Items)> GetPagedByRoomIdWithFiltersAsync(
            int roomId, BookingFilterDto filter, int page, int pageSize);

        /// <summary>
        /// Возвращает бронь по идентификатору с подгрузкой Room и User.
        /// </summary>
        Task<BookingResponseDto?> GetByIdWithIncludesAsync(int id);

        /// <summary>
        /// Проверяет, есть ли для комнаты активная бронь (Pending или Confirmed),
        /// пересекающаяся с интервалом [start, end). Отменённые брони не учитываются.
        /// </summary>
        /// <returns>true — если есть пересечение (комната занята), иначе false.</returns>
        Task<bool> HasBookingBetweenTimeAsync(int roomId, DateTime start, DateTime end);
    }
}