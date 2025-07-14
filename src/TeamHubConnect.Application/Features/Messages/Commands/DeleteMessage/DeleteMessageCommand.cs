using MediatR;

namespace TeamHubConnect.Application.Features.Messages.Commands.DeleteMessage;

public record DeleteMessageCommand(
    Guid MessageId,
    bool HardDelete = false
) : IRequest<DeleteMessageResult>;

public record DeleteMessageResult(
    bool Success,
    string? ErrorMessage = null
);