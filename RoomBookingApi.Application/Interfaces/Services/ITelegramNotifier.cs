using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Interfaces.Services
{
    public interface ITelegramNotifier
    {
        /// <summary>
        /// Отправляет текстовое сообщение в указанный чат.
        /// </summary>
        /// <param name="message">Текст сообщения.</param>
        Task NotifyAsync(string message);
    }
}
