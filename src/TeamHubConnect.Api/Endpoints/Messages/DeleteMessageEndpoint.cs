using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using TeamHubConnect.Application.Features.Messages.Commands.DeleteMessage;

namespace TeamHubConnect.Api.Endpoints.Messages;

public class DeleteMessageRequest
{
    public Guid MessageId { get; set; }
    public bool HardDelete { get; set; } = false;
}

[Authorize]
public class DeleteMessageEndpoint : Endpoint<DeleteMessageRequest, DeleteMessageResult>
{
    private readonly IMediator _mediator;

    public DeleteMessageEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Delete("/api/messages/{messageId}");
        Summary(s =>
        {
            s.Summary = "Delete a message";
            s.Description = "Delete a message (soft delete by default)";
            s.Response<DeleteMessageResult>(200, "Message deleted successfully");
            s.Response(400, "Invalid request");
            s.Response(403, "Not authorized to delete this message");
            s.Response(404, "Message not found");
        });
    }

    public override async Task HandleAsync(DeleteMessageRequest req, CancellationToken ct)
    {
        var command = new DeleteMessageCommand(req.MessageId, req.HardDelete);
        var result = await _mediator.Send(command, ct);

        if (!result.Success)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
            {
                await SendNotFoundAsync(ct);
                return;
            }
            
            if (result.ErrorMessage?.Contains("not authenticated") == true || 
                result.ErrorMessage?.Contains("only delete your own") == true)
            {
                await SendForbiddenAsync(ct);
                return;
            }

            AddError(result.ErrorMessage ?? "Failed to delete message");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        await SendOkAsync(result, ct);
    }
}