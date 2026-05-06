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
    IConversationParticipantService conversationParticipantService,
    ActiveConnectionsTracker tracker) : Microsoft.AspNetCore.SignalR.Hub
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

        foreach (var c in conversationDtos)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, c.ConversationId.ToString());
            tracker.Add(c.ConversationId, Context.ConnectionId);
        }

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

        if (found is null) return false;

        UserConversations.Add(conversationId);
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
        tracker.Add(conversationId, Context.ConnectionId);

        return true;
    }

    public async Task ExitConversation(Guid conversationId)
    {
        UserConversations.Remove(conversationId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());
        tracker.Remove(conversationId, Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        foreach (var convId in UserConversations)
            tracker.Remove(convId, Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }
}