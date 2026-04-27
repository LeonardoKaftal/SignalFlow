using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Service;
using System.Security.Claims;

namespace SignalFlowBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
[Authorize]
public class MessageController(
    IMessageService messageService,
    IConversationParticipantService conversationParticipantService) : ControllerBase
{
    private Guid? GetCurrentUserId()
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(rawUserId, out var userId) ? userId : null;
    }

    [HttpGet("{messageId:guid}")]
    public async Task<ActionResult<MessageDto>> GetMessageById(Guid messageId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var message = await messageService.GetMessageById(messageId);

        if (message is null)
            return NotFound();

        var isParticipant = await conversationParticipantService
            .IsUserParticipantOfConversationAsync(currentUserId.Value, message.ConversationId);

        if (!isParticipant)
            return StatusCode(403, "You are not part of the conversation");

        return Ok(message);
    }

    [HttpGet("conversation/{conversationId:guid}")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetAllMessagesByConversationId(Guid conversationId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var isParticipant = await conversationParticipantService
            .IsUserParticipantOfConversationAsync(currentUserId.Value, conversationId);

        if (!isParticipant)
            return StatusCode(403, "You are not part of the conversation");

        var messages = await messageService.GetAllMessagesByConversationId(conversationId);
        return messages is null ? NotFound() : Ok(messages);
    }

    [HttpGet("conversation/{conversationId:guid}/participant/{participantId:guid}")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetAllMessagesByConversationIdAndParticipantId(
        Guid conversationId,
        Guid participantId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var isParticipant = await conversationParticipantService
            .IsUserParticipantOfConversationAsync(currentUserId.Value, conversationId);

        if (!isParticipant)
            return StatusCode(403, "You are not part of the conversation");

        var messages = await messageService.GetAllMessagesByConversationIdAndConversationParticipantId(conversationId, participantId);
        return messages is null ? NotFound() : Ok(messages);
    }

    [HttpGet("conversation/{conversationId:guid}/latest")]
    public async Task<ActionResult<MessageDto>> GetLatestMessageByConversationId(Guid conversationId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var isParticipant = await conversationParticipantService
            .IsUserParticipantOfConversationAsync(currentUserId.Value, conversationId);

        if (!isParticipant)
            return StatusCode(403, "You are not part of the conversation");

        var message = await messageService.GetLatestMessageByConversationId(conversationId);
        return message is null ? NotFound() : Ok(message);
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetAllMessagesByUserId(Guid userId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        if (currentUserId.Value != userId)
            return StatusCode(403, "You can only read your own messages");

        var messages = await messageService.GetAllMessagesByUserId(userId);
        if (messages is null)
            return NotFound();

        return Ok(messages);
    }
    
    // sending and receiving message is handled by SignalR hub, useful for testing
    /*[HttpPost]
    public async Task<ActionResult<MessageDto>> SaveMessage([FromBody] MessageDto messageDto)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var isParticipant = await conversationParticipantService
            .IsUserParticipantOfConversationAsync(currentUserId.Value, messageDto.ConversationId);

        if (!isParticipant)
            return StatusCode(403, "You are not part of the conversation"); 
        
        var saved = await messageService.SaveMessage(messageDto);
        if (saved is null)
        {
            return BadRequest("Invalid message payload: sender must be a conversation participant" +
                              " of the specified conversation.");
        }

        return CreatedAtAction(nameof(GetMessageById), new { messageId = saved.MessageId }, saved);
    }*/

    [HttpPatch("{messageId:guid}")]
    public async Task<ActionResult<MessageDto>> UpdateMessage(Guid messageId, [FromBody] string newContent)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var existingMessage = await messageService.GetMessageById(messageId);
        if (existingMessage is null)
            return NotFound();

        var isParticipant = await conversationParticipantService
            .IsUserParticipantOfConversationAsync(currentUserId.Value, existingMessage.ConversationId);

        if (!isParticipant)
            return StatusCode(403, "You are not part of the conversation");

        var updated = await messageService.UpdateMessage(messageId, newContent);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{messageId:guid}")]
    public async Task<ActionResult> DeleteMessage(Guid messageId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var existingMessage = await messageService.GetMessageById(messageId);
        if (existingMessage is null)
            return NotFound();

        var isParticipant = await conversationParticipantService
            .IsUserParticipantOfConversationAsync(currentUserId.Value, existingMessage.ConversationId);

        if (!isParticipant)
            return StatusCode(403, "You are not part of the conversation");

        var deleted = await messageService.DeleteMessage(messageId);
        return deleted ? NoContent() : NotFound();
    }
}

