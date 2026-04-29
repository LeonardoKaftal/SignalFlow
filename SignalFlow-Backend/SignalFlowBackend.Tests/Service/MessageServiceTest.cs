using FluentAssertions;
using JetBrains.Annotations;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;
using SignalFlowBackend.Repository;
using SignalFlowBackend.Service;
using Xunit;

namespace SignalFlow.Backend.Service;

[TestSubject(typeof(MessageService))]
public class MessageServiceTest
{
    private static MessageService BuildSut(
        IMessageRepository messageRepository,
        IConversationParticipantRepository? participantRepository = null)
    {
        return new MessageService(
            messageRepository,
            participantRepository ?? Substitute.For<IConversationParticipantRepository>());
    }

    private static ConversationParticipant BuildParticipantEntity(Guid participantId, Guid conversationId)
    {
        return new ConversationParticipant
        {
            ConversationParticipantId = participantId,
            ConversationId = conversationId,
            UserId = Guid.NewGuid(),
            User = null!,
            ChatConversation = null!,
            LastAccess = DateTime.UtcNow
        };
    }


    private static readonly Guid ConversationId = Guid.NewGuid();
    private static readonly Guid SenderId       = Guid.NewGuid();
    private static readonly Guid MessageId      = Guid.NewGuid();

    private static readonly ChatConversation SampleConversation = new()
    {
        ConversationId = ConversationId,
        Name           = "General",
        IsGlobal       = false,
        CreatedAt      = DateTime.UtcNow
    };

    private static readonly Message SampleMessage = new()
    {
        MessageId      = MessageId,
        ConversationId = ConversationId,
        Conversation   = SampleConversation,
        SenderId       = SenderId,
        Sender         = new ConversationParticipant
        {
            ConversationParticipantId = Guid.NewGuid(),
            UserId           = SenderId,
            ConversationId   = ConversationId,
            ChatConversation = SampleConversation,
            User             = new User
            {
                Id = SenderId,
                Username = "alice",
                Email = "alice@test.com",
                PasswordHash = "hash",
                RegistrationTime = DateTime.Now,
                RefreshTokenHash = "refreshHash",
                RefreshTokenExpiryTime = DateTime.Now.AddDays(7)
            },
            LastAccess = DateTime.UtcNow
        },
        SentTime = DateTime.UtcNow,
        Content  = "Hello!"
    };

    private static readonly MessageDto SampleDto = new(
        MessageId, ConversationId, SenderId, "alice", SampleMessage.SentTime, "Hello!"
    );

    // ── GetAllMessagesByConversationId ────────────────────────────────────────

    [Fact]
    public async Task GetAllMessagesByConversationId_ShouldReturnMessages_WhenConversationHasMessages()
    {
        // given
        var repository = Substitute.For<IMessageRepository>();
        var sut = BuildSut(repository);

        repository.GetAllMessagesByConversationId(ConversationId)
            .Returns(new List<MessageDto> { SampleDto });

        // when
        var actual = (await sut.GetAllMessagesByConversationId(ConversationId))?.ToList();

        // then
        actual.Should().NotBeNull();
        actual.Should().HaveCount(1);
        actual.Should().Contain(msg => msg.Equals(SampleDto));
    }

    [Fact]
    public async Task GetAllMessagesByConversationId_ShouldReturnNull_WhenRepositoryReturnsNull()
    {
        // given
        var repository = Substitute.For<IMessageRepository>();
        var sut = BuildSut(repository);

        repository.GetAllMessagesByConversationId(ConversationId).ReturnsNull();

        // when
        var actual = await sut.GetAllMessagesByConversationId(ConversationId);

        // then
        actual.Should().BeNull();
    }

