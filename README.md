# Room Booking API

Простой RESTful API для управления переговорными и бронированиями — пет‑проект, который был сделан чтобы попрактиковать Clean Architecture, асинхронное программирование и обработку конкурентных операций в .NET.

Проект реализует базовые функции системы бронирования: управление комнатами, бронирования с проверкой конфликтов, аутентификация и авторизация.

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
- CRUD для комнат (название, вместимость, локация, цена за час)
- Бронирования: создание, чтение, изменение, отмена
- Предотвращение двойного бронирования при пересечениях временных интервалов
- Обработка race conditions через Optimistic Locking
- Базовая аутентификация пользователей (JWT) и ролевой доступ (admin/user)
- Динамический расчет стоимости с применением разных тарифов (будни/выходные)
- Проверка доступности комнат с кешированием
- Отправка уведомлений в Telegram при новых бронированиях
- Email уведомления с кодами подтверждения (SMTP)
- Rate Limiting для защиты от перегрузок
- Документация API (Swagger / OpenAPI)
- Unit тестирование

## Стек технологий
- Язык: C#
- Фреймворк: ASP.NET Core Web API (.NET)
- Доступ к данным: Entity Framework Core
- Аутентификация: JWT + ASP.NET Identity
- Документация API: Swagger / Swashbuckle
- Тестирование: xUnit + NSubstitute (мокирование зависимостей)
- Внешние сервисы: Telegram Bot API, SMTP (Gmail)

## Обзор архитектуры

Приложение построено на базе **Clean Architecture** с четкой структурой слоев. Каждый слой имеет свою зону ответственности и минимальные зависимости между ними. Используются паттерны и принципы проектирования для обеспечения гибкости и тестируемости кода.

### Используемые паттерны проектирования

1. **Clean Architecture** — разделение на слои с чёткими границами зависимостей
2. **Repository Pattern** — абстракция доступа к данным через интерфейсы
3. **Unit of Work** — управление транзакциями и координация репозиториев
4. **Dependency Injection** — внедрение зависимостей через конструкторы (встроенный DI контейнер .NET)
5. **Strategy Pattern** — разные стратегии расчета цены (будни vs выходные) через `IPricingStrategy`
6. **Decorator Pattern** — `CachedAvailabilityChecker` оборачивает базовый `AvailabilityChecker`
7. **JWT** — безопасная аутентификация через токены
8. **Optimistic Locking** — предотвращение race conditions через версии записей

### Слои архитектуры

#### **1. API Layer (Presentation)**
Внешняя граница приложения, ответственная за взаимодействие с клиентами.

- **Controllers** — HTTP эндпоинты для работы с ресурсами
  - `RoomsController` — управление комнатами (список, создание, обновление, удаление)
  - `BookingsController` — управление бронированиями (создание, чтение, отмена)
  - `AuthController` — аутентификация (регистрация, вход, подтверждение email)
  - Валидация входных данных и формирование ответов

- **Middlewares & Exception Handling** — обработка глобальных операций
  - `GlobalExceptionHandler` — централизованная обработка исключений
  - `RateLimiter` — ограничение частоты запросов (max 5 запросов на создание брони в минуту)
  - JWT Bearer аутентификация на каждый защищённый запрос

- **DTOs (Data Transfer Objects)** — контракты обмена данными
  - `CreateRoomRequestDto`, `UpdateRoomRequestDto` — для операций с комнатами
  - `CreateBookingRequestDto`, `BookingResponseDto` — для операций с бронированиями
  - `LoginRequestDto`, `RegisterRequestDto`, `AuthResponseDto` — для аутентификации

#### **2. Application Layer**
Бизнес-логика приложения, независимая от деталей реализации.

- **Services** — инкапсуляция бизнес-правил
  - `BookingService` — логика создания, валидации и отмены бронирований
    - Проверка пересечений временных интервалов через `IAvailabilityChecker`
    - Обработка race conditions через Optimistic Locking (версионирование)
    - Отправка уведомлений в Telegram при создании/отмене бронирования
  - `RoomService` — операции с комнатами (CRUD) и фильтрация
  - `AuthService` — проверка credentials, регистрация, управление сессией
  - `PricingService` — расчет стоимости с применением Strategy паттерна
  - `TelegramNotifier` — отправка сообщений через Telegram Bot API
  - `SmtpEmailSenderService` — отправка email через SMTP (подтверждение email)

- **Interfaces** — контракты для инверсии зависимостей
  - `IBookingService`, `IRoomService`, `IAuthService` — сервисные контракты
  - `IJwtService` — интерфейс для работы с JWT токенами
  - `IEmailSender`, `IEmailCodeGenerator` — отправка писем и генерация кодов
  - `ITelegramNotifier` — отправка уведомлений в Telegram
  - `IAvailabilityChecker` — проверка доступности комнат
  - `IPricingStrategy` — интерфейс для Strategy паттерна (разные тарифы)

