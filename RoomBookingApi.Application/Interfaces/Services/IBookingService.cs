using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoomBookingApi.Application.Dto;
using RoomBookingApi.Domain.Entities;

namespace RoomBookingApi.Application.Interfaces.Services
{
    public interface IBookingService
    {
        /// <summary>
        /// Создаёт новую бронь для указанной комнаты и пользователя. Проверяет доступность,
        /// рассчитывает цену через PricingService и сохраняет бронь со статусом Pending.
        /// </summary>
        /// <param name="roomId">Идентификатор комнаты.</param>
        /// <param name="userId">Идентификатор пользователя (владельца брони).</param>
        /// <param name="start">Начало бронирования (UTC).</param>
        /// <param name="end">Конец бронирования (UTC).</param>
        /// <returns>DTO созданной брони.</returns>
        /// <exception cref="BookingConflictException">Комната уже занята.</exception>
        /// <exception cref="NotFoundException">Комната не найдена.</exception>
        Task<BookingResponseDto> CreateBookingAsync(int roomId, int userId, DateTime start, DateTime end);

        /// <summary>
        /// Отменяет существующую бронь. Доступно только владельцу или администратору.
        /// Проверяет, что бронь ещё не началась и не отменена ранее.
        /// </summary>
        /// <param name="bookingId">Идентификатор брони.</param>
        /// <param name="userId">Идентификатор текущего пользователя.</param>
        /// <param name="isAdmin">Флаг, является ли пользователь администратором.</param>
        /// <exception cref="NotFoundException">Бронь не найдена.</exception>
        /// <exception cref="UnauthorizedAccessException">Нет прав на отмену.</exception>
        /// <exception cref="BookingConflictException">Бронь уже отменена или началась.</exception>
        Task CancelBookingAsync(int bookingId, int userId, bool isAdmin);

        /// <summary>
        /// Подтверждает бронь (меняет статус с Pending на Confirmed). Доступно только администратору.
        /// </summary>
        /// <param name="bookingId">Идентификатор брони.</param>
        /// <exception cref="NotFoundException">Бронь не найдена.</exception>
        /// <exception cref="BookingConflictException">Бронь уже подтверждена или отменена.</exception>
        Task ConfirmBookingAsync(int bookingId);

        /// <summary>
        /// Возвращает список броней текущего пользователя с пагинацией.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="page">Номер страницы (начиная с 1).</param>
        /// <param name="pageSize">Количество записей на странице.</param>
        /// <returns>Постраничный результат с DTO броней.</returns>
        Task<PagedResult<BookingResponseDto>> GetMyBookingsAsync(int userId, int page, int pageSize);

        /// <summary>
        /// Возвращает список броней для конкретной комнаты с фильтрацией (по дате, статусу, пользователю)
        /// и пагинацией. Доступно только администратору.
        /// </summary>
        /// <param name="roomId">Идентификатор комнаты.</param>
        /// <param name="filter">Фильтры (период, статус, пользователь).</param>
        /// <param name="page">Номер страницы.</param>
        /// <param name="pageSize">Размер страницы.</param>
        /// <returns>Постраничный результат с DTO броней.</returns>
        Task<PagedResult<BookingResponseDto>> GetRoomBookingsAsync(int roomId, BookingFilterDto filter, int page, int pageSize);

        /// <summary>
        /// Возвращает детали одной брони по ID с проверкой прав: владелец или администратор.
        /// </summary>
        /// <param name="bookingId">Идентификатор брони.</param>
        /// <param name="userId">Идентификатор текущего пользователя.</param>
        /// <param name="isAdmin">Флаг администратора.</param>
        /// <returns>DTO брони.</returns>
        /// <exception cref="NotFoundException">Бронь не найдена.</exception>
        /// <exception cref="UnauthorizedAccessException">Нет прав на просмотр.</exception>
        Task<BookingResponseDto> GetBookingByIdAsync(int bookingId, int userId, bool isAdmin);
    }
}
