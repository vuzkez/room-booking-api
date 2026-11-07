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
        /// <returns>Результат операции: успех/неудача с сообщением об ошибке.</returns>
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);

        /// <summary>
        /// Выполняет вход пользователя по email и паролю. Проверяет, что email подтверждён.
        /// В случае успеха генерирует JWT-токен с ролями пользователя.
        /// </summary>
        /// <param name="request">DTO с учётными данными (Email, Password).</param>
        /// <returns>Результат с токеном при успехе или сообщением об ошибке.</returns>
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request);

        /// <summary>
        /// Подтверждает email пользователя по 6-значному коду (публичный сценарий).
        /// Находит пользователя по email и делегирует подтверждение в <see cref="IEmailConfirmOrChange"/>.
        /// </summary>
        /// <param name="email">Email пользователя.</param>
        /// <param name="code">Код подтверждения (6 цифр).</param>
        /// <returns>true — если подтверждение прошло успешно, иначе false.</returns>
        Task<bool> ConfirmEmailAsync(string email, string code);

        /// <summary>
        /// Повторно отправляет код подтверждения, если пользователь не подтверждён
        /// и нет активного неиспользованного кода.
        /// </summary>
        /// <param name="email">Email пользователя.</param>
        /// <returns>true — если код отправлен, иначе false.</returns>
        Task<bool> ResendConfirmationCodeAsync(string email);

        /// <summary>
        /// Инициирует смену email: генерирует код и отправляет на новый адрес.
        /// Требует авторизации. Возвращает false, если новый email занят,
        /// совпадает с текущим, или уже есть активный код.
        /// </summary>
        Task<bool> RequestEmailChangeAsync(int userId, string newEmail);

        /// <summary>
        /// Подтверждает смену email по коду, отправленному на новый адрес.
        /// </summary>
        Task<bool> ConfirmEmailChangeAsync(int userId, string code);
    }
}