using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamHubConnect.Domain.Entities;
using TeamHubConnect.Domain.ValueObjects;
using System.Text.Json;

namespace TeamHubConnect.Infrastructure.Data.Configurations;

public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("Workspaces");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(w => w.Slug)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(w => w.Slug)
            .IsUnique();

        builder.Property(w => w.Description)
            .HasMaxLength(500);

        builder.Property(w => w.LogoUrl)
            .HasMaxLength(500);

        builder.Property(w => w.BannerUrl)
            .HasMaxLength(500);

        builder.Property(w => w.Plan)
            .HasConversion<string>();

        builder.Property(w => w.Settings)
            .HasConversion(
                settings => JsonSerializer.Serialize(settings, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<WorkspaceSettings>(json, (JsonSerializerOptions?)null) ?? new WorkspaceSettings())
            .HasColumnType("nvarchar(max)");

        builder.Property(w => w.Branding)
            .HasConversion(
                branding => JsonSerializer.Serialize(branding, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<BrandingSettings>(json, (JsonSerializerOptions?)null) ?? new BrandingSettings())
            .HasColumnType("nvarchar(max)");

        builder.Property(w => w.Security)
            .HasConversion(
                security => JsonSerializer.Serialize(security, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<SecuritySettings>(json, (JsonSerializerOptions?)null) ?? new SecuritySettings())
            .HasColumnType("nvarchar(max)");

        builder.HasMany(w => w.Members)
            .WithOne(wu => wu.Workspace)
            .HasForeignKey(wu => wu.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.Channels)
            .WithOne(c => c.Workspace)
            .HasForeignKey(c => c.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.CustomEmojis)
            .WithOne(e => e.Workspace)
            .HasForeignKey(e => e.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.Integrations)
            .WithOne(i => i.Workspace)
            .HasForeignKey(i => i.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events
        builder.Ignore(w => w.DomainEvents);
    }
}