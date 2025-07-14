using FastEndpoints;
using MediatR;
using TeamHubConnect.Application.Features.Messages.Commands.SendMessage;

namespace TeamHubConnect.Api.Endpoints.Messages;

public class SendMessageEndpoint : Endpoint<SendMessageCommand, SendMessageResult>
{
    private readonly IMediator _mediator;

    public SendMessageEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/messages");
        Version(1);
        Summary(s =>
        {
            s.Summary = "Send a message to a channel";
            s.Description = "Sends a new message to the specified channel with optional attachments and scheduling";
            s.ResponseExamples[200] = new SendMessageResult
            {
                MessageId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                IsScheduled = false
            };
        });
    }

    public override async Task HandleAsync(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        await SendOkAsync(result, cancellationToken);
    }
}