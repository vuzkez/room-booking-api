# Room Booking API

Простой RESTful API для управления переговорными и бронированиями — пет‑проект, который был сделан чтобы попрактиковаться в C#, ASP.NET Core и создании production‑готовых API.

Проект реализует базовые функции системы бронирования: управление комнатами, бронирования с проверкой конфликтов, простая аутентификация пользователей и документация API. Он создан в образовательных целях и демонстрирует архитектуру, тесты и конфигурацию, пригодную для деплоя.

## Содержание
- [Функции](#функции)
- [Стек технологий](#стек-технологий)
- [Обзор архитектуры](#обзор-архитектуры)
- [Быстрый старт](#быстрый-старт)
  - [Требования](#требования)
  - [Клонирование и запуск](#клонирование-и-запуск)
  - [Конфигурация окружения](#конфигурация-окружения)
- [Обзор API](#обзор-api)
  - [Основные эндпоинты](#основные-эндпоинты)
  - [Аутентификация](#аутентификация)
- [Тестирование](#тестирование)

## Функции
- CRUD для комнат (название, вместимость, локация, удобства)
- Бронирования: создание, чтение, изменение, отмена
- Предотвращение двойного бронирования при пересечениях временных интервалов (race condition)
- Базовая аутентификация пользователей (JWT) и ролевой доступ (admin/user)
- Документация API (Swagger / OpenAPI)
- Unit и интеграционные тесты для критичной логики бронирований

## Стек технологий
- Язык: C#
- Фреймворк: ASP.NET Core Web API
- Доступ к данным: Entity Framework Core
- Документация API: Swagger / Swashbuckle
- Тестирование: xUnit + NSubstitute
- Опционально: Docker для контейнеризации

## Обзор архитектуры
- Controllers: эндпоинты API (RoomsController, BookingsController, AuthController)
- Services: бизнес‑логика и проверка конфликтов бронирований
- Repositories / DbContext: репозиторный слой
- DTOs & Mappers: формирование запросов/ответов и валидация
- Middlewares: обработка ошибок (встроенный с .NET 8)

Проект организован с разделением ответственности, чтобы бизнес‑логику можно было тестировать отдельно от контроллеров и доступа к данным.

## Быстрый старт

### Требования
- .NET SDK 7.0+ (или версия, используемая в репозитории)
- SQL Server / SQLite / PostgreSQL (или in-memory DB для разработки)
- (Опционально) Docker и Docker Compose

### Клонирование и запуск
1. Клонируйте репозиторий
   git clone https://github.com/vuzkez/room-booking-api.git
   cd room-booking-api

2. Восстановите зависимости и запустите
   dotnet restore
   dotnet build
   dotnet run --project src/RoomBooking.Api

По умолчанию API запускается на http://localhost:5000 (или как настроено в launchSettings). Swagger UI будет доступен по адресу http://localhost:5000/swagger при запуске в окружении Development.

### Конфигурация окружения
Скопируйте и отредактируйте пример файла окружения или `appsettings.Development.json`:
- Connection string: строка подключения для EF Core
- JWT настройки: Secret, issuer, audience, время жизни токена
- Logging: уровень логирования и вывод

Пример (appsettings.Development.json)
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=RoomBookingDb;User Id=sa;Password=Your_password123;"
  },
  "Jwt": {
    "Secret": "replace-with-a-strong-secret",
    "Issuer": "RoomBookingApi",
    "Audience": "RoomBookingClient",
    "ExpiresMinutes": 60
  }
}

Если вы используете миграции EF Core:
- dotnet ef database update --project src/RoomBooking.Infrastructure --startup-project src/RoomBooking.Api

## Обзор API

### Основные эндпоинты (примеры)
- GET /api/rooms — список всех комнат
- GET /api/rooms/{id} — детали комнаты
- POST /api/rooms — создать комнату (только admin)
- PUT /api/rooms/{id} — обновить комнату (только admin)
- DELETE /api/rooms/{id} — удалить комнату (только admin)

- GET /api/bookings — список бронирований пользователя (admin видит все)
- POST /api/bookings — создать бронирование
- GET /api/bookings/{id} — детали бронирования
- PUT /api/bookings/{id} — изменить бронирование
- DELETE /api/bookings/{id} — отменить бронирование

### Аутентификация
- POST /api/auth/login — возвращает JWT токен
- Добавляйте заголовок `Authorization: Bearer <token>` к защищённым запросам

При создании бронирования необходимо валидировать:
- Комната существует
- Запрошенный временной интервал не пересекается с существующими подтверждёнными бронированиями для этой комнаты
- У пользователя есть права для выполнения операции

Пример curl для создания бронирования (замените токен и полезную нагрузку):
curl -X POST http://localhost:5000/api/bookings \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "roomId": 1,
    "startTime": "2026-08-10T09:00:00Z",
    "endTime": "2026-08-10T10:00:00Z",
    "title": "Sprint planning"
  }'

## Тестирование
Тесты покрывают сервисы: BookingsService, RoomsService, AuthService.
