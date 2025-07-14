using FluentValidation;

namespace TeamHubConnect.Application.Features.Messages.Commands.SendMessage;

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Message content is required")
            .MaximumLength(4000)
            .WithMessage("Message content cannot exceed 4000 characters");

        RuleFor(x => x.ChannelId)
            .NotEmpty()
            .WithMessage("Channel ID is required");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid message type");

        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("Invalid message priority");

        RuleFor(x => x.ScheduledAt)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.ScheduledAt.HasValue)
            .WithMessage("Scheduled time must be in the future");

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.ExpiresAt.HasValue)
            .WithMessage("Expiration time must be in the future");

        RuleForEach(x => x.Attachments)
            .SetValidator(new MessageAttachmentValidator());
    }
}

public class MessageAttachmentValidator : AbstractValidator<MessageAttachmentDto>
{
    public MessageAttachmentValidator()
    {
        RuleFor(x => x.Filename)
            .NotEmpty()
            .WithMessage("Filename is required")
            .MaximumLength(255)
            .WithMessage("Filename cannot exceed 255 characters");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .WithMessage("Content type is required");

        RuleFor(x => x.Size)
            .GreaterThan(0)
            .WithMessage("File size must be greater than 0")
            .LessThanOrEqualTo(100 * 1024 * 1024) // 100MB
            .WithMessage("File size cannot exceed 100MB");

        RuleFor(x => x.Url)
            .NotEmpty()
            .WithMessage("File URL is required")
            .Must(BeAValidUrl)
            .WithMessage("Invalid URL format");
    }

    private static bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}