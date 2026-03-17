using FluentAssertions;
using JetBrains.Annotations;
using NSubstitute;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;
using SignalFlowBackend.Repository;
using SignalFlowBackend.Service;
using Xunit;

namespace SignalFlow.Backend.Service;

[TestSubject(typeof(ConversationParticipantService))]
public class ConversationParticipantServiceTest
{
    [Fact]
    public async Task GetParticipantByIdAsync_ShouldReturnParticipant_WhenParticipantExists()
    {
        // given
        var participantId = Guid.NewGuid();
        var expected = new ConversationParticipantDto(participantId, "alice", DateTime.UtcNow);
        var repository = Substitute.For<IConversationParticipantRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var sut = new ConversationParticipantService(repository, userRepository);

        repository.GetParticipantByIdAsync(participantId).Returns(expected);

        // when
        var actual = await sut.GetParticipantByIdAsync(participantId);

        // then
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task GetAllParticipantsByConversationIdAsync_ShouldReturnParticipants_WhenConversationHasParticipants()
    {
        // given
        var conversationId = Guid.NewGuid();
        var expected = new List<ConversationParticipantDto>
        {
            new(Guid.NewGuid(), "alice", DateTime.UtcNow),
            new(Guid.NewGuid(), "bob", DateTime.UtcNow)
        };
        var repository = Substitute.For<IConversationParticipantRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var sut = new ConversationParticipantService(repository, userRepository);

        repository.GetAllParticipantByConversationId(conversationId).Returns(expected);

        // when
        var actual = (await sut.GetAllParticipantsByConversationIdAsync(conversationId)).ToList();

        // then
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SaveParticipantAsync_ShouldReturnExistingParticipant_WhenParticipantAlreadyExists()
    {
        // given
        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var existingParticipant = new ConversationParticipantDto(Guid.NewGuid(), "alice", DateTime.UtcNow);
        var repository = Substitute.For<IConversationParticipantRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var sut = new ConversationParticipantService(repository, userRepository);

        repository.GetParticipantByUserIdAndConversationId(userId, conversationId).Returns(existingParticipant);

        // when
        var actual = await sut.SaveParticipantAsync(userId, conversationId);

        // then
        actual.Should().Be(existingParticipant);
        await userRepository.DidNotReceive().FindUserEntityByIdAsync(Arg.Any<Guid>());
        await repository.DidNotReceive().SaveAsync(Arg.Any<ConversationParticipant>());
    }

    [Fact]
    public async Task SaveParticipantAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // given
        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var repository = Substitute.For<IConversationParticipantRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var sut = new ConversationParticipantService(repository, userRepository);

        repository.GetParticipantByUserIdAndConversationId(userId, conversationId).Returns((ConversationParticipantDto?)null);
        userRepository.FindUserEntityByIdAsync(userId).Returns((User?)null);

        // when
        var actual = await sut.SaveParticipantAsync(userId, conversationId);

        // then
        actual.Should().BeNull();
        await repository.DidNotReceive().SaveAsync(Arg.Any<ConversationParticipant>());
    }

    [Fact]
    public async Task SaveParticipantAsync_ShouldSaveNewParticipant_WhenParticipantDoesNotExist()
    {
        // given
        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "alice",
            Email = "alice@test.com",
            PasswordHash = "hash",
            RegistrationTime = DateTime.UtcNow,
            RefreshTokenHash = "refresh-hash",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };
        var expected = new ConversationParticipantDto(Guid.NewGuid(), user.Username, DateTime.UtcNow);
        var repository = Substitute.For<IConversationParticipantRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var sut = new ConversationParticipantService(repository, userRepository);

        repository.GetParticipantByUserIdAndConversationId(userId, conversationId).Returns((ConversationParticipantDto?)null);
        userRepository.FindUserEntityByIdAsync(userId).Returns(user);
        repository.SaveAsync(Arg.Any<ConversationParticipant>()).Returns(expected);
        var before = DateTime.UtcNow;

        // when
        var actual = await sut.SaveParticipantAsync(userId, conversationId);
        var after = DateTime.UtcNow;

        // then
        actual.Should().Be(expected);
        await repository.Received(1).SaveAsync(Arg.Is<ConversationParticipant>(participant =>
            participant.UserId == userId &&
            participant.User == user &&
            participant.ConversationId == conversationId &&
            participant.LastAccess >= before &&
            participant.LastAccess <= after));
    }

    [Fact]
    public async Task DeleteParticipantAsync_ShouldReturnTrue_WhenParticipantIsDeleted()
    {
        // given
        var participantId = Guid.NewGuid();
        var repository = Substitute.For<IConversationParticipantRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var sut = new ConversationParticipantService(repository, userRepository);

        repository.DeleteAsync(participantId).Returns(true);

        // when
        var actual = await sut.DeleteParticipantAsync(participantId);

        // then
        actual.Should().BeTrue();
        await repository.Received(1).DeleteAsync(participantId);
    }

    [Fact]
    public async Task DeleteParticipantAsync_ShouldReturnFalse_WhenParticipantDoesNotExist()
    {
        // given
        var participantId = Guid.NewGuid();
        var repository = Substitute.For<IConversationParticipantRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var sut = new ConversationParticipantService(repository, userRepository);

        repository.DeleteAsync(participantId).Returns(false);

        // when
        var actual = await sut.DeleteParticipantAsync(participantId);

        // then
        actual.Should().BeFalse();
        await repository.Received(1).DeleteAsync(participantId);
    }
}

