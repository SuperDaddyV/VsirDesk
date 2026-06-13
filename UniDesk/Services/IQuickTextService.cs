using UniDesk.Models;

namespace UniDesk.Services;

public interface IQuickTextService
{
    Task<List<ClipboardHistoryItem>> GetClipboardHistoryAsync(int? limit = null);
    Task<List<TextSnippet>> GetTextSnippetsAsync();
    Task<bool> RecordClipboardTextAsync(string? text);
    Task DeleteClipboardHistoryAsync(int id);
    Task ClearClipboardHistoryAsync();
    Task TrimClipboardHistoryAsync(int maxCount);
    Task<int> CreateTextSnippetAsync(TextSnippet snippet);
    Task UpdateTextSnippetAsync(TextSnippet snippet);
    Task DeleteTextSnippetAsync(int id);
    Task<TextSnippet?> CreateSnippetFromHistoryAsync(ClipboardHistoryItem? item);
    Task MarkSnippetUsedAsync(int id);
}
