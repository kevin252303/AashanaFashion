using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AashanaFashion.Controllers;

[Authorize]
public class CommunicationController : Controller
{
    private readonly ICommunicationService _communicationService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IEmailService _emailService;

    public CommunicationController(
        ICommunicationService communicationService,
        IWhatsAppService whatsAppService,
        IEmailService emailService)
    {
        _communicationService = communicationService;
        _whatsAppService = whatsAppService;
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<IActionResult> Chatter(string docType, int docId)
    {
        var vm = await _communicationService.GetChatterViewModelAsync(docType, docId);
        return PartialView("_DocumentChatter", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendWhatsApp([FromBody] SendCommunicationInputModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid communication parameters." });
        }

        var sentBy = User.Identity?.Name ?? "User";
        var result = await _communicationService.SendWhatsAppAsync(model, sentBy);

        return Json(new
        {
            success = result.Success,
            message = result.Message,
            externalId = result.ExternalId,
            redirectUrl = model.SendViaDirectApi ? null : result.ExternalId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendEmail([FromBody] SendCommunicationInputModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid communication parameters." });
        }

        var sentBy = User.Identity?.Name ?? "User";
        var result = await _communicationService.SendEmailAsync(model, sentBy);

        return Json(new
        {
            success = result.Success,
            message = result.Message,
            externalId = result.ExternalId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddInternalNote([FromBody] SendCommunicationInputModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Message))
        {
            return BadRequest(new { success = false, message = "Note cannot be empty." });
        }

        var sentBy = User.Identity?.Name ?? "User";
        var result = await _communicationService.AddInternalNoteAsync(model, sentBy);

        return Json(new
        {
            success = result.Success,
            message = result.Message
        });
    }
}
