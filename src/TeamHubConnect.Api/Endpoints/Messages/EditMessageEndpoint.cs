using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using TeamHubConnect.Application.Features.Messages.Commands.EditMessage;

namespace TeamHubConnect.Api.Endpoints.Messages;

public class EditMessageRequest
{
    public Guid MessageId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? EditReason { get; set; }
}

[Authorize]
public class EditMessageEndpoint : Endpoint<EditMessageRequest, EditMessageResult>
{
    private readonly IMediator _mediator;

    public EditMessageEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/api/messages/{messageId}");
        Summary(s =>
        {
            s.Summary = "Edit a message";
            s.Description = "Edit the content of an existing message";
            s.Response<EditMessageResult>(200, "Message edited successfully");
            s.Response(400, "Invalid request");
            s.Response(403, "Not authorized to edit this message");
            s.Response(404, "Message not found");
        });
    }

    public override async Task HandleAsync(EditMessageRequest req, CancellationToken ct)
    {
        var command = new EditMessageCommand(req.MessageId, req.Content, req.EditReason);
        var result = await _mediator.Send(command, ct);

        if (!result.Success)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
            {
                await SendNotFoundAsync(ct);
                return;
            }
            
            if (result.ErrorMessage?.Contains("not authenticated") == true || 
                result.ErrorMessage?.Contains("only edit your own") == true)
            {
                await SendForbiddenAsync(ct);
                return;
            }

            AddError(result.ErrorMessage ?? "Failed to edit message");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        await SendOkAsync(result, ct);
    }
}