using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Service;

namespace SignalFlowBackend.Hub;

[Authorize]
public sealed class ChatHub(
    IConversationService conversationService,
    IMessageService messageService,
    IConversationParticipantService conversationParticipantService) : Microsoft.AspNetCore.SignalR.Hub
{
    private Guid UserId => Guid.Parse(
        Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private HashSet<Guid> UserConversations =>
        (HashSet<Guid>)Context.Items["Conversations"]!;

    public override async Task OnConnectedAsync()
    {
        var conversationList = await conversationService
            .GetAllConversationsByUserIdAsync(UserId);

        var conversationDtos = conversationList.ToList();

        var joinTasks = conversationDtos.Select(c =>
            Groups.AddToGroupAsync(Context.ConnectionId, c.ConversationId.ToString()));

        await Task.WhenAll(joinTasks);

        Context.Items["Conversations"] = conversationDtos
            .Select(c => c.ConversationId)
            .ToHashSet();

        await base.OnConnectedAsync();
    }

    public async Task<bool> JoinConversation(Guid conversationId)
    {
        var found = 
            await conversationParticipantService
                .GetParticipantByUserIdAndConversationIdAsync(UserId, conversationId);

        // not authorized
        if (found is null) return false;

        UserConversations.Add(conversationId);
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());

        return true;
    }

    public async Task<bool> SendMessage(MessageDto message)
    {
        if (!UserConversations.Contains(message.ConversationId))
            return false;

        var saved = await messageService.SaveMessage(message);
        if (saved is null) return false;

        await Clients
            .Group(message.ConversationId.ToString())
            .SendAsync("MessageReceived", saved);

        return true;
    }

    public async Task ExitConversation(Guid conversationId)
    {
        UserConversations.Remove(conversationId);
        await Groups
            .RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());
    }
}