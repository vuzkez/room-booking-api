using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoomBookingApi.Application.Interfaces.Services;
using RoomBookingApi.Application.Options;

namespace RoomBookingApi.Infrastructure.Services
{
    public class TelegramNotifier : ITelegramNotifier
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TelegramSettings _settings;
        private readonly ILogger<TelegramNotifier> _logger;

        public TelegramNotifier(
            IHttpClientFactory httpClientFactory,
            IOptions<TelegramSettings> options,
            ILogger<TelegramNotifier> logger)
        {
            _httpClientFactory = httpClientFactory;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task NotifyAsync(string message)
        {
            if (string.IsNullOrEmpty(_settings.BotToken) || string.IsNullOrEmpty(_settings.ChatId))
            {
                _logger.LogWarning("Telegram settings are missing. Notification skipped.");
                return;
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"https://api.telegram.org/bot{_settings.BotToken}/sendMessage";

            var payload = new
            {
                chat_id = _settings.ChatId,
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