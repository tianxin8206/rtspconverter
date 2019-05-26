using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RtspConverter.Application.Models;
using System;

namespace RtspConverter.Application.Config
{
    class ChannelConfig : IEntityTypeConfiguration<Channel>
    {
        public void Configure(EntityTypeBuilder<Channel> builder)
        {
            builder.ToTable("T_Channels");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasMaxLength(36);
            builder.Property(e => e.ChannelName).IsRequired().HasMaxLength(50);
            builder.Property(e => e.IsEnable).IsRequired();
            builder.Property(e => e.RtspUrl).IsRequired().HasMaxLength(255);
            builder.Property(e => e.Transport).HasConversion(v => v.ToString(), v => (Transport)Enum.Parse(typeof(Transport), v)).IsRequired().HasMaxLength(1);
            builder.Property(e => e.CreateTime).IsRequired();
            builder.Property(e => e.IsDelete).IsRequired();

            builder.HasQueryFilter(e => e.IsDelete == false);
        }
    }
}
