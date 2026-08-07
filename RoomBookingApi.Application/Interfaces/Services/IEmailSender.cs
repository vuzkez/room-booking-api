using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Interfaces.Services
{
    public interface IEmailSender
    {
        /// <summary>
        /// Отправляет письмо с кодом подтверждения на указанный email.
        /// </summary>
        /// <param name="toEmail">Адрес получателя.</param>
        /// <param name="userId">Идентификатор пользователя (используется для генерации кода).</param>
        /// <exception cref="EmailSettingsNullException">Отсутствуют настройки SMTP.</exception>
        /// <exception cref="FileNotFoundException">Не найден шаблон письма.</exception>
        /// <exception cref="SmtpCommandException">Ошибка при отправке (невалидный адрес, проблемы с сервером).</exception>
        Task SendAsync(string toEmail, int userId);
    }
}
