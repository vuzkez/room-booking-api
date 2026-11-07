using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using RoomBookingApi.Application.Interfaces.Repositories;
using RoomBookingApi.Infrastructure.Data;

namespace RoomBookingApi.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _tx = null;
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
        public async ValueTask DisposeAsync()   
        {
            if (!_disposed)
            {
                if (_tx != null)
                {
                    try
                    {
                        await _tx.RollbackAsync();
                    }
                    catch (Exception)
                    {

                    }
                    finally
                    {
                        _tx = null;
                    }
                }

                await _context.DisposeAsync();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_tx != null)
                return;

            _tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_tx == null) 
                return;

            await _tx.CommitAsync(cancellationToken);
            _tx = null;
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_tx == null)
                return;

            await _tx.RollbackAsync(cancellationToken);
            _tx = null;
        }
    }
}
