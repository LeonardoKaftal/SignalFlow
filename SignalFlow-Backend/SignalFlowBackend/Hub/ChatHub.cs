using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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

        foreach (var c in conversationDtos)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, c.ConversationId.ToString());
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

        return true;
    }

    public async Task ExitConversation(Guid conversationId)
    {
        UserConversations.Remove(conversationId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());
    }

    public async Task<bool> UpdateLastMessageRead(Guid conversationId)
    {
        var participant =
            await conversationParticipantService.GetParticipantEntityByUserIdAndConversationIdAsync(UserId, conversationId);
        if (participant is null) return false;
        
        var message = await messageService
            .GetLatestMessageByConversationId(conversationId);
        if (message is null) 
            return false;
        
        participant.LastMessageRead = message.MessageId;
        await conversationParticipantService.UpdateParticipantAsync(participant);
        return true;
    }
}