using RoomBookingApi.Domain.Entities;

namespace RoomBookingApi.Application.Interfaces.Services
{
    public interface IEmailCodeGenerator
    {
        /// <summary>
        /// Генерирует код подтверждения на 5 минут.
        /// </summary>
        /// <param name="userId">ID пользователя.</param>
        /// <param name="targetEmail">
        /// null — подтверждение текущего email.
        /// Задан — смена email на этот адрес.
        /// </param>
        EmailConfirmationCode Generate(int userId, string? targetEmail = null);
    }
}