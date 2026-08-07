using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoomBookingApi.Domain.Entities;

namespace RoomBookingApi.Application.Interfaces.Repositories
{
    public interface IEmailCodeRepository
    {
        /// <summary>
        /// Возвращает код подтверждения по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор кода.</param>
        /// <returns>Сущность кода или null.</returns>
        Task<EmailConfirmationCode?> GetByIdAsync(int id);

        /// <summary>
        /// Возвращает все коды подтверждения (без фильтрации).
        /// </summary>
        /// <returns>Список всех кодов.</returns>
        Task<IEnumerable<EmailConfirmationCode>> GetAllAsync();

        /// <summary>
        /// Добавляет новый код в контекст (без сохранения).
        /// </summary>
        /// <param name="emailConfirmationCode">Сущность кода.</param>
        Task AddAsync(EmailConfirmationCode emailConfirmationCode);

        /// <summary>
        /// Обновляет существующий код в контексте (без сохранения).
        /// </summary>
        /// <param name="emailConfirmationCode">Сущность кода с обновлёнными данными.</param>
        Task UpdateAsync(EmailConfirmationCode emailConfirmationCode);

        /// <summary>
        /// Удаляет код по идентификатору. Выбрасывает NotFoundException, если код не найден.
        /// </summary>
        /// <param name="id">Идентификатор кода.</param>
        /// <exception cref="NotFoundException">Код не найден.</exception>
        Task DeleteByIdAsync(int id);

        /// <summary>
        /// Возвращает код подтверждения для указанного пользователя и самого кода.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="code">Строка кода.</param>
        /// <returns>Сущность кода или null, если не найден.</returns>
        Task<EmailConfirmationCode?> GetByUserIdAndCodeAsync(int userId, string code);

        /// <summary>
        /// Проверяет, существует ли для указанного пользователя неиспользованный и неистекший код подтверждения.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <returns>true — если есть активный код, иначе false.</returns>
        Task<bool> HasUnusedValidCodeAsync(int userId);
    }
}
