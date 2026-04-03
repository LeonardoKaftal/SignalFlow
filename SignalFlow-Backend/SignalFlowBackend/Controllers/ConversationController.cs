using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Service;
using System.Security.Claims;

namespace SignalFlowBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
[Authorize]
public class ConversationController(
    IConversationService conversationService,
    IConversationParticipantService conversationParticipantService) : ControllerBase
{
    private Guid? GetCurrentUserId()
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(rawUserId, out var userId) ? userId : null;
    }

    [HttpGet("{conversationId:guid}")]
    public async Task<ActionResult<ChatConversationDto>> GetConversationById(Guid conversationId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var isParticipant = await conversationParticipantService
            .IsUserParticipantOfConversationAsync(currentUserId.Value, conversationId);

        if (!isParticipant)
            return StatusCode(403, "You are not part of the conversation");

        var conversation = await conversationService.GetConversationByIdAsync(conversationId);
        return conversation is null ? NotFound() : Ok(conversation);
    }

    [HttpGet("search/user/{userId:guid}")]
    public async Task<ActionResult<IEnumerable<ChatConversationDto>>> GetUserConversations(
        Guid userId,
        [FromQuery] string? name)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        if (currentUserId.Value != userId)
            return StatusCode(403, "You are not part of the conversation");

        if (string.IsNullOrWhiteSpace(name))
        {
            var conversations = await conversationService.GetAllConversationsByUserIdAsync(userId);
            return Ok(conversations);
        }

        var filtered = await conversationService.GetConversationsByNameAndUserIdAsync(name, userId);
        return Ok(filtered);
    }

    [HttpGet("search/participant/{participantId:guid}")]
    public async Task<ActionResult<ChatConversationDto>> GetConversationByNameAndParticipantId(Guid participantId, [FromQuery] string name)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var targetParticipant = await conversationParticipantService.GetParticipantEntityByIdAsync(participantId);
        if (targetParticipant is null)
            return NotFound();

        var isParticipant = await conversationParticipantService
            .IsUserParticipantOfConversationAsync(currentUserId.Value, targetParticipant.ConversationId);

        if (!isParticipant)
            return StatusCode(403, "You are not part of the conversation");

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Name is required");

        var conversation = await conversationService.GetConversationByNameAndParticipantIdAsync(name, participantId);
        return conversation is null ? NotFound() : Ok(conversation);
    }

    [HttpGet("global")]
    public async Task<ActionResult<ChatConversationDto>> GetOrCreateGlobalConversation()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var conversation = await conversationService.GetOrCreateGlobalConversationAsync();

        if (conversation is null)
            return NotFound();

        var isParticipant = await conversationParticipantService
            .IsUserParticipantOfConversationAsync(currentUserId.Value, conversation.ConversationId);

        if (!isParticipant)
            return StatusCode(403, "You are not part of the conversation");

        return Ok(conversation);
    }

    [HttpPost]
    public async Task<ActionResult<ChatConversationDto>> CreateConversation([FromBody] CreateConversationRequestDto request)
    {
        var created = await conversationService.CreateConversationAsync(request.Name, request.UserIds);
        if (created is null)
            return BadRequest("The conversation already exist or you haven't provided at least 2 valid user IDs");
        return Ok(created);
    }

    [HttpDelete("{conversationId:guid}")]
    public async Task<ActionResult> DeleteConversation(Guid conversationId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var requesterParticipant = await conversationParticipantService
            .GetParticipantByUserIdAndConversationIdAsync(currentUserId.Value, conversationId);

        if (requesterParticipant is null)
            return StatusCode(403, "You are not part of the conversation or the conversation does not exist");

        var deleted = await conversationService
            .DeleteConversationAsync(conversationId, requesterParticipant.ConversationParticipantId);

        return deleted ? NoContent() : StatusCode(403, "Only admins can delete the conversation");
    }
}

