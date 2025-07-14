using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamHubConnect.Application.Common.Interfaces;
using TeamHubConnect.Domain.Entities;
using TeamHubConnect.Domain.Enums;

namespace TeamHubConnect.Application.Features.Workspaces.Commands.CreateWorkspace;

public class CreateWorkspaceCommandHandler : IRequestHandler<CreateWorkspaceCommand, CreateWorkspaceResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateWorkspaceCommandHandler> _logger;

    public CreateWorkspaceCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<CreateWorkspaceCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<CreateWorkspaceResult> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            throw new UnauthorizedAccessException("User must be authenticated");

        // Check if slug is already taken
        var existingWorkspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Slug.ToLower() == request.Slug.ToLower(), cancellationToken);

        if (existingWorkspace != null)
            throw new InvalidOperationException("A workspace with this slug already exists");

        // Create workspace
        var workspace = Workspace.Create(request.Name, request.Slug, request.Description, userId.Value);
        
        if (!string.IsNullOrEmpty(request.LogoUrl))
        {
            workspace.UpdateDetails(logoUrl: request.LogoUrl);
        }

        _context.Workspaces.Add(workspace);

        Guid? generalChannelId = null;
        Guid? randomChannelId = null;

        // Create default channels
        if (request.CreateGeneralChannel)
        {
            var generalChannel = workspace.CreateChannel(
                "general",
                "General discussion channel for the workspace",
                ChannelType.Public,
                userId.Value);
            generalChannelId = generalChannel.Id;
        }

        if (request.CreateRandomChannel)
        {
            var randomChannel = workspace.CreateChannel(
                "random",
                "Random conversations and off-topic discussions",
                ChannelType.Public,
                userId.Value);
            randomChannelId = randomChannel.Id;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Workspace {WorkspaceId} created by user {UserId}", 
            workspace.Id, userId.Value);

        return new CreateWorkspaceResult
        {
            WorkspaceId = workspace.Id,
            Name = workspace.Name,
            Slug = workspace.Slug,
            GeneralChannelId = generalChannelId,
            RandomChannelId = randomChannelId,
            CreatedAt = workspace.CreatedAt
        };
    }
}