    [Fact]
    public async Task GetAllMessagesByConversationId_ShouldReturnEmptyCollection_WhenConversationHasNoMessages()
    {
        // given
        var repository = Substitute.For<IMessageRepository>();
        var sut = BuildSut(repository);

        repository.GetAllMessagesByConversationId(ConversationId)
            .Returns(new List<MessageDto>());

        // when
        var actual = (await sut.GetAllMessagesByConversationId(ConversationId))?.ToList();

        // then
        actual.Should().NotBeNull();
        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllMessagesByConversationId_ShouldMapDtoCorrectly_WhenConversationHasMessages()
    {
        // given
        var repository = Substitute.For<IMessageRepository>();
        var sut = BuildSut(repository);

        repository.GetAllMessagesByConversationId(ConversationId)
            .Returns(new List<MessageDto> { SampleDto });

        // when
        var actual = (await sut.GetAllMessagesByConversationId(ConversationId))!.First();

        // then
        actual.MessageId.Should().Be(SampleMessage.MessageId);
        actual.ConversationId.Should().Be(SampleMessage.ConversationId);
        actual.SenderId.Should().Be(SampleMessage.SenderId);
        actual.Content.Should().Be(SampleMessage.Content);
    }

    // ── GetMessageById ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMessageById_ShouldReturnMessageDto_WhenMessageExists()
    {
        // given
        var repository = Substitute.For<IMessageRepository>();
        var sut = BuildSut(repository);

        repository.GetMessageById(MessageId).Returns(SampleDto);

        // when
        var actual = await sut.GetMessageById(MessageId);

        // then
        actual.Should().NotBeNull();
        actual.MessageId.Should().Be(MessageId);
    }

    [Fact]
    public async Task GetMessageById_ShouldReturnNull_WhenMessageDoesNotExist()
    {
        // given
        var repository = Substitute.For<IMessageRepository>();
        var sut = BuildSut(repository);

        repository.GetMessageById(MessageId).Returns((MessageDto?)null);

        // when
        var actual = await sut.GetMessageById(MessageId);

        // then
        actual.Should().BeNull();
    }

    // ── SaveMessage ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveMessage_ShouldReturnMessageDto_WhenMessageIsSaved()
    {
        // given
        var repository = Substitute.For<IMessageRepository>();
        var participantRepository = Substitute.For<IConversationParticipantRepository>();
        var sut = BuildSut(repository, participantRepository);

        participantRepository
            .GetParticipantEntityByIdAsync((Guid)SampleDto.SenderId!)
            .Returns(BuildParticipantEntity((Guid)SampleDto.SenderId!, SampleDto.ConversationId));

        repository.Save(Arg.Any<Message>()).Returns(SampleDto);

        // when
        var actual = await sut.SaveMessage(SampleDto);

        // then
        actual.Should().NotBeNull();
        actual.Content.Should().Be(SampleDto.Content);
    }

    [Fact]
    public async Task SaveMessage_ShouldCallRepositoryOnce_WhenSavingMessage()
    {
        // given
        var repository = Substitute.For<IMessageRepository>();
        var participantRepository = Substitute.For<IConversationParticipantRepository>();
        var sut = BuildSut(repository, participantRepository);

        participantRepository
            .GetParticipantEntityByIdAsync((Guid)SampleDto.SenderId!)
            .Returns(BuildParticipantEntity((Guid)SampleDto.SenderId!, SampleDto.ConversationId));

        repository.Save(Arg.Any<Message>()).Returns(SampleDto);

        // when
        await sut.SaveMessage(SampleDto);

        // then
        await repository.Received(1).Save(Arg.Any<Message>());
    }

    [Fact]
    public async Task SaveMessage_ShouldKeepEmptyId_WhenMessageIdIsEmpty()
    {
        // given
        var repository = Substitute.For<IMessageRepository>();
        var participantRepository = Substitute.For<IConversationParticipantRepository>();
        var sut = BuildSut(repository, participantRepository);

        var dtoWithEmptyId = SampleDto with { MessageId = Guid.Empty };

        repository.Save(Arg.Any<Message>())
            .Returns(callInfo => 
            {
                var msg = callInfo.Arg<Message>();
                return new MessageDto(msg.MessageId, msg.ConversationId, msg.SenderId, "alice", msg.SentTime, msg.Content);
            });

        participantRepository
            .GetParticipantEntityByIdAsync((Guid)dtoWithEmptyId.SenderId!)
            .Returns(BuildParticipantEntity((Guid)dtoWithEmptyId.SenderId!, dtoWithEmptyId.ConversationId));

        // when
        var actual = await sut.SaveMessage(dtoWithEmptyId);

        // then
        actual.Should().NotBeNull();
        // The service should leave it Empty so EF Core handles it in the repository
        actual.MessageId.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task SaveMessage_ShouldReturnNull_WhenSenderDoesNotExist()
    {
        // given
        var repository = Substitute.For<IMessageRepository>();
        var participantRepository = Substitute.For<IConversationParticipantRepository>();
        var sut = BuildSut(repository, participantRepository);

        participantRepository.GetParticipantEntityByIdAsync((Guid)SampleDto.SenderId!).Returns((ConversationParticipant?)null);

        // when
        var actual = await sut.SaveMessage(SampleDto);

        // then
        actual.Should().BeNull();
        await repository.DidNotReceive().Save(Arg.Any<Message>());
    }

    [Fact]
    public async Task SaveMessage_ShouldReturnNull_WhenSenderIsNotPartOfConversation()
    {
        // given
        var repository = Substitute.For<IMessageRepository>();
        var participantRepository = Substitute.For<IConversationParticipantRepository>();
        var sut = BuildSut(repository, participantRepository);

        participantRepository
            .GetParticipantEntityByIdAsync((Guid)SampleDto.SenderId!)
            .Returns(BuildParticipantEntity((Guid)SampleDto.SenderId!, Guid.NewGuid()));

        // when
        var actual = await sut.SaveMessage(SampleDto);

        // then
        actual.Should().BeNull();
        await repository.DidNotReceive().Save(Arg.Any<Message>());
    }

    // ── DeleteMessage ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteMessage_ShouldReturnTrue_WhenMessageIsDeleted()
    {
        // given
        var repository = Substitute.For<IMessageRepository>();
        var sut = BuildSut(repository);

        repository.Delete(MessageId).Returns(true);

        // when
        var actual = await sut.DeleteMessage(MessageId);

        // then
        actual.Should().BeTrue();
        await repository.Received(1).Delete(MessageId);
    }

    [Fact]
    public async Task DeleteMessage_ShouldReturnFalse_WhenMessageDoesNotExist()
    {
        // given
        var messageId = Guid.NewGuid();
        var repository = Substitute.For<IMessageRepository>();
        var sut = BuildSut(repository);

        repository.Delete(messageId).Returns(false);

        // when
        var actual = await sut.DeleteMessage(messageId);

        // then
        actual.Should().BeFalse();
        await repository.Received(1).Delete(messageId);
    }
}