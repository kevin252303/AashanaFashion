using AashanaFashion.Models;

namespace AashanaFashion.Services;

public interface ICommunicationService
{
    Task<DocumentChatterViewModel> GetChatterViewModelAsync(string documentType, int documentId);
    Task<(bool Success, string Message, string? ExternalId)> SendWhatsAppAsync(SendCommunicationInputModel model, string? sentBy);
    Task<(bool Success, string Message, string? ExternalId)> SendEmailAsync(SendCommunicationInputModel model, string? sentBy);
    Task<(bool Success, string Message)> AddInternalNoteAsync(SendCommunicationInputModel model, string? sentBy);
    Task LogSystemActivityAsync(string documentType, int documentId, string documentReference, string title, string description, string? user = null);
}
