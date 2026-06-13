using UniDesk.Models;

namespace UniDesk.Services;

public interface IQuickNoteService
{
    Task<List<QuickNote>> GetAllQuickNotesAsync();
    Task<QuickNote?> GetQuickNoteAsync(int id);
    Task<int> CreateQuickNoteAsync(QuickNote note);
    Task UpdateQuickNoteAsync(QuickNote note);
    Task DeleteQuickNoteAsync(int id);
    Task SetPinnedAsync(int id, bool isPinned);
}
