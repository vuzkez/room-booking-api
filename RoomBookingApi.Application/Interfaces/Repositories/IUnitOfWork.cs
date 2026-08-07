using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Interfaces.Repositories
{
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Репозиторий для работы с комнатами.
        /// </summary>
        IRoomRepository Rooms { get; }

        /// <summary>
        /// Репозиторий для работы с бронированиями.
        /// </summary>
        IBookingRepository Bookings { get; }

        /// <summary>
        /// Репозиторий для работы с кодами подтверждения email.
        /// </summary>
        IEmailCodeRepository Codes { get; }

        /// <summary>
        /// Асинхронно сохраняет все изменения в базу данных.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Количество затронутых записей.</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
