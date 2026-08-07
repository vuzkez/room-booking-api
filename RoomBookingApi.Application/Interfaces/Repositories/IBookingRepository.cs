using RoomBookingApi.Application.Dto;
using RoomBookingApi.Domain.Entities;

namespace RoomBookingApi.Application.Interfaces.Repositories;
public interface IBookingRepository
{
    /// <summary>
    /// Возвращает бронь по идентификатору без подгрузки связанных данных.
    /// </summary>
    /// <param name="id">Идентификатор брони.</param>
    /// <returns>Сущность брони или null, если не найдена.</returns>
    Task<Booking?> GetByIdAsync(int id);

    /// <summary>
    /// Возвращает все бронирования (без пагинации). Используется осторожно, предпочтительнее использовать пагинированные методы.
    /// </summary>
    /// <returns>Список всех броней.</returns>
    Task<IEnumerable<Booking>> GetAllAsync();

    /// <summary>
    /// Добавляет новую бронь в контекст (без сохранения).
    /// </summary>
    /// <param name="booking">Сущность брони.</param>
    Task AddAsync(Booking booking);

    /// <summary>
    /// Обновляет существующую бронь в контексте (без сохранения).
    /// </summary>
    /// <param name="booking">Сущность брони с изменёнными данными.</param>
    Task UpdateAsync(Booking booking);

    /// <summary>
    /// Удаляет бронь по идентификатору. Выбрасывает NotFoundException, если запись не найдена.
    /// </summary>
    /// <param name="id">Идентификатор брони.</param>
    /// <exception cref="NotFoundException">Бронь не найдена.</exception>
    Task DeleteByIdAsync(int id);

    /// <summary>
    /// Возвращает все брони конкретного пользователя (без пагинации).
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <returns>Список броней пользователя.</returns>
    Task<List<Booking>> GetAllByUserId(int userId);

    /// <summary>
    /// Возвращает страницу броней с сортировкой по времени начала.
    /// </summary>
    /// <param name="page">Номер страницы (начиная с 1).</param>
    /// <param name="pageSize">Количество записей на странице.</param>
    /// <returns>Список броней для указанной страницы.</returns>
    Task<List<Booking>> GetPagedAsync(int page, int pageSize);

    /// <summary>
    /// Возвращает страницу броней конкретного пользователя с подгрузкой связанных сущностей (Room, User).
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="page">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <returns>Список броней пользователя на указанной странице.</returns>
    Task<List<Booking>> GetPagedByUserIdAsync(int userId, int page, int pageSize);

    /// <summary>
    /// Возвращает общее количество броней пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <returns>Количество броней.</returns>
    Task<int> CountByUserIdAsync(int userId);

    /// <summary>
    /// Возвращает пагинированный список броней для указанной комнаты с возможностью фильтрации по дате, статусу и пользователю.
    /// </summary>
    /// <param name="roomId">Идентификатор комнаты.</param>
    /// <param name="filter">Объект фильтрации (период, статус, пользователь).</param>
    /// <param name="page">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <returns>Кортеж: общее количество записей и список броней на странице.</returns>
    Task<(int TotalCount, List<Booking> Items)> GetPagedByRoomIdWithFiltersAsync(int roomId, BookingFilterDto filter, int page, int pageSize);

    /// <summary>
    /// Возвращает бронь по идентификатору с подгрузкой связанных сущностей (Room, User).
    /// </summary>
    /// <param name="id">Идентификатор брони.</param>
    /// <returns>Сущность брони с заполненными навигационными свойствами или null.</returns>
    Task<Booking?> GetByIdWithIncludesAsync(int id);

    /// <summary>
    /// Проверяет, существует ли хотя бы одно подтверждённое бронирование для указанной комнаты,
    /// которое пересекается с заданным временным интервалом.
    /// </summary>
    /// <param name="roomId">ID комнаты.</param>
    /// <param name="start">Начало интервала (UTC).</param>
    /// <param name="end">Конец интервала (UTC).</param>
    /// <returns>true — если есть пересечение (комната занята), иначе false.</returns>
    Task<bool> HasBookingBetweenTimeAsync(int roomId, DateTime start, DateTime end);
}