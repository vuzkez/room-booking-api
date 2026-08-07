using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoomBookingApi.Domain.Entities;

namespace RoomBookingApi.Application.Interfaces.Services
{
    public interface IEmailCodeGenerator
    {
        /// <summary>
        /// Генерирует новый код подтверждения для указанного пользователя со сроком действия 5 минут.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <returns>Сущность EmailConfirmationCode с заполненными полями (код, время истечения, признак использования).</returns>
        EmailConfirmationCode Generate(int userId);
    }
}
