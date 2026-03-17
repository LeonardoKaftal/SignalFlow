using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Service;

namespace SignalFlowBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
[Authorize]
public class ConversationParticipantController(IConversationParticipantService conversationParticipantService) : ControllerBase
{
    
    [HttpGet("{participantId:guid}")]
    public async Task<ActionResult<ConversationParticipantDto>> GetParticipantById(Guid participantId)
    {
        var participant = await conversationParticipantService.GetParticipantByIdAsync(participantId);
        return participant is null ? NotFound() : Ok(participant);
    }

    [HttpGet("conversation/{conversationId:guid}")]
    public async Task<ActionResult<IEnumerable<ConversationParticipantDto>>> GetAllParticipantsByConversationId(Guid conversationId)
    {
        var participants = await conversationParticipantService.GetAllParticipantsByConversationIdAsync(conversationId);
        return Ok(participants);
    }

    [HttpGet("user/{userId:guid}/conversation/{conversationId:guid}")]
    public async Task<ActionResult<ConversationParticipantDto>> GetParticipantByUserIdAndConversationId(
        Guid userId,
        Guid conversationId)
    {
        var participant = await conversationParticipantService.GetParticipantByUserIdAndConversationIdAsync(userId, conversationId);
        return participant is null ? NotFound() : Ok(participant);
    }

    [HttpPost("user/{userId:guid}/conversation/{conversationId:guid}")]
    public async Task<ActionResult<ConversationParticipantDto>> SaveParticipant(Guid userId, Guid conversationId)
    {
        var saved = await conversationParticipantService.SaveParticipantAsync(userId, conversationId);
        if (saved is null)
            return NotFound("User not found");
        return CreatedAtAction(nameof(GetParticipantById), new { participantId = saved.ConversationParticipantId }, saved);
    }

    [HttpDelete("{participantId:guid}")]
    public async Task<ActionResult> DeleteParticipant(Guid participantId)
    {
        var deleted = await conversationParticipantService.DeleteParticipantAsync(participantId);
        return deleted ? NoContent() : NotFound();
    }
}