#### **3. Domain Layer**
Ядро приложения, содержит бизнес-сущности и правила.

- **Entities** — основные модели домена
  - `Room` — переговорная (ID, название, вместимость, локация, цена за час)
  - `Booking` — бронирование (ID, комната, пользователь, время, статус, версия для Optimistic Locking)
  - `RoleApplication` - роль пользователя (наследует от IdentityRole)
  - `UserApplication` — пользователь (наследует от IdentityUser, email, роли)
  - `EmailCode` — коды для подтверждения email с таймаутом

- **Enums** — перечисления
  - `Status` — статусы бронирований (Pending, Confirmed, Cancelled)

#### **4. Infrastructure Layer**
Техническая реализация, взаимодействие с внешними системами.

- **Unit of Work Pattern**
  - `IUnitOfWork` интерфейс — координирует работу всех репозиториев
  - `UnitOfWork` реализация — управляет транзакциями и состоянием контекста БД
  - Гарантирует атомарность операций при сохранении изменений

- **Repositories** — доступ к данным
  - `IBookingRepository`, `IRoomRepository`, `IEmailCodeRepository` — интерфейсы
  - Конкретные реализации для каждого репозитория

- **Database**
  - `AppDbContext` — конфигурация модели данных (конфигурации таблиц вынесены)
  - Маппинг сущностей на таблицы БД
  - Миграции для версионирования схемы

## Быстрый старт

### Требования
- .NET SDK 8.0
- SQL Server / SQLite / PostgreSQL / MySQL для базы данных
- (Опционально) Docker и Docker Compose

### Клонирование и запуск
1. Клонируйте репозиторий
```
   git clone https://github.com/vuzkez/room-booking-api.git
   cd room-booking-api
```

2. Восстановите зависимости и запустите
```
   dotnet restore
   dotnet build
   dotnet run --project src/RoomBooking.Api
```

По умолчанию API запускается на http://localhost:5000 (или как настроено в launchSettings). Swagger UI будет доступен по адресу http://localhost:5000/swagger при запуске в окружении Development.

### Конфигурация окружения
Скопируйте и отредактируйте пример файла окружения или `appsettings.template.json`:
- Connection string: строка подключения для EF Core
- JWT настройки: Secret, issuer, audience, время жизни токена
- Logging: уровень логирования и вывод
- EmailSetting: настройки для отправки Email сообщений (сервер,порт,пароль,имя отправителя и email отправителя)
- TelegramSetting: настройка отправки сообщений через Бота (айди чата и токен бота)

Пример (appsettings.template.json)
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": ""
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

Если вы используете миграции EF Core:
```
dotnet ef database update --project src/RoomBooking.Infrastructure --startup-project src/RoomBooking.Api
```

## Основные эндпоинты API

Для доступа к защищенным маршрутам необходимо передавать заголовок:  
`Authorization: Bearer <token>`

### Auth
- `POST /api/auth/register` — Регистрация нового пользователя (Публичный)
- `POST /api/auth/login` — Вход в аккаунт, возврат JWT-токена (Публичный)
- `POST /api/auth/confirm-email` — Подтверждение email по коду (Требуется авторизация)
- `POST /api/auth/resend-code` — Повторная отправка кода подтверждения (Требуется авторизация)

### Rooms
- `GET /api/rooms` — Список комнат. Поддерживает query-параметры: `minCapacity`, `location`, `IsActive`, `page`, `pageSize` (Публичный)
- `GET /api/rooms/{id}` — Детальная информация о комнате (Публичный)
- `POST /api/rooms` — Создание новой комнаты (Только Admin)
- `PUT /api/rooms/{id}` — Обновление данных комнаты (Только Admin)
- `DELETE /api/rooms/{id}` — Удаление комнаты (Только Admin)

### Bookings
*Все эндпоинты этого раздела требуют авторизации.*
- `GET /api/bookings/my` — Список бронирований текущего пользователя с пагинацией (`page`, `pageSize`)
- `GET /api/bookings/{id}` — Детали конкретного бронирования (Доступно владельцу или Admin)
- `POST /api/bookings` — Создание нового бронирования (Применяется Rate Limiting)
- `DELETE /api/bookings/{id}` — Отмена бронирования (Доступно владельцу или Admin)
- `PUT /api/bookings/{id}/confirm` — Подтверждение бронирования (Только Admin)
- `GET /api/bookings/room/{id}` — Список бронирований конкретной комнаты. Поддерживает фильтрацию: `from`, `to`, `status`, `userId`, `page`, `pageSize` (Только Admin)

Пример curl для создания бронирования (замените токен и остальное под ваш вариант):
```
curl -X POST http://localhost:5000/api/bookings \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "roomId": 1,
    "startTime": "2026-08-10T09:00:00Z",
    "endTime": "2026-08-10T10:00:00Z",
    "title": "Sprint planning"
  }'
```

## Тестирование
Тесты покрывают сервисы: BookingsService, RoomsService, AuthService.
