using Microsoft.EntityFrameworkCore;
using TeamHubConnect.Application.Common.Interfaces;
using TeamHubConnect.Domain.Entities;
using TeamHubConnect.Infrastructure.Data.Configurations;
using System.Reflection;

namespace TeamHubConnect.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceUser> WorkspaceUsers => Set<WorkspaceUser>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<ChannelMember> ChannelMembers => Set<ChannelMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();
    public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();
    public DbSet<MessageMention> MessageMentions => Set<MessageMention>();
    public DbSet<CustomEmoji> CustomEmojis => Set<CustomEmoji>();
    public DbSet<WorkspaceIntegration> WorkspaceIntegrations => Set<WorkspaceIntegration>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all configurations from this assembly
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Global query filters for soft delete
        builder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Workspace>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Channel>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Message>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<CustomEmoji>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<WorkspaceIntegration>().HasQueryFilter(e => !e.IsDeleted);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TeamHubConnect;Trusted_Connection=true;MultipleActiveResultSets=true");
        }
    }
}