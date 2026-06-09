using LumiDesk.Models;
using System.Globalization;

namespace LumiDesk.Services;

public class NoteService : INoteService
{
    private readonly IDatabaseService _databaseService;

    public NoteService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public Task<List<NoteItem>> GetAllNotesAsync()
    {
        return GetAllNotesInternalAsync();
    }

    public Task<NoteItem?> GetNoteAsync(int id)
    {
        return GetNoteInternalAsync(id);
    }

    public Task<int> CreateNoteAsync(NoteItem note)
    {
        return CreateNoteInternalAsync(note);
    }

    public Task UpdateNoteAsync(NoteItem note)
    {
        return UpdateNoteInternalAsync(note);
    }

    public Task DeleteNoteAsync(int id)
    {
        return DeleteNoteInternalAsync(id);
    }

    private async Task<List<NoteItem>> GetAllNotesInternalAsync()
    {
        try
        {
            return await _databaseService.QueryAsync(
                "SELECT Id, Title, Content, Color, CreatedAt, UpdatedAt FROM Notes ORDER BY UpdatedAt DESC",
                MapNote
            );
        }
        catch
        {
            return new List<NoteItem>();
        }
    }

    private async Task<NoteItem?> GetNoteInternalAsync(int id)
    {
        try
        {
            return await _databaseService.QuerySingleAsync(
                "SELECT Id, Title, Content, Color, CreatedAt, UpdatedAt FROM Notes WHERE Id = @p0",
                MapNote,
                id
            );
        }
        catch
        {
            return null;
        }
    }

    private async Task<int> CreateNoteInternalAsync(NoteItem note)
    {
        try
        {
            var now = DateTime.UtcNow;
            var createdAt = note.CreatedAt == default ? now : note.CreatedAt;
            var updatedAt = note.UpdatedAt == default ? now : note.UpdatedAt;

            var title = note.Title ?? string.Empty;
            var content = note.Content ?? string.Empty;
            var color = string.IsNullOrWhiteSpace(note.Color) ? "#FFFFFF" : note.Color;

            var id = await _databaseService.QuerySingleAsync(
                "INSERT INTO Notes (Title, Content, Color, CreatedAt, UpdatedAt) VALUES (@p0, @p1, @p2, @p3, @p4) RETURNING Id",
                reader => reader.GetInt32(0),
                title,
                content,
                color,
                createdAt.ToString("o", CultureInfo.InvariantCulture),
                updatedAt.ToString("o", CultureInfo.InvariantCulture)
            );

            return id;
        }
        catch
        {
            return 0;
        }
    }

    private async Task UpdateNoteInternalAsync(NoteItem note)
    {
        try
        {
            var updatedAt = note.UpdatedAt == default ? DateTime.UtcNow : note.UpdatedAt;

            await _databaseService.ExecuteNonQueryAsync(
                "UPDATE Notes SET Title = @p0, Content = @p1, Color = @p2, UpdatedAt = @p3 WHERE Id = @p4",
                note.Title ?? string.Empty,
                note.Content ?? string.Empty,
                string.IsNullOrWhiteSpace(note.Color) ? "#FFFFFF" : note.Color,
                updatedAt.ToString("o", CultureInfo.InvariantCulture),
                note.Id
            );
        }
        catch
        {
        }
    }

    private async Task DeleteNoteInternalAsync(int id)
    {
        try
        {
            await _databaseService.ExecuteNonQueryAsync(
                "DELETE FROM Notes WHERE Id = @p0",
                id
            );
        }
        catch
        {
        }
    }

    private static NoteItem MapNote(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        var createdAtRaw = reader.IsDBNull(4) ? null : reader.GetString(4);
        var updatedAtRaw = reader.IsDBNull(5) ? null : reader.GetString(5);

        return new NoteItem
        {
            Id = reader.GetInt32(0),
            Title = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            Content = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            Color = reader.IsDBNull(3) ? "#FFFFFF" : reader.GetString(3),
            CreatedAt = ParseDateTimeOrDefault(createdAtRaw),
            UpdatedAt = ParseDateTimeOrDefault(updatedAtRaw)
        };
    }

    private static DateTime ParseDateTimeOrDefault(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return default;

        if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed))
        {
            return parsed;
        }

        return default;
    }
}
