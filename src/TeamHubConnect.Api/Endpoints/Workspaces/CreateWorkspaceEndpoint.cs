using FastEndpoints;
using MediatR;
using TeamHubConnect.Application.Features.Workspaces.Commands.CreateWorkspace;

namespace TeamHubConnect.Api.Endpoints.Workspaces;

public class CreateWorkspaceEndpoint : Endpoint<CreateWorkspaceCommand, CreateWorkspaceResult>
{
    private readonly IMediator _mediator;

    public CreateWorkspaceEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/workspaces");
        Version(1);
        Summary(s =>
        {
            s.Summary = "Create a new workspace";
            s.Description = "Creates a new workspace with default channels and settings";
            s.ResponseExamples[200] = new CreateWorkspaceResult
            {
                WorkspaceId = Guid.NewGuid(),
                Name = "My Company",
                Slug = "my-company",
                GeneralChannelId = Guid.NewGuid(),
                RandomChannelId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
        });
    }

    public override async Task HandleAsync(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        await SendOkAsync(result, cancellationToken);
    }
}