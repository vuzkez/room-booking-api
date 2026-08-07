using System;
using System.Collections.Generic;
using System.Text;
using RoomBookingApi.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RoomBookingApi.Infrastructure.Data.Configurations;

namespace RoomBookingApi.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<UserApplication,RoleApplication,int>
    {
        public DbSet<Booking> Bookings {  get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<EmailConfirmationCode> EmailCodes { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfiguration(new BookingConfiguration());
            builder.ApplyConfiguration(new RoomConfiguration());
            builder.ApplyConfiguration(new EmailCodeConfiguration());
        }
    }
}
