using LumiDesk.Models;

namespace LumiDesk.Services;

public interface INoteService
{
    Task<List<NoteItem>> GetAllNotesAsync();
    Task<NoteItem?> GetNoteAsync(int id);
    Task<int> CreateNoteAsync(NoteItem note);
    Task UpdateNoteAsync(NoteItem note);
    Task DeleteNoteAsync(int id);
}
