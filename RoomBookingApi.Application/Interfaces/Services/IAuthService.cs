using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.Data;
using RoomBookingApi.Application.Dto;

namespace RoomBookingApi.Application.Interfaces.Services
{
    public interface IAuthService
    {
        /// <summary>
        /// Регистрирует нового пользователя, создаёт запись в базе, назначает роль "User"
        /// и отправляет код подтверждения на указанный email.
        /// </summary>
        /// <param name="request">DTO с данными для регистрации (Name, Email, Password).</param>
        /// <returns>Результат операции: успех/неудача с сообщением об ошибке или JWT-токеном.</returns>
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);

        /// <summary>
        /// Выполняет вход пользователя по email и паролю. Проверяет, что email подтверждён.
        /// В случае успеха генерирует JWT-токен с ролями пользователя.
        /// </summary>
        /// <param name="request">DTO с учётными данными (Email, Password).</param>
        /// <returns>Результат с токеном при успехе или сообщением об ошибке.</returns>
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request);

        /// <summary>
        /// Подтверждает email пользователя по 6-значному коду. Проверяет, что код существует,
        /// не истёк и не использован. Затем вызывает Identity для подтверждения email.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="code">Код подтверждения (6 цифр).</param>
        /// <returns>true — если подтверждение прошло успешно, иначе false.</returns>
        Task<bool> ConfirmEmailAsync(int userId, string code);

        /// <summary>
        /// Повторно отправляет код подтверждения, если пользователь не подтверждён
        /// и нет активного неиспользованного кода.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <returns>true — если код отправлен, иначе false.</returns>
        Task<bool> ResendConfirmationCodeAsync(int userId);
    }
}
