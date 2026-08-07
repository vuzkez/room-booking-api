using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomBookingApi.Domain.Entities;

namespace RoomBookingApi.Infrastructure.Data.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.Property(x => x.Status).IsRequired();
            builder.Property(x => x.StartTime).HasColumnType("datetime").IsRequired();
            builder.Property(x => x.EndTime).HasColumnType("datetime").IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnType("datetime").IsRequired();
            builder.Property(x => x.TotalPrice).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.Version).IsConcurrencyToken();
            builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Room).WithMany(x => x.Bookings).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.RoomId);
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.Status);
        }
    }
}
