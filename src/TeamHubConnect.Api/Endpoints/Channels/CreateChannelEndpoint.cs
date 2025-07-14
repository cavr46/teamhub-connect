using FastEndpoints;
using MediatR;
using TeamHubConnect.Application.Features.Channels.Commands.CreateChannel;

namespace TeamHubConnect.Api.Endpoints.Channels;

public class CreateChannelEndpoint : Endpoint<CreateChannelCommand, CreateChannelResult>
{
    private readonly IMediator _mediator;

    public CreateChannelEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/channels");
        Version(1);
        Summary(s =>
        {
            s.Summary = "Create a new channel";
            s.Description = "Creates a new channel in the specified workspace with the given configuration";
            s.ResponseExamples[200] = new CreateChannelResult
            {
                ChannelId = Guid.NewGuid(),
                Name = "general",
                Type = "Public",
                CreatedAt = DateTime.UtcNow
            };
        });
    }

    public override async Task HandleAsync(CreateChannelCommand request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        await SendOkAsync(result, cancellationToken);
    }
}