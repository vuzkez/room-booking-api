using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Interfaces.Services
{
    public interface IEmailConfirmOrChange
    {
        /// <summary>
        /// Подтверждает текущий email пользователя или меняет его на новый, если передан newEmail.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="code">Код подтверждения.</param>
        /// <param name="newEmail">Новый email (если null, то просто подтверждение).</param>
        /// <returns>true — если операция успешна, иначе false.</returns>
        Task<bool> TryConfirmOrChangeEmail(int userId, string code, string? newEmail = null);
    }
}
