using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Options
{
    public class TelegramSettings
    {
        public string ChatId { get; set; } = string.Empty;
        public string BotToken { get; set; } = string.Empty;
    }
}
