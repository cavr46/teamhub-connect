using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamHubConnect.Domain.Entities;

namespace TeamHubConnect.Infrastructure.Data.Configurations;

public class MessageMentionConfiguration : IEntityTypeConfiguration<MessageMention>
{
    public void Configure(EntityTypeBuilder<MessageMention> builder)
    {
        builder.ToTable("MessageMentions");

        builder.HasKey(mm => mm.Id);

        builder.Property(mm => mm.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.HasIndex(mm => new { mm.MessageId, mm.MentionedUserId })
            .IsUnique();

        builder.HasIndex(mm => mm.MentionedUserId);

        builder.HasOne(mm => mm.Message)
            .WithMany(m => m.Mentions)
            .HasForeignKey(mm => mm.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mm => mm.MentionedUser)
            .WithMany()
            .HasForeignKey(mm => mm.MentionedUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}