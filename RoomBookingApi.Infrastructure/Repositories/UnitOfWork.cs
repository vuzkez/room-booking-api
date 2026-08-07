using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoomBookingApi.Application.Interfaces.Repositories;
using RoomBookingApi.Infrastructure.Data;

namespace RoomBookingApi.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        public IBookingRepository Bookings { get; }
        public IRoomRepository Rooms {  get; }
        public IEmailCodeRepository Codes { get; }
        private bool _disposed;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Codes = new EmailCodeRepository(_context);
            Bookings = new BookingRepository(_context);
            Rooms = new RoomRepository(_context);
        }
        public void Dispose()
        {
            if (!_disposed)
            {
                _context.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);    
        }
        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                await _context.DisposeAsync();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

    }
}
