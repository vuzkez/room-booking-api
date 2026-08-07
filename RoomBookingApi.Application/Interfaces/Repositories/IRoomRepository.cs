using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoomBookingApi.Application.Dto;
using RoomBookingApi.Domain.Entities;

namespace RoomBookingApi.Application.Interfaces.Repositories
{
    public interface IRoomRepository
    {
        /// <summary>
        /// Возвращает комнату по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор комнаты.</param>
        /// <returns>Сущность комнаты или null.</returns>
        Task<Room?> GetByIdAsync(int id);

        /// <summary>
        /// Возвращает все комнаты (без пагинации).
        /// </summary>
        /// <returns>Список всех комнат.</returns>
        Task<IEnumerable<Room>> GetAllAsync();

        /// <summary>
        /// Добавляет новую комнату в контекст (без сохранения).
        /// </summary>
        /// <param name="room">Сущность комнаты.</param>
        Task AddAsync(Room room);

        /// <summary>
        /// Обновляет существующую комнату в контексте (без сохранения).
        /// </summary>
        /// <param name="room">Сущность комнаты с обновлёнными данными.</param>
        Task UpdateAsync(Room room);

        /// <summary>
        /// Удаляет комнату по идентификатору. Выбрасывает NotFoundException, если комната не найдена.
        /// </summary>
        /// <param name="id">Идентификатор комнаты.</param>
        /// <exception cref="NotFoundException">Комната не найдена.</exception>
        Task DeleteByIdAsync(int id);

        /// <summary>
        /// Возвращает пагинированный список комнат с фильтрацией по вместимости, локации и активности.
        /// </summary>
        /// <param name="filter">Объект фильтрации (MinCapacity, Location, IsActive).</param>
        /// <param name="page">Номер страницы (начиная с 1).</param>
        /// <param name="pageSize">Размер страницы.</param>
        /// <returns>Кортеж: общее количество записей и список комнат на странице.</returns>
        Task<(int TotalCount, List<Room> Items)> GetPagedWithFiltersAsync(RoomFilterDto filter, int page, int pageSize);
    }
}
