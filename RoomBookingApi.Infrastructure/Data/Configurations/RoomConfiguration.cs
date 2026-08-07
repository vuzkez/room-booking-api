using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomBookingApi.Domain.Entities;

namespace RoomBookingApi.Infrastructure.Data.Configurations
{
    public class RoomConfiguration : IEntityTypeConfiguration<Room>
    {
        public void Configure(EntityTypeBuilder<Room> builder) 
        {
            builder.Property(x => x.PricePerHour).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.RoomName).IsRequired();
            builder.Property(x => x.Capacity).IsRequired();
            builder.Property(x => x.Location).IsRequired();

            builder.HasIndex(x => x.IsActive);
            builder.HasIndex(x => x.Capacity);
        }
    }
}
