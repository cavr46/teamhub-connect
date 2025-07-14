using FluentAssertions;
using TeamHubConnect.Domain.Entities;
using TeamHubConnect.Domain.Enums;
using Xunit;

namespace TeamHubConnect.Domain.Tests.Entities;

public class MessageTests
{
    [Fact]
    public void Create_ShouldCreateValidMessage()
    {
        // Arrange
        var content = "Hello, World!";
        var authorId = Guid.NewGuid();
        var channelId = Guid.NewGuid();

        // Act
        var message = Message.Create(content, authorId, channelId);

        // Assert
        message.Should().NotBeNull();
        message.Content.Should().Be(content);
        message.AuthorId.Should().Be(authorId);
        message.ChannelId.Should().Be(channelId);
        message.Type.Should().Be(MessageType.Text);
        message.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        message.IsDeleted.Should().BeFalse();
        message.IsEdited.Should().BeFalse();
    }

    [Fact]
    public void CreateScheduled_ShouldCreateScheduledMessage()
    {
        // Arrange
        var content = "Scheduled message";
        var authorId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var scheduledAt = DateTime.UtcNow.AddHours(1);

        // Act
        var message = Message.CreateScheduled(content, authorId, channelId, scheduledAt);

        // Assert
        message.Should().NotBeNull();
        message.ScheduledAt.Should().Be(scheduledAt);
        message.DeliveryStatus.Should().Be(MessageDeliveryStatus.Scheduled);
        message.IsScheduled.Should().BeTrue();
    }

    [Fact]
    public void Edit_ShouldUpdateMessageContent()
    {
        // Arrange
        var message = Message.Create("Original content", Guid.NewGuid(), Guid.NewGuid());
        var newContent = "Edited content";
        var reason = "Fixed typo";

        // Act
        message.Edit(newContent, reason);

        // Assert
        message.Content.Should().Be(newContent);
        message.IsEdited.Should().BeTrue();
        message.EditedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        message.EditReason.Should().Be(reason);
        message.EditHistory.Should().HaveCount(1);
        message.EditHistory.First().NewContent.Should().Be(newContent);
    }

    [Fact]
    public void Edit_DeletedMessage_ShouldThrowException()
    {
        // Arrange
        var message = Message.Create("Content", Guid.NewGuid(), Guid.NewGuid());
        message.Delete(Guid.NewGuid());

        // Act & Assert
        var act = () => message.Edit("New content");
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Cannot edit deleted message");
    }

    [Fact]
    public void AddReaction_ShouldAddReactionToMessage()
    {
        // Arrange
        var message = Message.Create("Content", Guid.NewGuid(), Guid.NewGuid());
        var emoji = "👍";
        var userId = Guid.NewGuid();

        // Act
        message.AddReaction(emoji, userId);

        // Assert
        message.Reactions.Should().HaveCount(1);
        message.Reactions.First().Emoji.Should().Be(emoji);
        message.Reactions.First().UserId.Should().Be(userId);
    }

    [Fact]
    public void AddReaction_DuplicateReaction_ShouldNotAddDuplicate()
    {
        // Arrange
        var message = Message.Create("Content", Guid.NewGuid(), Guid.NewGuid());
        var emoji = "👍";
        var userId = Guid.NewGuid();

        // Act
        message.AddReaction(emoji, userId);
        message.AddReaction(emoji, userId); // Duplicate

        // Assert
        message.Reactions.Should().HaveCount(1);
    }

    [Fact]
    public void SetExpiration_ShouldSetExpirationDate()
    {
        // Arrange
        var message = Message.Create("Content", Guid.NewGuid(), Guid.NewGuid());
        var expiresAt = DateTime.UtcNow.AddDays(1);

        // Act
        message.SetExpiration(expiresAt);

        // Assert
        message.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void IsExpired_WithPastExpiration_ShouldReturnTrue()
    {
        // Arrange
        var message = Message.Create("Content", Guid.NewGuid(), Guid.NewGuid());
        message.SetExpiration(DateTime.UtcNow.AddMinutes(-1)); // Past expiration

        // Act & Assert
        message.IsExpired.Should().BeTrue();
    }

    [Theory]
    [InlineData(MessagePriority.Low)]
    [InlineData(MessagePriority.Normal)]
    [InlineData(MessagePriority.High)]
    [InlineData(MessagePriority.Urgent)]
    public void SetPriority_ShouldSetCorrectPriority(MessagePriority priority)
    {
        // Arrange
        var message = Message.Create("Content", Guid.NewGuid(), Guid.NewGuid());

        // Act
        message.SetPriority(priority);

        // Assert
        message.Priority.Should().Be(priority);
    }
}