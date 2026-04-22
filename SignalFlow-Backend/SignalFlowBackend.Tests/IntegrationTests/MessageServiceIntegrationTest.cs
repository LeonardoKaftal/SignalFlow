using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SignalFlowBackend.Data;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;
using SignalFlowBackend.Service;
using Xunit;

namespace SignalFlow.Backend.IntegrationTests;

public class MessageServiceIntegrationTest(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    private static string ShortSuffix() => Guid.NewGuid().ToString("N")[..8];

    private static User BuildUser()
    {
        var suffix = ShortSuffix();

        return new User
        {
            Id = Guid.NewGuid(),
            Username = $"u-{suffix}",
            Email = $"m-{suffix}@sf.it",
            PasswordHash = "hash",
            RegistrationTime = DateTime.UtcNow,
            RefreshTokenHash = "refresh-hash",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };
    }

    private static ChatConversation BuildConversation()
    {
        return new ChatConversation
        {
            ConversationId = Guid.NewGuid(),
            Name = $"conv-{ShortSuffix()}",
            IsGlobal = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static ConversationParticipant BuildParticipant(User user, ChatConversation conversation)
    {
        return new ConversationParticipant
        {
            ConversationParticipantId = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            ConversationId = conversation.ConversationId,
            ChatConversation = conversation,
            LastAccess = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task SaveMessage_ShouldPersistMessage_WhenRequestIsValid()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IMessageService>();

        var user = BuildUser();
        var conversation = BuildConversation();
        var participant = BuildParticipant(user, conversation);

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.Participants.AddAsync(participant, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var toSave = new MessageDto(Guid.Empty, conversation.ConversationId, participant.ConversationParticipantId, user.Username, DateTime.MinValue, "Integration message");

        // when
        var result = await service.SaveMessage(toSave);

        // then
        result.Should().NotBeNull();
        result!.ConversationId.Should().Be(conversation.ConversationId);
        result.SenderId.Should().Be(participant.ConversationParticipantId);
        result.Content.Should().Be("Integration message");

        var saved = await dbContext.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MessageId == result.MessageId, cancellationToken);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveMessage_ShouldReturnNull_WhenSenderIdIsNotAConversationParticipantId()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IMessageService>();

        var user = BuildUser();
        var conversation = BuildConversation();

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Intentionally wrong: this is a UserId, not a ConversationParticipantId.
        var toSave = new MessageDto(Guid.Empty, conversation.ConversationId, user.Id, user.Username, DateTime.MinValue, "Integration message");

        // when
        var result = await service.SaveMessage(toSave);

        // then
        result.Should().BeNull();

        var messageCount = await dbContext.Messages.CountAsync(cancellationToken);
        messageCount.Should().Be(0);
    }

    [Fact]
    public async Task GetMessageById_ShouldReturnMessageDto_WhenMessageExists()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IMessageService>();

        var user = BuildUser();
        var conversation = BuildConversation();
        var participant = BuildParticipant(user, conversation);
        var message = new Message
        {
            MessageId = Guid.NewGuid(),
            ConversationId = conversation.ConversationId,
            Conversation = conversation,
            SenderId = participant.ConversationParticipantId,
            Sender = participant,
            SentTime = DateTime.UtcNow,
            Content = "Hello"
        };

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.Participants.AddAsync(participant, cancellationToken);
        await dbContext.Messages.AddAsync(message, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var result = await service.GetMessageById(message.MessageId);

        // then
        result.Should().NotBeNull();
        result!.MessageId.Should().Be(message.MessageId);
        result.Content.Should().Be("Hello");
    }

    [Fact]
    public async Task GetAllMessagesByConversationId_ShouldReturnMessagesOrderedBySentTime_WhenConversationHasMessages()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IMessageService>();

        var user = BuildUser();
        var conversation = BuildConversation();
        var participant = BuildParticipant(user, conversation);

        var older = new Message
        {
            MessageId = Guid.NewGuid(),
            ConversationId = conversation.ConversationId,
            Conversation = conversation,
            SenderId = participant.ConversationParticipantId,
            Sender = participant,
            SentTime = DateTime.UtcNow.AddMinutes(-10),
            Content = "First"
        };

        var newer = new Message
        {
            MessageId = Guid.NewGuid(),
            ConversationId = conversation.ConversationId,
            Conversation = conversation,
            SenderId = participant.ConversationParticipantId,
            Sender = participant,
            SentTime = DateTime.UtcNow,
            Content = "Second"
        };

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.Participants.AddAsync(participant, cancellationToken);
        await dbContext.Messages.AddRangeAsync([newer, older], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var result = (await service.GetAllMessagesByConversationId(conversation.ConversationId))!.ToList();

        // then
        result.Should().HaveCount(2);
        result[0].MessageId.Should().Be(older.MessageId);
        result[1].MessageId.Should().Be(newer.MessageId);
    }

    [Fact]
    public async Task GetAllMessagesByConversationIdAndConversationParticipantId_ShouldReturnOnlyParticipantMessages_WhenConversationHasMultipleSenders()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IMessageService>();

        var userOne = BuildUser();
        var userTwo = BuildUser();
        var conversation = BuildConversation();
        var participantOne = BuildParticipant(userOne, conversation);
        var participantTwo = BuildParticipant(userTwo, conversation);

        await dbContext.Users.AddRangeAsync([userOne, userTwo], cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.Participants.AddRangeAsync([participantOne, participantTwo], cancellationToken);
        await dbContext.Messages.AddRangeAsync([
            new Message
            {
                MessageId = Guid.NewGuid(),
                ConversationId = conversation.ConversationId,
                Conversation = conversation,
                SenderId = participantOne.ConversationParticipantId,
                Sender = participantOne,
                SentTime = DateTime.UtcNow.AddMinutes(-2),
                Content = "U1"
            },
            new Message
            {
                MessageId = Guid.NewGuid(),
                ConversationId = conversation.ConversationId,
                Conversation = conversation,
                SenderId = participantTwo.ConversationParticipantId,
                Sender = participantTwo,
                SentTime = DateTime.UtcNow.AddMinutes(-1),
                Content = "U2"
            }
        ], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var result = (await service.GetAllMessagesByConversationIdAndConversationParticipantId(
            conversation.ConversationId,
            participantOne.ConversationParticipantId))!.ToList();

        // then
        result.Should().HaveCount(1);
        result[0].SenderId.Should().Be(participantOne.ConversationParticipantId);
        result[0].Content.Should().Be("U1");
    }

    [Fact]
    public async Task GetLatestMessageByConversationId_ShouldReturnMostRecentMessage_WhenConversationHasMessages()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IMessageService>();

        var user = BuildUser();
        var conversation = BuildConversation();
        var participant = BuildParticipant(user, conversation);

        var older = new Message
        {
            MessageId = Guid.NewGuid(),
            ConversationId = conversation.ConversationId,
            Conversation = conversation,
            SenderId = participant.ConversationParticipantId,
            Sender = participant,
            SentTime = DateTime.UtcNow.AddMinutes(-5),
            Content = "Old"
        };

        var latest = new Message
        {
            MessageId = Guid.NewGuid(),
            ConversationId = conversation.ConversationId,
            Conversation = conversation,
            SenderId = participant.ConversationParticipantId,
            Sender = participant,
            SentTime = DateTime.UtcNow,
            Content = "Recent"
        };

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.Participants.AddAsync(participant, cancellationToken);
        await dbContext.Messages.AddRangeAsync([older, latest], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var result = await service.GetLatestMessageByConversationId(conversation.ConversationId);

        // then
        result.Should().NotBeNull();
        result!.MessageId.Should().Be(latest.MessageId);
        result.Content.Should().Be("Recent");
    }

    [Fact]
    public async Task UpdateMessage_ShouldReturnUpdatedMessage_WhenMessageExists()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IMessageService>();

        var user = BuildUser();
        var conversation = BuildConversation();
        var participant = BuildParticipant(user, conversation);
        var message = new Message
        {
            MessageId = Guid.NewGuid(),
            ConversationId = conversation.ConversationId,
            Conversation = conversation,
            SenderId = participant.ConversationParticipantId,
            Sender = participant,
            SentTime = DateTime.UtcNow,
            Content = "To be updated"
        };

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.Participants.AddAsync(participant, cancellationToken);
        await dbContext.Messages.AddAsync(message, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var result = await service.UpdateMessage(message.MessageId, "Updated");

        // then
        result.Should().NotBeNull();
        result!.Content.Should().Be("Updated");
    }

    [Fact]
    public async Task DeleteMessage_ShouldReturnTrueAndRemoveMessage_WhenMessageExists()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IMessageService>();

        var user = BuildUser();
        var conversation = BuildConversation();
        var participant = BuildParticipant(user, conversation);
        var message = new Message
        {
            MessageId = Guid.NewGuid(),
            ConversationId = conversation.ConversationId,
            Conversation = conversation,
            SenderId = participant.ConversationParticipantId,
            Sender = participant,
            SentTime = DateTime.UtcNow,
            Content = "To be deleted"
        };

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.Participants.AddAsync(participant, cancellationToken);
        await dbContext.Messages.AddAsync(message, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var deleted = await service.DeleteMessage(message.MessageId);

        // then
        deleted.Should().BeTrue();

        var found = await dbContext.Messages.FindAsync([message.MessageId], cancellationToken);
        found.Should().BeNull();
    }

    [Fact]
    public async Task GetAllMessagesByUserId_ShouldReturnUserMessagesOrderedDesc_WhenUserHasMessagesInDifferentConversations()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IMessageService>();

        var user = BuildUser();
        var secondUser = BuildUser();

        var conversationOne = BuildConversation();
        var conversationTwo = BuildConversation();

        var participantOne = BuildParticipant(user, conversationOne);
        var participantTwo = BuildParticipant(user, conversationTwo);
        var otherParticipant = BuildParticipant(secondUser, conversationTwo);

        var older = new Message
        {
            MessageId = Guid.NewGuid(),
            ConversationId = conversationOne.ConversationId,
            Conversation = conversationOne,
            SenderId = participantOne.ConversationParticipantId,
            Sender = participantOne,
            SentTime = DateTime.UtcNow.AddMinutes(-4),
            Content = "M1"
        };

        var newer = new Message
        {
            MessageId = Guid.NewGuid(),
            ConversationId = conversationTwo.ConversationId,
            Conversation = conversationTwo,
            SenderId = participantTwo.ConversationParticipantId,
            Sender = participantTwo,
            SentTime = DateTime.UtcNow.AddMinutes(-1),
            Content = "M2"
        };

        var otherUserMessage = new Message
        {
            MessageId = Guid.NewGuid(),
            ConversationId = conversationTwo.ConversationId,
            Conversation = conversationTwo,
            SenderId = otherParticipant.ConversationParticipantId,
            Sender = otherParticipant,
            SentTime = DateTime.UtcNow,
            Content = "Other user"
        };

        await dbContext.Users.AddRangeAsync([user, secondUser], cancellationToken);
        await dbContext.Conversations.AddRangeAsync([conversationOne, conversationTwo], cancellationToken);
        await dbContext.Participants.AddRangeAsync([participantOne, participantTwo, otherParticipant], cancellationToken);
        await dbContext.Messages.AddRangeAsync([older, newer, otherUserMessage], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var result = (await service.GetAllMessagesByUserId(user.Id))!.ToList();

        // then
        result.Should().HaveCount(2);
        result[0].MessageId.Should().Be(newer.MessageId);
        result[1].MessageId.Should().Be(older.MessageId);
    }
}


