public interface IEmailSender
{
    /// <summary>
    /// Отправляет письмо с кодом подтверждения.
    /// </summary>
    /// <param name="toEmail">Куда слать (для смены email — новый адрес).</param>
    /// <param name="userId">ID пользователя.</param>
    /// <param name="targetEmail">
    /// null — код подтверждения текущего email.
    /// Задан — код смены email на этот адрес (сохраняется в БД).
    /// </param>
    Task SendAsync(string toEmail, int userId, string? targetEmail = null);
}