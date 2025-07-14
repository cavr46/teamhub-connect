using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using TeamHubConnect.Application.Features.Messages.Commands.AddReaction;

namespace TeamHubConnect.Api.Endpoints.Messages;

public class AddReactionRequest
{
    public Guid MessageId { get; set; }
    public string Emoji { get; set; } = string.Empty;
}

[Authorize]
public class AddReactionEndpoint : Endpoint<AddReactionRequest, AddReactionResult>
{
    private readonly IMediator _mediator;

    public AddReactionEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/api/messages/{messageId}/reactions");
        Summary(s =>
        {
            s.Summary = "Add or remove a reaction to a message";
            s.Description = "Toggle a reaction on a message. If the reaction exists, it will be removed; otherwise, it will be added.";
            s.Response<AddReactionResult>(200, "Reaction added/removed successfully");
            s.Response(400, "Invalid request");
            s.Response(404, "Message not found");
        });
    }

    public override async Task HandleAsync(AddReactionRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Emoji))
        {
            AddError("Emoji is required");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var command = new AddReactionCommand(req.MessageId, req.Emoji);
        var result = await _mediator.Send(command, ct);

        if (!result.Success)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
            {
                await SendNotFoundAsync(ct);
                return;
            }
            
            if (result.ErrorMessage?.Contains("not authenticated") == true)
            {
                await SendUnauthorizedAsync(ct);
                return;
            }

            AddError(result.ErrorMessage ?? "Failed to add reaction");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        await SendOkAsync(result, ct);
    }
}