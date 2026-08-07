using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoomBookingApi.Application.Dto;

namespace RoomBookingApi.Application.Interfaces.Services
{
    public interface IRoomService
    {
        /// <summary>
        /// Возвращает детали комнаты по ID. Доступно всем пользователям (включая неавторизованных).
        /// </summary>
        /// <param name="id">Идентификатор комнаты.</param>
        /// <returns>DTO комнаты.</returns>
        /// <exception cref="NotFoundException">Комната не найдена.</exception>
        Task<RoomResponseDto> GetByIdAsync(int id);

        /// <summary>
        /// Возвращает список комнат с фильтрацией (минимальная вместимость, локация, активность)
        /// и пагинацией. Доступно всем пользователям (включая неавторизованных).
        /// </summary>
        /// <param name="filter">Фильтры (MinCapacity, Location, IsActive).</param>
        /// <param name="page">Номер страницы (начиная с 1).</param>
        /// <param name="pageSize">Размер страницы.</param>
        /// <returns>Постраничный результат с DTO комнат.</returns>
        Task<PagedResult<RoomResponseDto>> GetAllAsync(RoomFilterDto filter, int page, int pageSize);

        /// <summary>
        /// Создаёт новую комнату. Доступно только администратору.
        /// </summary>
        /// <param name="request">DTO с данными для создания (Name, Capacity, Location, PricePerHour).</param>
        /// <returns>DTO созданной комнаты.</returns>
        Task<RoomResponseDto> CreateAsync(CreateRoomRequestDto request);

        /// <summary>
        /// Обновляет информацию о комнате. Доступно только администратору.
        /// </summary>
        /// <param name="id">Идентификатор комнаты.</param>
        /// <param name="request">DTO с обновлёнными данными.</param>
        /// <returns>DTO обновлённой комнаты.</returns>
        /// <exception cref="NotFoundException">Комната не найдена.</exception>
        Task<RoomResponseDto> UpdateByIdAsync(int id, UpdateRoomRequestDto request);

        /// <summary>
        /// Удаляет комнату. Доступно только администратору.
        /// </summary>
        /// <param name="id">Идентификатор комнаты.</param>
        /// <exception cref="NotFoundException">Комната не найдена.</exception>
        Task DeleteByIdAsync(int id);
    }
}
