# Room Booking API

REST API для бронирования переговорных комнат на ASP.NET Core. Учебный проект, в котором я отрабатывал чистую архитектуру, транзакции и защиту от гонок при бронировании.

## Возможности

* Комнаты: CRUD, фильтры (вместимость, локация, активность), пагинация, мягкое удаление (`IsActive = false`)
* Бронирования: создание, просмотр, отмена, подтверждение администратором
* Защита от двойного бронирования при параллельных запросах (см. ниже)
* Расчёт стоимости по тарифам будни / выходные
* Регистрация с подтверждением email по коду, смена email, JWT-аутентификация, роли `Admin` / `User`
* Блокировка аккаунта после неудачных попыток входа, rate limiting
* Уведомления в Telegram (best-effort), письма через SMTP
* Swagger, health check, глобальная обработка ошибок (`ProblemDetails`)

## Стек

C#, ASP.NET Core (net8.0), Entity Framework Core + Pomelo (MySQL 8), ASP.NET Core Identity, JWT Bearer, MailKit, Swashbuckle, xUnit, NSubstitute.

## Архитектура

```
Api  ->  Infrastructure  ->  Application  ->  Domain
```

|Проект|Что внутри|
|-|-|
|`RoomBookingApi.Domain`|Сущности (`Room`, `Booking`, `UserApplication`, `EmailConfirmationCode`), `Status`|
|`RoomBookingApi.Application`|Сервисы, DTO, интерфейсы (репозитории, `IUnitOfWork`, `IEmailSender`, `IJwtService`...), стратегии расчёта цены, Options|
|`RoomBookingApi.Infrastructure`|`AppDbContext`, конфигурации и миграции, репозитории, Unit of Work, SMTP, Telegram, JWT|
|`RoomBookingApi.Api`|Контроллеры, `Program.cs`, rate limiting, обработчик ошибок|
|`RoomBookingApi.Tests`|Unit тесты|

Применяемые подходы: Dependency Injection, Repository + Unit of Work, Strategy (тарифы), Options pattern с валидацией при старте (`ValidateOnStart`).

**Зачем Repository + Unit of Work при EF Core.** Они изолируют Application от деталей хранения, позволяют подменять доступ к данным в unit-тестах, а Unit of Work даёт сервисам транзакцию (`BeginTransactionAsync` / `CommitAsync` / `RollbackAsync`), не раскрывая типы EF Core. Цена: лишний слой. Репозитории возвращают проекции в DTO (`Select`) с `AsNoTracking` для чтения.

## Защита от двойного бронирования

Создание брони выполняется в одной транзакции:

1. Блокируем строку комнаты: `SELECT \\\\\\\\\\\\\\\* FROM Rooms WHERE Id = @id FOR UPDATE`. Запросы на одну комнату выстраиваются в очередь.
2. Проверяем пересечение интервалов среди броней в статусах `Pending` и `Confirmed`: `existing.Start < newEnd AND existing.End > newStart`. Бронь, которая заканчивается ровно в момент начала другой, не конфликтует.
3. Сохраняем бронь и коммитим транзакцию.
4. Только после коммита отправляем уведомление в Telegram.

Почему это корректно при уровне изоляции `REPEATABLE READ` в MySQL: блокирующее чтение (`FOR UPDATE`) видит последнюю закоммиченную версию, а снимок для следующего обычного `SELECT` создаётся уже после получения блокировки, то есть после коммита предыдущей транзакции. Поэтому в транзакции до блокировки нет обычных чтений.

`Booking.Version` используется как concurrency token для смены статуса (подтверждение / отмена): параллельные изменения одной брони приводят к `DbUpdateConcurrencyException` и ответу `409`.

## Расчёт стоимости

Интервал режется на часовые сегменты от времени начала. Для каждого сегмента выбирается `IPricingStrategy` по `AppliesTo(...)`: будни по базовой цене, суббота и воскресенье с коэффициентом ×2. Итог округляется до копеек один раз в конце. Новый тариф добавляется отдельным классом без изменения `PricingService`.

## Правила бронирования

* `start < end`, начало не в прошлом, длительность не более 24 часов
* Комната должна существовать и быть активной
* Время принимается как `DateTimeOffset` (ISO 8601) и хранится в UTC
* Отменить можно только бронь, которая ещё не началась; отменить может владелец или администратор

## Безопасность

* JWT (HS256), время жизни настраивается, пароли хешируются Identity
* Пароль от 8 символов, уникальный email, обязательное подтверждение email
* Lockout: 5 неудачных попыток входа, блокировка на 10 минут
* Подтверждение email: 6-значный код, действует 5 минут, одноразовый; смена email подтверждается кодом, отправленным на новый адрес
* Rate limiting (HTTP 429):

