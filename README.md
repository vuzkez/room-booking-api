# Room Booking API

Простой RESTful API для управления переговорными и бронированиями — пет‑проект, созданный, чтобы попрактиковать Clean Architecture, асинхронное программирование и обработку конкурентных операций в .NET.

Проект реализует базовые функции системы бронирования: управление комнатами, бронирования с проверкой конфликтов, аутентификация и авторизация.

## Содержание
- [Функции](#функции)
- [Стек технологий](#стек-технологий)
- [Архитектура](#архитектура)
- [Запуск приложения](#запуск-приложения)
- [Эндпоинты API](#эндпоинты-api)
- [Тестирование](#тестирование)

## Функции
- CRUD для комнат (название, вместимость, локация, цена за час)
- Бронирования: создание, чтение, отмена, подтверждение администратором
- Предотвращение двойного бронирования при пересечении временных интервалов
- Обработка race conditions через Optimistic Locking
- Аутентификация (JWT, ASP.NET Identity) и ролевой доступ (admin/user)
- Динамический расчёт стоимости с разными тарифами (будни/выходные)
- Проверка доступности комнат с кешированием
- Уведомления в Telegram о бронированиях
- Email с кодами подтверждения (SMTP)
- Rate Limiting (не более 5 запросов на создание брони в минуту)
- Документация API (Swagger / OpenAPI)
- Unit-тесты

## Стек технологий
- Язык: C#
- Фреймворк: ASP.NET Core Web API (.NET)
- Доступ к данным: Entity Framework Core, MySQL
- Аутентификация: JWT + ASP.NET Identity
- Документация API: Swagger / Swashbuckle
- Тестирование: xUnit + NSubstitute
- Внешние сервисы: Telegram Bot API, SMTP (например, Gmail)

## Архитектура

Приложение построено на **Clean Architecture**, решение разбито на проекты:

`RoomBookingApi.Api`: HTTP-слой: контроллеры, DTO, обработка ошибок, rate limiting, настройка JWT 
`RoomBookingApi.Application`: Бизнес-логика: сервисы, контракты (интерфейсы), стратегии расчёта цены 
 `RoomBookingApi.Domain`: Сущности и перечисления 
 `RoomBookingApi.Infrastructure`: Работа с БД: `AppDbContext`, конфигурации таблиц, миграции, репозитории, Unit of Work 
 `RoomBookingApi.Tests`: Unit-тесты 

### Используемые паттерны и подходы
- **Clean Architecture** — разделение на слои
- **Dependency Injection** — встроенный DI-контейнер .NET
- **Repository + Unit of Work** — абстракция над EF Core
- **Strategy** — разные тарифы (будни/выходные) через `IPricingStrategy`
- **Decorator** — `CachedAvailabilityChecker` оборачивает `AvailabilityChecker`
- **Optimistic Locking** — защита от конфликтов при одновременных запросах
- **JWT** — аутентификация через токены

Repository и Unit of Work я добавил для абстрагирования доступа к данным, но для этого проекта это избыточно: используется только EF Core, а `DbContext` уже реализует Unit of Work. Слой абстракции не дал практической пользы.

### Основные компоненты

**API**
- `RoomsController`, `BookingsController`, `AuthController`
- `GlobalExceptionHandler` — централизованная обработка исключений
- Rate limiter для создания бронирований
- DTO: `CreateRoomRequestDto`, `UpdateRoomRequestDto`, `CreateBookingRequestDto`, `BookingResponseDto`, `LoginRequestDto`, `RegisterRequestDto`, `AuthResponseDto`

**Application**
- `BookingService` — создание, проверка конфликтов, отмена и подтверждение бронирований
- `RoomService` — CRUD комнат, фильтрация, пагинация
- `AuthService` — регистрация, вход, подтверждение email
- `PricingService` + `IPricingStrategy` — расчёт стоимости
- `IAvailabilityChecker` — проверка доступности комнаты
- `IJwtService`, `IEmailSender`, `IEmailCodeGenerator`, `ITelegramNotifier` — контракты

**Domain**
- `Room`, `Booking` (со статусом и версией для Optimistic Locking), `UserApplication`, `RoleApplication`, `EmailCode`
- `Status` — Pending, Confirmed, Cancelled

**Интеграции**
- `TelegramNotifier` — отправка уведомлений через Telegram Bot API
- `SmtpEmailSenderService` — отправка писем через SMTP

## Запуск приложения

### Требования
- .NET SDK 8.0
- MySQL

### Клонирование и запуск
```
git clone https://github.com/vuzkez/room-booking-api.git
cd room-booking-api
dotnet run --project RoomBookingApi.Api
```

API по умолчанию запускается на `http://localhost:5000` (или как настроено в `launchSettings.json`). Swagger UI доступен по адресу `http://localhost:5000/swagger` в окружении Development.

### Конфигурация
Создайте рядом с `appsettings.template.json` файл `appsettings.Development.json` (или используйте переменные окружения / user-secrets) и заполните значения.

- `ConnectionStrings:DefaultConnection` — строка подключения к MySQL
- `Jwt` — `Key`, `Issuer`, `Audience`, `ExpiryInMinutes`
- `EmailSetting` — SMTP-сервер, порт, пароль, email и имя отправителя
- `TelegramSetting` — `ChatId` и `BotToken` бота

Пример:
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=RoomBookingDb;User=root;Password=your_password;"
  },
  "EmailSetting": {
    "Server": "smtp.gmail.com",
    "Port": "587",
    "Password": "",
    "SenderEmail": "@gmail.com",
    "SenderName": "RoomBookingApi"
  },
  "Jwt": {
    "Key": "",
    "Issuer": "RoomBookingApi",
    "Audience": "RoomBookingClient",
    "ExpiryInMinutes": 60
  },
  "TelegramSetting": {
    "ChatId": "",
    "BotToken": ""
  },
  "AllowedHosts": "*"
}
```

### Миграции
Нужен инструмент `dotnet-ef` (`dotnet tool install --global dotnet-ef`):
```
dotnet ef database update --project RoomBookingApi.Infrastructure --startup-project RoomBookingApi.Api
```

## Эндпоинты API

Для защищённых маршрутов нужен заголовок `Authorization: Bearer <token>`.

### Auth
- `POST /api/auth/register` — регистрация (публичный)
- `POST /api/auth/login` — вход, возвращает JWT (публичный)
- `POST /api/auth/confirm-email` — подтверждение email по коду (нужна авторизация)
- `POST /api/auth/resend-code` — повторная отправка кода (нужна авторизация)

### Rooms
- `GET /api/rooms` — список комнат; параметры `minCapacity`, `location`, `IsActive`, `page`, `pageSize` (публичный)
- `GET /api/rooms/{id}` — информация о комнате (публичный)
- `POST /api/rooms` — создать комнату (Admin)
- `PUT /api/rooms/{id}` — обновить комнату (Admin)
- `DELETE /api/rooms/{id}` — удалить комнату (Admin)

### Bookings
*Все эндпоинты требуют авторизации.*
- `GET /api/bookings/my` — мои бронирования с пагинацией (`page`, `pageSize`)
- `GET /api/bookings/{id}` — детали брони (владелец или Admin)
- `POST /api/bookings` — создать бронь (rate limiting)
- `DELETE /api/bookings/{id}` — отменить бронь (владелец или Admin)
- `PUT /api/bookings/{id}/confirm` — подтвердить бронь (Admin)
- `GET /api/bookings/room/{id}` — брони комнаты; фильтры `from`, `to`, `status`, `userId`, `page`, `pageSize` (Admin)

Пример создания бронирования:
```
curl -X POST http://localhost:5000/api/bookings \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "roomId": 1,
    "startTime": "2027-01-10T09:00:00Z",
    "endTime": "2027-01-10T10:00:00Z",
    "title": "Sprint planning"
  }'
```

## Тестирование

Инструменты: **xUnit** и **NSubstitute** (подмена зависимостей).

```
dotnet test RoomBookingApi.Tests
```

Тесты следуют схеме **Arrange-Act-Assert**: подготовка данных и моков → вызов метода → проверка результата и вызовов зависимостей (например, `Received(1)`).

Покрытие:
- **BookingService** — создание брони (проверка доступности и расчёт цены), отмена владельцем, подтверждение админом, списки с пагинацией и фильтрацией, уведомления в Telegram
- **AuthService** — регистрация с назначением ролей, логин и генерация JWT, подтверждение email, повторная отправка кода
- **RoomService** — CRUD, пагинация и фильтрация, обработка исключений (например, `NotFoundException` при удалении несуществующей комнаты)
