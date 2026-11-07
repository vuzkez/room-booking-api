using RoomBookingApi.Application.Dto;
using RoomBookingApi.Domain.Entities;

namespace RoomBookingApi.Application.Interfaces.Repositories
{
    public interface IRoomRepository
    {
        /// <summary>Возвращает комнату по идентификатору.</summary>
        Task<Room?> GetByIdAsync(int id);

        /// <summary>Добавляет новую комнату в контекст (без сохранения).</summary>
        Task AddAsync(Room room);

        /// <summary>Обновляет существующую комнату в контексте (без сохранения).</summary>
        Task UpdateAsync(Room room);

        /// <summary>
        /// Возвращает пагинированный список комнат с фильтрацией
        /// по вместимости, локации и активности.
        /// </summary>
        Task<(int TotalCount, List<Room> Items)> GetPagedWithFiltersAsync(
            RoomFilterDto filter, int page, int pageSize);

        /// <summary>
        /// Возвращает комнату по идентификатору с пессимистичной блокировкой строки
        /// (SELECT ... FOR UPDATE). Используется внутри транзакции для сериализации
        /// создания броней по одной комнате.
        /// </summary>
        /// <remarks>Требует активной транзакции в UnitOfWork. Lock снимается при COMMIT/ROLLBACK.</remarks>
        Task<Room?> GetByIdForUpdateAsync(int id);
    }
}