|Политика|Лимит|Ключ|
|-|-|-|
|Создание брони|5 / 1 мин|пользователь|
|Вход|5 / 3 мин|IP|
|Регистрация|5 / 2 мин|IP|
|Подтверждение кода|4 / 1 мин|IP|
|Повторная отправка кода|2 / 3 мин|IP|
|Смена email|3 / 10 мин|пользователь|

* Ошибки 5xx не раскрывают внутренние детали клиенту; уровень логирования `System.Net.Http.HttpClient` снижен, чтобы токен бота не попадал в логи
* Секреты не хранятся в репозитории (`appsettings.json` и `appsettings.\\\\\\\\\\\\\\\*.json` в `.gitignore`)

## Запуск

### Требования

* .NET SDK 9.0.200+ (решение в формате `.slnx`) и среда выполнения .NET 8
* MySQL 8 (например, в Docker)

### 1\. Конфигурация

Скопируйте `RoomBookingApi.Api/appsettings.template.json` в `RoomBookingApi.Api/appsettings.Development.json` и заполните значения либо задайте их через user-secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=RoomBookingDb;User=root;Password=CHANGE\\\\\\\\\\\\\\\_ME;" --project RoomBookingApi.Api
dotnet user-secrets set "Jwt:Key" "<случайная строка от 32 символов>" --project RoomBookingApi.Api
dotnet user-secrets set "SeedData:AdminPassword" "<пароль админа>" --project RoomBookingApi.Api
```

Обязательные секции: `ConnectionStrings`, `Jwt`, `EmailSetting`, `SeedData` (проверяются при старте). `TelegramSetting` необязательна: без неё уведомления пропускаются.

### 3\. Миграции и запуск

```bash
dotnet tool install --global dotnet-ef
dotnet ef database update --project RoomBookingApi.Infrastructure --startup-project RoomBookingApi.Api
dotnet run --project RoomBookingApi.Api
```

Swagger: `http://localhost:5054/swagger` (только в Development). Health check: `GET /health`.
В окружении Development при старте создаются роли и администратор из секции `SeedData`.

## API

Защищённым маршрутам нужен заголовок `Authorization: Bearer <token>`. Ошибки в формате `ProblemDetails`: 400 (валидация), 401, 403, 404, 409 (конфликт брони), 429.

### Auth

|Метод|Путь|Доступ|Описание|
|-|-|-|-|
|POST|`/api/auth/register`|все|Регистрация, код уходит на почту|
|POST|`/api/auth/confirm-email`|все|`{ email, code }`|
|POST|`/api/auth/resend-code`|все|Повторная отправка кода|
|POST|`/api/auth/login`|все|Возвращает JWT|
|POST|`/api/auth/request-email-change`|пользователь|Код уходит на новый адрес|
|POST|`/api/auth/confirm-email-change`|пользователь|Подтверждение нового email|

### Rooms

|Метод|Путь|Доступ|Описание|
|-|-|-|-|
|GET|`/api/rooms`|все|Список: `minCapacity`, `location`, `isActive`, `page`, `pageSize`|
|GET|`/api/rooms/{id}`|все|Комната по id|
|POST|`/api/rooms`|Admin|Создать|
|PUT|`/api/rooms/{id}`|Admin|Изменить|
|DELETE|`/api/rooms/{id}`|Admin|Деактивировать|

### Bookings

|Метод|Путь|Доступ|Описание|
|-|-|-|-|
|POST|`/api/bookings`|пользователь|`{ roomId, startTime, endTime }`|
|GET|`/api/bookings/my`|пользователь|Мои брони (пагинация)|
|GET|`/api/bookings/{id}`|владелец / Admin|Бронь по id|
|DELETE|`/api/bookings/{id}`|владелец / Admin|Отмена|
|PUT|`/api/bookings/{id}/confirm`|Admin|Подтверждение|
|GET|`/api/bookings/room/{id}`|Admin|Брони комнаты: `from`, `to`, `status`, `userId`|

Пример:

```bash
curl -X POST http://localhost:5054/api/bookings \\\\\\\\\\\\\\\\
  -H "Authorization: Bearer <token>" -H "Content-Type: application/json" \\\\\\\\\\\\\\\\
  -d '{ "roomId": 1, "startTime": "2026-12-01T10:00:00Z", "endTime": "2026-12-01T12:00:00Z" }'
```

## Тестирование

```bash
dotnet test
```

Модульные тесты (xUnit + NSubstitute), зависимости подменяются заглушками:

* `BookingService`: создание, отмена, подтверждение, откат транзакции, права доступа
* `AuthService`: регистрация, вход, блокировка, подтверждение и смена email
* `RoomService`: CRUD, мягкое удаление
* `PricingService`: будни, выходные, переход через полночь, округление, известное ограничение по тарифу

Защита от параллельного бронирования (транзакция + `SELECT ... FOR UPDATE`) модульными тестами не проверяется: для этого нужна настоящая БД. Интеграционных тестов в проекте пока нет.

