using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamHubConnect.Domain.Entities;

namespace TeamHubConnect.Infrastructure.Data.Configurations;

public class MessageAttachmentConfiguration : IEntityTypeConfiguration<MessageAttachment>
{
    public void Configure(EntityTypeBuilder<MessageAttachment> builder)
    {
        builder.ToTable("MessageAttachments");

        builder.HasKey(ma => ma.Id);

        builder.Property(ma => ma.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(ma => ma.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ma => ma.FileUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(ma => ma.ThumbnailUrl)
            .HasMaxLength(1000);

        builder.Property(ma => ma.Description)
            .HasMaxLength(500);

        builder.HasIndex(ma => ma.MessageId);

        builder.HasOne(ma => ma.Message)
            .WithMany(m => m.Attachments)
            .HasForeignKey(ma => ma.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}