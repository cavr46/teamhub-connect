using TeamHubConnect.Domain.Common;
using TeamHubConnect.Domain.Enums;

namespace TeamHubConnect.Domain.Entities;

public class WorkspaceIntegration : BaseAuditableEntity
{
    public string Name { get; private set; } = null!;
    public IntegrationType Type { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public bool IsEnabled { get; private set; } = true;
    public Dictionary<string, string> Configuration { get; private set; } = [];
    public Dictionary<string, string> Credentials { get; private set; } = [];
    public string? WebhookUrl { get; private set; }
    public string? ApiKey { get; private set; }
    public DateTime? LastSyncAt { get; private set; }
    public string? LastSyncStatus { get; private set; }
    public string? LastError { get; private set; }
    public int FailureCount { get; private set; }

    public Workspace Workspace { get; private set; } = null!;

    private WorkspaceIntegration() { }

    public static WorkspaceIntegration Create(
        string name,
        IntegrationType type,
        Guid workspaceId,
        Guid createdBy)
    {
        var integration = new WorkspaceIntegration
        {
            Name = name,
            Type = type,
            WorkspaceId = workspaceId
        };

        integration.SetCreatedBy(createdBy.ToString());
        return integration;
    }

    public void UpdateConfiguration(Dictionary<string, string> configuration)
    {
        Configuration = configuration;
        MarkAsUpdated();
    }

    public void UpdateCredentials(Dictionary<string, string> credentials)
    {
        Credentials = credentials;
        MarkAsUpdated();
    }

    public void SetWebhook(string webhookUrl)
    {
        WebhookUrl = webhookUrl;
        MarkAsUpdated();
    }

    public void SetApiKey(string apiKey)
    {
        ApiKey = apiKey;
        MarkAsUpdated();
    }

    public void UpdateSyncStatus(bool success, string? error = null)
    {
        LastSyncAt = DateTime.UtcNow;
        LastSyncStatus = success ? "Success" : "Failed";
        
        if (success)
        {
            FailureCount = 0;
            LastError = null;
        }
        else
        {
            FailureCount++;
            LastError = error;
        }
        
        MarkAsUpdated();
    }

    public void Enable()
    {
        IsEnabled = true;
        MarkAsUpdated();
    }

    public void Disable()
    {
        IsEnabled = false;
        MarkAsUpdated();
    }
}

public enum IntegrationType
{
    Webhook = 1,
    OAuth = 2,
    ApiKey = 3,
    GitHub = 4,
    Jira = 5,
    Trello = 6,
    GoogleDrive = 7,
    OneDrive = 8,
    Salesforce = 9,
    Zapier = 10,
    Custom = 99
}