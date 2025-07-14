using Microsoft.EntityFrameworkCore;
using TeamHubConnect.Domain.Entities;

namespace TeamHubConnect.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Workspace> Workspaces { get; }
    DbSet<WorkspaceUser> WorkspaceUsers { get; }
    DbSet<Channel> Channels { get; }
    DbSet<ChannelMember> ChannelMembers { get; }
    DbSet<Message> Messages { get; }
    DbSet<MessageReaction> MessageReactions { get; }
    DbSet<MessageAttachment> MessageAttachments { get; }
    DbSet<MessageMention> MessageMentions { get; }
    DbSet<CustomEmoji> CustomEmojis { get; }
    DbSet<WorkspaceIntegration> WorkspaceIntegrations { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}