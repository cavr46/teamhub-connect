using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamHubConnect.Domain.Entities;
using TeamHubConnect.Domain.ValueObjects;
using System.Text.Json;

namespace TeamHubConnect.Infrastructure.Data.Configurations;

public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.ToTable("Channels");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.Topic)
            .HasMaxLength(200);

        builder.Property(c => c.Type)
            .HasConversion<string>();

        builder.Property(c => c.ArchivedBy)
            .HasMaxLength(100);

        builder.Property(c => c.ArchiveReason)
            .HasMaxLength(500);

        builder.Property(c => c.Settings)
            .HasConversion(
                settings => JsonSerializer.Serialize(settings, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<ChannelSettings>(json, (JsonSerializerOptions?)null) ?? new ChannelSettings())
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(c => new { c.WorkspaceId, c.Name })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasMany(c => c.Members)
            .WithOne(cm => cm.Channel)
            .HasForeignKey(cm => cm.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Channel)
            .HasForeignKey(m => m.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.PinnedMessages)
            .WithOne(p => p.Channel)
            .HasForeignKey(p => p.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events
        builder.Ignore(c => c.DomainEvents);
    }
}