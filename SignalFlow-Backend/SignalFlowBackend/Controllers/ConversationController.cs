using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Service;

namespace SignalFlowBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
[Authorize]
public class ConversationController(IConversationService conversationService) : ControllerBase
{
    [HttpGet("{conversationId:guid}")]
    public async Task<ActionResult<ChatConversationDto>> GetConversationById(Guid conversationId)
    {
        var conversation = await conversationService.GetConversationByIdAsync(conversationId);
        return conversation is null ? NotFound() : Ok(conversation);
    }

    [HttpGet("search/user/{userId:guid}/conversations")]
    public async Task<ActionResult<IEnumerable<ChatConversationDto>>> GetUserConversations(
        Guid userId,
        [FromQuery] string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            var conversations = await conversationService.GetAllConversationsByUserIdAsync(userId);
            return Ok(conversations);
        }

        var filtered = await conversationService.GetConversationsByNameAndUserIdAsync(name, userId);
        return Ok(filtered);
    }

    [HttpGet("search/participant/{participantId:guid}/conversations")]
    public async Task<ActionResult<ChatConversationDto>> GetConversationByNameAndParticipantId(Guid participantId, [FromQuery] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Name is required");

        var conversation = await conversationService.GetConversationByNameAndParticipantIdAsync(name, participantId);
        return conversation is null ? NotFound() : Ok(conversation);
    }

    [HttpGet("global")]
    public async Task<ActionResult<ChatConversationDto>> GetOrCreateGlobalConversation()
    {
        var conversation = await conversationService.GetOrCreateGlobalConversationAsync();
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
        var deleted = await conversationService.DeleteConversationAsync(conversationId);
        return deleted ? NoContent() : NotFound();
    }
}

