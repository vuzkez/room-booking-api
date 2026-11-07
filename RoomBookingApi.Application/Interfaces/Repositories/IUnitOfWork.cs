namespace RoomBookingApi.Application.Interfaces.Repositories
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        /// <summary>Репозиторий комнат.</summary>
        IRoomRepository Rooms { get; }

        /// <summary>Репозиторий бронирований.</summary>
        IBookingRepository Bookings { get; }

        /// <summary>Репозиторий кодов подтверждения email.</summary>
        IEmailCodeRepository Codes { get; }

        /// <summary>Асинхронно сохраняет все изменения в БД.</summary>
        /// <returns>Количество затронутых записей.</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Асинхронно открывает транзакцию в БД.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Асинхронно закрывает транзакцию при успешном результате транзакции.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task CommitAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Асинхронно возвращает все, как было ДО транзакции в случае неудачи.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}