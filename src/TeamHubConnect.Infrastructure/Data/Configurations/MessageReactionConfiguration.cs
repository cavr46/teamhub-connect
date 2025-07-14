using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamHubConnect.Domain.Entities;

namespace TeamHubConnect.Infrastructure.Data.Configurations;

public class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> builder)
    {
        builder.ToTable("MessageReactions");

        builder.HasKey(mr => mr.Id);

        builder.Property(mr => mr.Emoji)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(mr => new { mr.MessageId, mr.UserId, mr.Emoji })
            .IsUnique();

        builder.HasOne(mr => mr.Message)
            .WithMany(m => m.Reactions)
            .HasForeignKey(mr => mr.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mr => mr.User)
            .WithMany()
            .HasForeignKey(mr => mr.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}