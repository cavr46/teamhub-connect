using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamHubConnect.Domain.Entities;
using TeamHubConnect.Domain.ValueObjects;
using System.Text.Json;

namespace TeamHubConnect.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(u => u.Username)
            .IsUnique();

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255)
            .HasConversion(
                email => email.Value,
                value => new Email(value));

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.FirstName)
            .HasMaxLength(50);

        builder.Property(u => u.LastName)
            .HasMaxLength(50);

        builder.Property(u => u.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(u => u.TimeZone)
            .HasMaxLength(50);

        builder.Property(u => u.Status)
            .HasConversion<string>();

        builder.Property(u => u.StatusMessage)
            .HasMaxLength(200);

        builder.Property(u => u.Role)
            .HasConversion<string>();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.WorkspaceId)
            .IsRequired();

        builder.Property(u => u.FullName)
            .HasMaxLength(100);

        builder.Property(u => u.Avatar)
            .HasMaxLength(500);

        builder.Property(u => u.NotificationSettings)
            .HasConversion(
                settings => JsonSerializer.Serialize(settings, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<NotificationSettings>(json, (JsonSerializerOptions?)null) ?? new NotificationSettings())
            .HasColumnType("nvarchar(max)");

        builder.Property(u => u.PresenceSettings)
            .HasConversion(
                settings => JsonSerializer.Serialize(settings, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<PresenceSettings>(json, (JsonSerializerOptions?)null) ?? new PresenceSettings())
            .HasColumnType("nvarchar(max)");

        builder.Property(u => u.Skills)
            .HasConversion(
                skills => JsonSerializer.Serialize(skills, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<UserSkill>>(json, (JsonSerializerOptions?)null) ?? new List<UserSkill>())
            .HasColumnType("nvarchar(max)");

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt);

        builder.Property(u => u.CreatedBy)
            .HasMaxLength(100);

        builder.Property(u => u.UpdatedBy)
            .HasMaxLength(100);

        builder.HasMany(u => u.Workspaces)
            .WithOne(wu => wu.User)
            .HasForeignKey(wu => wu.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events
        builder.Ignore(u => u.DomainEvents);
    }
}