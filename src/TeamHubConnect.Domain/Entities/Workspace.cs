using TeamHubConnect.Domain.Common;
using TeamHubConnect.Domain.Enums;
using TeamHubConnect.Domain.ValueObjects;

namespace TeamHubConnect.Domain.Entities;

public class Workspace : BaseAuditableEntity
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? BannerUrl { get; private set; }
    public WorkspaceSettings Settings { get; private set; } = new();
    public BrandingSettings Branding { get; private set; } = new();
    public SecuritySettings Security { get; private set; } = new();
    public bool IsActive { get; private set; } = true;
    public SubscriptionPlan Plan { get; private set; } = SubscriptionPlan.Free;
    public DateTime? PlanExpiresAt { get; private set; }
    public int MaxMembers { get; private set; } = 10;
    public int MaxChannels { get; private set; } = 10;
    public long StorageQuotaBytes { get; private set; } = 5L * 1024 * 1024 * 1024; // 5GB
    public long StorageUsedBytes { get; private set; }

    private readonly List<WorkspaceUser> _members = [];
    public IReadOnlyCollection<WorkspaceUser> Members => _members.AsReadOnly();

    private readonly List<Channel> _channels = [];
    public IReadOnlyCollection<Channel> Channels => _channels.AsReadOnly();

    private readonly List<WorkspaceIntegration> _integrations = [];
    public IReadOnlyCollection<WorkspaceIntegration> Integrations => _integrations.AsReadOnly();

    private readonly List<CustomEmoji> _customEmojis = [];
    public IReadOnlyCollection<CustomEmoji> CustomEmojis => _customEmojis.AsReadOnly();

    private Workspace() { }

    public static Workspace Create(
        string name,
        string slug,
        string? description = null,
        Guid? ownerId = null)
    {
        var workspace = new Workspace
        {
            Name = name,
            Slug = slug,
            Description = description
        };

        if (ownerId.HasValue)
        {
            workspace.AddMember(ownerId.Value, WorkspaceRole.Owner);
        }

        return workspace;
    }

    public void UpdateDetails(
        string? name = null,
        string? description = null,
        string? logoUrl = null,
        string? bannerUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name;
            
        Description = description;
        LogoUrl = logoUrl;
        BannerUrl = bannerUrl;
        
        MarkAsUpdated();
    }

    public void UpdateSettings(WorkspaceSettings settings)
    {
        Settings = settings;
        MarkAsUpdated();
    }

    public void UpdateBranding(BrandingSettings branding)
    {
        Branding = branding;
        MarkAsUpdated();
    }

    public void UpdateSecurity(SecuritySettings security)
    {
        Security = security;
        MarkAsUpdated();
    }

    public void UpdatePlan(SubscriptionPlan plan, DateTime? expiresAt = null)
    {
        Plan = plan;
        PlanExpiresAt = expiresAt;
        
        MaxMembers = plan switch
        {
            SubscriptionPlan.Free => 10,
            SubscriptionPlan.Pro => 100,
            SubscriptionPlan.Business => 500,
            SubscriptionPlan.Enterprise => 10000,
            _ => 10
        };

        MaxChannels = plan switch
        {
            SubscriptionPlan.Free => 10,
            SubscriptionPlan.Pro => 100,
            SubscriptionPlan.Business => 1000,
            SubscriptionPlan.Enterprise => int.MaxValue,
            _ => 10
        };

        StorageQuotaBytes = plan switch
        {
            SubscriptionPlan.Free => 5L * 1024 * 1024 * 1024, // 5GB
            SubscriptionPlan.Pro => 100L * 1024 * 1024 * 1024, // 100GB
            SubscriptionPlan.Business => 1024L * 1024 * 1024 * 1024, // 1TB
            SubscriptionPlan.Enterprise => long.MaxValue,
            _ => 5L * 1024 * 1024 * 1024
        };
        
        MarkAsUpdated();
    }

    public void AddMember(Guid userId, WorkspaceRole role = WorkspaceRole.Member)
    {
        if (_members.Count >= MaxMembers)
            throw new InvalidOperationException("Workspace has reached maximum member limit");

        if (_members.Any(m => m.UserId == userId))
            return;

        _members.Add(WorkspaceUser.Create(Id, userId, role));
        MarkAsUpdated();
    }

    public void RemoveMember(Guid userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member != null)
        {
            _members.Remove(member);
            MarkAsUpdated();
        }
    }

    public void UpdateMemberRole(Guid userId, WorkspaceRole role)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member != null)
        {
            member.UpdateRole(role);
            MarkAsUpdated();
        }
    }

    public Channel CreateChannel(
        string name,
        string? description = null,
        ChannelType type = ChannelType.Public,
        Guid? createdBy = null)
    {
        if (_channels.Count >= MaxChannels)
            throw new InvalidOperationException("Workspace has reached maximum channel limit");

        var channel = Channel.Create(name, description, type, Id, createdBy);
        _channels.Add(channel);
        MarkAsUpdated();
        
        return channel;
    }

    public void AddCustomEmoji(string name, string imageUrl, Guid createdBy)
    {
        if (_customEmojis.Any(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return;

        _customEmojis.Add(CustomEmoji.Create(name, imageUrl, Id, createdBy));
        MarkAsUpdated();
    }

    public void RemoveCustomEmoji(string name)
    {
        var emoji = _customEmojis.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (emoji != null)
        {
            _customEmojis.Remove(emoji);
            MarkAsUpdated();
        }
    }

    public void UpdateStorageUsage(long bytesUsed)
    {
        StorageUsedBytes = bytesUsed;
        MarkAsUpdated();
    }

    public bool HasStorageQuota(long additionalBytes = 0)
    {
        return StorageUsedBytes + additionalBytes <= StorageQuotaBytes;
    }

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }
}

public class WorkspaceUser : BaseEntity
{
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public WorkspaceRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? CustomTitle { get; private set; }

    public User User { get; private set; } = null!;
    public Workspace Workspace { get; private set; } = null!;

    private WorkspaceUser() { }

    public static WorkspaceUser Create(Guid workspaceId, Guid userId, WorkspaceRole role)
    {
        return new WorkspaceUser
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };
    }

    public void UpdateRole(WorkspaceRole role)
    {
        Role = role;
        MarkAsUpdated();
    }

    public void UpdateCustomTitle(string? title)
    {
        CustomTitle = title;
        MarkAsUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }
}