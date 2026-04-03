using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Service;
using System.Security.Claims;

namespace SignalFlowBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
[Authorize]
public class ConversationParticipantController(IConversationParticipantService conversationParticipantService) : ControllerBase
{
    private Guid? GetCurrentUserId()
     {
         var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
         return Guid.TryParse(rawUserId, out var userId) ? userId : null;
    }
    
    private async Task<ConversationParticipantDto?> GetCurrentParticipantAsync(Guid conversationId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return null;

        return await conversationParticipantService
            .GetParticipantByUserIdAndConversationIdAsync(currentUserId.Value, conversationId);
    }
    
    [HttpGet("{participantId:guid}")]
    public async Task<ActionResult<ConversationParticipantDto>> GetParticipantById(Guid participantId)
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

        var participant = await conversationParticipantService.GetParticipantByIdAsync(participantId);
        return participant is null ? NotFound() : Ok(participant);
    }

    [HttpGet("conversation/{conversationId:guid}")]
    public async Task<ActionResult<IEnumerable<ConversationParticipantDto>>> GetAllParticipantsByConversationId(Guid conversationId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var isParticipant = await conversationParticipantService
            .IsUserParticipantOfConversationAsync(currentUserId.Value, conversationId);

        if (!isParticipant)
            return StatusCode(403, "You are not part of the conversation");

        var participants = await conversationParticipantService.GetAllParticipantsByConversationIdAsync(conversationId);
        return Ok(participants);
    }

    [HttpGet("conversation/{conversationId:guid}/user/{userId:guid}")]
    public async Task<ActionResult<ConversationParticipantDto>> GetParticipantByUserIdAndConversationId(
        Guid userId,
        Guid conversationId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var isParticipant = await conversationParticipantService
            .IsUserParticipantOfConversationAsync(currentUserId.Value, conversationId);

        if (!isParticipant)
            return StatusCode(403, "You are not part of the conversation");

        var participant = await conversationParticipantService.GetParticipantByUserIdAndConversationIdAsync(userId, conversationId);
        return participant is null ? NotFound() : Ok(participant);
    }

    [HttpPost("conversation/{conversationId:guid}/user/{userId:guid}")]
    public async Task<ActionResult<ConversationParticipantDto>> SaveParticipant(Guid userId, Guid conversationId)
    {
        var requesterParticipant = await GetCurrentParticipantAsync(conversationId);
        if (requesterParticipant is null)
            return StatusCode(403, "You are not part of the conversation");

        var saved = await conversationParticipantService
            .SaveParticipantAsync(userId, conversationId, requesterParticipant.ConversationParticipantId);

        if (saved is null)
            return StatusCode(403, "Only admins can add participants or target user was not found");

        return CreatedAtAction(nameof(GetParticipantById), new { participantId = saved.ConversationParticipantId }, saved);
    }

    [HttpPut("conversation/{conversationId:guid}/participant/{participantId:guid}/admin")]
    public async Task<ActionResult> PromoteParticipantToAdmin(Guid participantId, Guid conversationId)
    {
        var requesterParticipant = await GetCurrentParticipantAsync(conversationId);
        if (requesterParticipant is null)
            return StatusCode(403, "You are not part of the conversation");

        var promoted = await conversationParticipantService.AddAdministratorToConversation(
            participantId,
            conversationId,
            requesterParticipant.ConversationParticipantId);

        return promoted
            ? NoContent()
            : StatusCode(403, "Only admins can promote participants or participant was not found");
    }

    [HttpDelete("/conversation/{conversationId:guid}/participant/{participantId:guid}")]
    public async Task<ActionResult> DeleteParticipant(Guid participantId, Guid conversationId)
    {
        var requesterParticipant = await GetCurrentParticipantAsync(conversationId);
        if (requesterParticipant is null)
            return StatusCode(403, "You are not part of the conversation");

        var deleted = await conversationParticipantService
            .DeleteParticipantAsync(participantId, conversationId, requesterParticipant.ConversationParticipantId);

        return deleted ? NoContent() : StatusCode(403, "Only admins can remove participants or participant was not found");
    }
}

