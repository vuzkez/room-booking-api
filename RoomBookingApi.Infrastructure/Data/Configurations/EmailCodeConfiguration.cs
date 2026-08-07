using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata;
using RoomBookingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RoomBookingApi.Infrastructure.Data.Configurations
{
    public class EmailCodeConfiguration : IEntityTypeConfiguration<EmailConfirmationCode>
    {
        public void Configure(EntityTypeBuilder<EmailConfirmationCode> builder)
        {
            builder.Property(x => x.Code).IsRequired();
            builder.Property(x => x.ExpiresAt).HasColumnType("datetime").IsRequired();
            builder.Property(x => x.IsUsed).IsRequired();
            builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.Code);
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.ExpiresAt);
        }
    }
}
