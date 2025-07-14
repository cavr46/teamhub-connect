using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamHubConnect.Domain.Entities;
using System.Text.Json;

namespace TeamHubConnect.Infrastructure.Data.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Content)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(m => m.FormattedContent)
            .HasMaxLength(8000);

        builder.Property(m => m.Type)
            .HasConversion<string>();

        builder.Property(m => m.Priority)
            .HasConversion<string>();

        builder.Property(m => m.DeliveryStatus)
            .HasConversion<string>();

        builder.Property(m => m.EditReason)
            .HasMaxLength(200);

        builder.Property(m => m.DeletedBy)
            .HasMaxLength(100);

        builder.Property(m => m.Metadata)
            .HasConversion(
                metadata => JsonSerializer.Serialize(metadata, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<MessageMetadata>(json, (JsonSerializerOptions?)null) ?? new MessageMetadata())
            .HasColumnType("nvarchar(max)");

        builder.Property(m => m.EditHistory)
            .HasConversion(
                history => JsonSerializer.Serialize(history, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<MessageEdit>>(json, (JsonSerializerOptions?)null) ?? new List<MessageEdit>())
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(m => m.ChannelId);
        builder.HasIndex(m => m.AuthorId);
        builder.HasIndex(m => m.CreatedAt);
        builder.HasIndex(m => m.ParentMessageId);
        builder.HasIndex(m => m.ThreadRootId);

        builder.HasOne(m => m.Author)
            .WithMany()
            .HasForeignKey(m => m.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.ParentMessage)
            .WithMany(m => m.Replies)
            .HasForeignKey(m => m.ParentMessageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.Reactions)
            .WithOne(r => r.Message)
            .HasForeignKey(r => r.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Attachments)
            .WithOne(a => a.Message)
            .HasForeignKey(a => a.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Mentions)
            .WithOne(mention => mention.Message)
            .HasForeignKey(mention => mention.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events
        builder.Ignore(m => m.DomainEvents);
    }
}