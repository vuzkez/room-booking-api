using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoomBookingApi.Domain.Entities;

namespace RoomBookingApi.Application.Interfaces.Services
{
    public interface IJwtService
    {
        /// <summary>
        /// Генерирует JWT-токен для пользователя с указанными ролями.
        /// </summary>
        /// <param name="user">Объект пользователя.</param>
        /// <param name="roles">Список ролей пользователя (объекты RoleApplication).</param>
        /// <returns>Строка JWT-токена.</returns>
        /// <exception cref="JwtSettingsNullException">Отсутствуют обязательные настройки JWT.</exception>
        string GenerateJwtToken(UserApplication user, IList<RoleApplication> roles);
    }
}
