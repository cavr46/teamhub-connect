using TeamHubConnect.Domain.Common;

namespace TeamHubConnect.Domain.Entities;

public class CustomEmoji : BaseAuditableEntity
{
    public string Name { get; private set; } = null!;
    public string ImageUrl { get; private set; } = null!;
    public Guid WorkspaceId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? Description { get; private set; }
    public List<string> Aliases { get; private set; } = [];
    public int UsageCount { get; private set; }

    public Workspace Workspace { get; private set; } = null!;

    private CustomEmoji() { }

    public static CustomEmoji Create(
        string name,
        string imageUrl,
        Guid workspaceId,
        Guid createdBy,
        string? description = null)
    {
        var emoji = new CustomEmoji
        {
            Name = name,
            ImageUrl = imageUrl,
            WorkspaceId = workspaceId,
            Description = description
        };

        emoji.SetCreatedBy(createdBy.ToString());
        return emoji;
    }

    public void UpdateDetails(string? name = null, string? description = null, string? imageUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name;
            
        Description = description;
        
        if (!string.IsNullOrWhiteSpace(imageUrl))
            ImageUrl = imageUrl;
            
        MarkAsUpdated();
    }

    public void AddAlias(string alias)
    {
        if (!Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase))
        {
            Aliases.Add(alias);
            MarkAsUpdated();
        }
    }

    public void RemoveAlias(string alias)
    {
        if (Aliases.Remove(alias))
        {
            MarkAsUpdated();
        }
    }

    public void IncrementUsage()
    {
        UsageCount++;
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