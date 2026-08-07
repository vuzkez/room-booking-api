using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RoomBookingApi.Application.Interfaces.Services;

namespace RoomBookingApi.Application.Services
{
    public class TelegramNotifier : ITelegramNotifier
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _botToken;
        private readonly string _chatId;
        private readonly ILogger<TelegramNotifier> _logger;

        public TelegramNotifier(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<TelegramNotifier> logger)
        {
            _httpClientFactory = httpClientFactory;
            _botToken = configuration["TelegramSetting:BotToken"];
            _chatId = configuration["TelegramSetting:ChatId"];
            _logger = logger;
        }

        public async Task NotifyAsync(string message)
        {
            if (string.IsNullOrEmpty(_botToken) || string.IsNullOrEmpty(_chatId))
            {
                _logger.LogWarning("Telegram settings are missing. Notification skipped.");
                return;
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";

            var payload = new
            {
                chat_id = _chatId,
                text = message,
                parse_mode = "HTML"
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Telegram notification sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Telegram notification.");
            }
        }
    }
}
