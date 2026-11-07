public interface IEmailConfirmOrChange
{
    /// <summary>
    /// Подтверждает текущий email пользователя по коду (TargetEmail=null).
    /// Возвращает false, если код не найден, истёк, использован или выдан для смены email.
    /// </summary>
    Task<bool> TryConfirmEmailAsync(int userId, string code);

    /// <summary>
    /// Меняет email пользователя на адрес из кода (TargetEmail!=null).
    /// Возвращает false, если код не найден, истёк, использован или выдан для подтверждения.
    /// </summary>
    Task<bool> TryChangeEmailAsync(int userId, string code);
}