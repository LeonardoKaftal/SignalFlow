using FluentAssertions;
using JetBrains.Annotations;
using NSubstitute;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;
using SignalFlowBackend.Repository;
using SignalFlowBackend.Role;
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
        var expected = new ConversationParticipantDto(participantId, "alice", ConversationParticipantRole.Regular, DateTime.UtcNow);
        var repository = Substitute.For<IConversationParticipantRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var conversationRepository = Substitute.For<IConversationRepository>();
        var sut = new ConversationParticipantService(repository, userRepository, conversationRepository);

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
            new(Guid.NewGuid(), "alice", ConversationParticipantRole.Regular, DateTime.UtcNow),
            new(Guid.NewGuid(), "bob", ConversationParticipantRole.Regular, DateTime.UtcNow)
        };
        var repository = Substitute.For<IConversationParticipantRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var conversationRepository = Substitute.For<IConversationRepository>();
        var sut = new ConversationParticipantService(repository, userRepository, conversationRepository);

        repository.GetAllParticipantsByConversationId(conversationId).Returns(expected);

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
        var existingParticipant = new ConversationParticipantDto(Guid.NewGuid(), "alice", ConversationParticipantRole.Regular, DateTime.UtcNow);
        var repository = Substitute.For<IConversationParticipantRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var conversationRepository = Substitute.For<IConversationRepository>();
        var sut = new ConversationParticipantService(repository, userRepository, conversationRepository);

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
        var conversationRepository = Substitute.For<IConversationRepository>();
        var sut = new ConversationParticipantService(repository, userRepository, conversationRepository);

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
        var expected = new ConversationParticipantDto(Guid.NewGuid(), user.Username, ConversationParticipantRole.Regular, DateTime.UtcNow);
        var repository = Substitute.For<IConversationParticipantRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var conversationRepository = Substitute.For<IConversationRepository>();
        var sut = new ConversationParticipantService(repository, userRepository, conversationRepository);

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
        var conversationId = Guid.NewGuid();
        var participant = new ConversationParticipant
        {
            ConversationParticipantId = participantId,
            ConversationId = conversationId,
            Role = ConversationParticipantRole.Admin,
            LastAccess = DateTime.UtcNow
        };
        var repository = Substitute.For<IConversationParticipantRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var conversationRepository = Substitute.For<IConversationRepository>();
        var sut = new ConversationParticipantService(repository, userRepository, conversationRepository);

        repository.GetParticipantEntityByIdAsync(participantId).Returns(participant);
        repository.DeleteAsync(participantId).Returns(true);

        conversationRepository.GetConversationByIdAsync(conversationId).Returns(new ChatConversationDto(
            conversationId,
            "test",
            false,
            DateTime.UtcNow
        ));

        repository.GetAllAdminsByConversationId(conversationId).Returns(new List<ConversationParticipantDto>
        {
            new(participantId, "alice", ConversationParticipantRole.Admin, participant.LastAccess)
        });

        // when
        var actual = await sut.DeleteParticipantAsync(participantId, conversationId, participantId);

        // then
        actual.Should().BeTrue();
        await repository.Received(1).DeleteAsync(participantId);
    }

    [Fact]
    public async Task DeleteParticipantAsync_ShouldReturnFalse_WhenParticipantDoesNotExist()
    {
        // given
        var participantId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var repository = Substitute.For<IConversationParticipantRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var conversationRepository = Substitute.For<IConversationRepository>();
        var sut = new ConversationParticipantService(repository, userRepository, conversationRepository);

        repository.GetParticipantEntityByIdAsync(participantId).Returns((ConversationParticipant?)null);

        // when
        var actual = await sut.DeleteParticipantAsync(participantId, conversationId, Guid.NewGuid());

        // then
        actual.Should().BeFalse();
        await repository.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task SaveParticipantAsync_ShouldReturnNull_WhenRequesterIsNotAdmin()
    {
        // given
        var requesterParticipantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var repository = Substitute.For<IConversationParticipantRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var conversationRepository = Substitute.For<IConversationRepository>();
        var sut = new ConversationParticipantService(repository, userRepository, conversationRepository);

        repository.GetParticipantEntityByIdAsync(requesterParticipantId).Returns(new ConversationParticipant
        {
            ConversationParticipantId = requesterParticipantId,
            ConversationId = conversationId,
            Role = ConversationParticipantRole.Regular,
            LastAccess = DateTime.UtcNow
        });

        // when
        var actual = await sut.SaveParticipantAsync(userId, conversationId, requesterParticipantId);

        // then
        actual.Should().BeNull();
        await repository.DidNotReceive().SaveAsync(Arg.Any<ConversationParticipant>());
    }

    [Fact]
    public async Task DeleteParticipantAsync_ShouldReturnFalse_WhenRequesterIsNotAdmin()
    {
        // given
        var requesterParticipantId = Guid.NewGuid();
        var targetParticipantId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var repository = Substitute.For<IConversationParticipantRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var conversationRepository = Substitute.For<IConversationRepository>();
        var sut = new ConversationParticipantService(repository, userRepository, conversationRepository);

        repository.GetParticipantEntityByIdAsync(targetParticipantId).Returns(new ConversationParticipant
        {
            ConversationParticipantId = targetParticipantId,
            ConversationId = conversationId,
            Role = ConversationParticipantRole.Regular,
            LastAccess = DateTime.UtcNow
        });

        repository.GetParticipantEntityByIdAsync(requesterParticipantId).Returns(new ConversationParticipant
        {
            ConversationParticipantId = requesterParticipantId,
            ConversationId = conversationId,
            Role = ConversationParticipantRole.Regular,
            LastAccess = DateTime.UtcNow
        });

        // when
        var actual = await sut.DeleteParticipantAsync(targetParticipantId, conversationId, requesterParticipantId);

        // then
        actual.Should().BeFalse();
        await repository.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task AddAdministratorToConversation_ShouldReturnFalse_WhenRequesterIsNotAdmin()
    {
        // given
        var requesterParticipantId = Guid.NewGuid();
        var targetParticipantId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var repository = Substitute.For<IConversationParticipantRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var conversationRepository = Substitute.For<IConversationRepository>();
        var sut = new ConversationParticipantService(repository, userRepository, conversationRepository);

        var target = new ConversationParticipant
        {
            ConversationParticipantId = targetParticipantId,
            ConversationId = conversationId,
            Role = ConversationParticipantRole.Regular,
            LastAccess = DateTime.UtcNow
        };

        repository.GetParticipantEntityByIdAsync(targetParticipantId).Returns(target);
        repository.GetParticipantEntityByIdAsync(requesterParticipantId).Returns(new ConversationParticipant
        {
            ConversationParticipantId = requesterParticipantId,
            ConversationId = conversationId,
            Role = ConversationParticipantRole.Regular,
            LastAccess = DateTime.UtcNow
        });

        // when
        var actual = await sut.AddAdministratorToConversation(targetParticipantId, conversationId, requesterParticipantId);

        // then
        actual.Should().BeFalse();
        target.Role.Should().Be(ConversationParticipantRole.Regular);
        await repository.DidNotReceive().SaveAsync(Arg.Any<ConversationParticipant>());
    }

    [Fact]
    public async Task AddAdministratorToConversation_ShouldReturnTrueAndPromote_WhenRequesterIsAdmin()
    {
        // given
        var requesterParticipantId = Guid.NewGuid();
        var targetParticipantId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var repository = Substitute.For<IConversationParticipantRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var conversationRepository = Substitute.For<IConversationRepository>();
        var sut = new ConversationParticipantService(repository, userRepository, conversationRepository);

        var target = new ConversationParticipant
        {
            ConversationParticipantId = targetParticipantId,
            ConversationId = conversationId,
            Role = ConversationParticipantRole.Regular,
            LastAccess = DateTime.UtcNow
        };

        repository.GetParticipantEntityByIdAsync(targetParticipantId).Returns(target);
        repository.GetParticipantEntityByIdAsync(requesterParticipantId).Returns(new ConversationParticipant
        {
            ConversationParticipantId = requesterParticipantId,
            ConversationId = conversationId,
            Role = ConversationParticipantRole.Admin,
            LastAccess = DateTime.UtcNow
        });
        repository.SaveAsync(Arg.Any<ConversationParticipant>())
            .Returns(new ConversationParticipantDto(targetParticipantId, "target", ConversationParticipantRole.Admin, DateTime.UtcNow));

        // when
        var actual = await sut.AddAdministratorToConversation(targetParticipantId, conversationId, requesterParticipantId);

        // then
        actual.Should().BeTrue();
        target.Role.Should().Be(ConversationParticipantRole.Admin);
        await repository.Received(1).SaveAsync(target);
    }

    [Fact]
    public async Task DeleteParticipantAsync_ShouldReturnFalse_WhenLastAdminAttemptsToLeaveWithOtherParticipants()
    {
        // given
        var lastAdminParticipantId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var repository = Substitute.For<IConversationParticipantRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var conversationRepository = Substitute.For<IConversationRepository>();
        var sut = new ConversationParticipantService(repository, userRepository, conversationRepository);

        var lastAdminParticipant = new ConversationParticipant
        {
            ConversationParticipantId = lastAdminParticipantId,
            ConversationId = conversationId,
            Role = ConversationParticipantRole.Admin,
            LastAccess = DateTime.UtcNow
        };

        conversationRepository.GetConversationByIdAsync(conversationId).Returns(new ChatConversationDto(
            conversationId,
            "test",
            false,
            DateTime.UtcNow
        ));
        repository.GetParticipantEntityByIdAsync(lastAdminParticipantId).Returns(lastAdminParticipant);
        repository.GetAllAdminsByConversationId(conversationId).Returns(new List<ConversationParticipantDto>
        {
            new(lastAdminParticipantId, "alice", ConversationParticipantRole.Admin, lastAdminParticipant.LastAccess)
        });
        repository.GetNumberOfParticipantsByConversationId(conversationId).Returns(2); // At least 2 participants

        // when
        var actual = await sut.DeleteParticipantAsync(lastAdminParticipantId, conversationId, lastAdminParticipantId);

        // then
        actual.Should().BeNull();
        await repository.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task DeleteParticipantAsync_ShouldReturnTrue_WhenLastAdminLeavesAndIsTheOnlyParticipant()
    {
        // given
        var lastAdminParticipantId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var repository = Substitute.For<IConversationParticipantRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var conversationRepository = Substitute.For<IConversationRepository>();
        var sut = new ConversationParticipantService(repository, userRepository, conversationRepository);

        var lastAdminParticipant = new ConversationParticipant
        {
            ConversationParticipantId = lastAdminParticipantId,
            ConversationId = conversationId,
            Role = ConversationParticipantRole.Admin,
            LastAccess = DateTime.UtcNow
        };

        conversationRepository.GetConversationByIdAsync(conversationId).Returns(new ChatConversationDto(
            conversationId,
            "test",
            false,
            DateTime.UtcNow
        ));
        conversationRepository.DeleteAsync(conversationId).Returns(true);
        repository.GetParticipantEntityByIdAsync(lastAdminParticipantId).Returns(lastAdminParticipant);
        repository.GetAllAdminsByConversationId(conversationId).Returns(new List<ConversationParticipantDto>
        {
            new(lastAdminParticipantId, "alice", ConversationParticipantRole.Admin, lastAdminParticipant.LastAccess)
        });
        repository.GetNumberOfParticipantsByConversationId(conversationId).Returns(1); // Only 1 participant
        repository.DeleteAsync(lastAdminParticipantId).Returns(true);

        // when
        var actual = await sut.DeleteParticipantAsync(lastAdminParticipantId, conversationId, lastAdminParticipantId);

        // then
        actual.Should().BeTrue();
        await repository.Received(1).DeleteAsync(lastAdminParticipantId);
    }
}
