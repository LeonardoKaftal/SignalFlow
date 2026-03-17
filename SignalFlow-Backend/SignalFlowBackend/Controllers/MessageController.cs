using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Service;

namespace SignalFlowBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
[Authorize]
public class MessageController(IMessageService messageService) : ControllerBase
{
    [HttpGet("{messageId:guid}")]
    public async Task<ActionResult<MessageDto>> GetMessageById(Guid messageId)
    {
        var message = await messageService.GetMessageById(messageId);
        return message is null ? NotFound() : Ok(message);
    }

    [HttpGet("conversation/{conversationId:guid}")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetAllMessagesByConversationId(Guid conversationId)
    {
        var messages = await messageService.GetAllMessagesByConversationId(conversationId);
        return messages is null ? NotFound() : Ok(messages);
    }

    [HttpGet("conversation/{conversationId:guid}/participant/{participantId:guid}")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetAllMessagesByConversationIdAndParticipantId(
        Guid conversationId,
        Guid participantId)
    {
        var messages = await messageService.GetAllMessagesByConversationIdAndConversationParticipantId(conversationId, participantId);
        return messages is null ? NotFound() : Ok(messages);
    }

    [HttpGet("conversation/{conversationId:guid}/latest")]
    public async Task<ActionResult<MessageDto>> GetLatestMessageByConversationId(Guid conversationId)
    {
        var message = await messageService.GetLatestMessageByConversationId(conversationId);
        return message is null ? NotFound() : Ok(message);
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetAllMessagesByUserId(Guid userId)
    {
        var messages = await messageService.GetAllMessagesByUserId(userId);
        return messages is null ? NotFound() : Ok(messages);
    }

    [HttpPost]
    public async Task<ActionResult<MessageDto>> SaveMessage([FromBody] MessageDto messageDto)
    {
        var saved = await messageService.SaveMessage(messageDto);
        return saved is null ? BadRequest() : CreatedAtAction(nameof(GetMessageById), new { messageId = saved.MessageId }, saved);
    }

    [HttpPatch("{messageId:guid}")]
    public async Task<ActionResult<MessageDto>> UpdateMessage(Guid messageId, [FromBody] string newContent)
    {
        var updated = await messageService.UpdateMessage(messageId, newContent);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{messageId:guid}")]
    public async Task<ActionResult> DeleteMessage(Guid messageId)
    {
        var deleted = await messageService.DeleteMessage(messageId);
        return deleted ? NoContent() : NotFound();
    }
}

