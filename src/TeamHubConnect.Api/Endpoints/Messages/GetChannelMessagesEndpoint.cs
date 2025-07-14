using FastEndpoints;
using MediatR;
using TeamHubConnect.Application.Features.Messages.Queries.GetChannelMessages;

namespace TeamHubConnect.Api.Endpoints.Messages;

public class GetChannelMessagesEndpoint : Endpoint<GetChannelMessagesQuery, GetChannelMessagesResult>
{
    private readonly IMediator _mediator;

    public GetChannelMessagesEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/channels/{channelId}/messages");
        Version(1);
        Summary(s =>
        {
            s.Summary = "Get messages from a channel";
            s.Description = "Retrieves paginated messages from the specified channel with optional filtering";
        });
    }

    public override async Task HandleAsync(GetChannelMessagesQuery request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        await SendOkAsync(result, cancellationToken);
    }
}