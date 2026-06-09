using CommunityToolkit.Mvvm.ComponentModel;
using LumiDesk.Models;
using LumiDesk.Services;
using System;
using System.Threading.Tasks;

namespace LumiDesk.ViewModels;

public partial class NoteEditViewModel : ObservableObject
{
    private readonly INoteService _noteService;
    private readonly bool _isNew;
    private readonly int _noteId;
    private readonly DateTime _createdAt;
    private readonly string _color;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    public string WindowTitle => _isNew ? "新建便签" : "编辑便签";

    public NoteEditViewModel(INoteService noteService, NoteItem? note = null)
    {
        _noteService = noteService;

        if (note == null)
        {
            _isNew = true;
            _noteId = 0;
            _createdAt = DateTime.UtcNow;
            _color = "#FFFFFF";
            return;
        }

        _isNew = false;
        _noteId = note.Id;
        _createdAt = note.CreatedAt == default ? DateTime.UtcNow : note.CreatedAt;
        _color = string.IsNullOrWhiteSpace(note.Color) ? "#FFFFFF" : note.Color;
        Title = note.Title;
        Content = note.Content;
    }

    public async Task<bool> SaveAsync()
    {
        try
        {
            var now = DateTime.UtcNow;

            if (_isNew)
            {
                var note = new NoteItem
                {
                    Title = Title ?? string.Empty,
                    Content = Content ?? string.Empty,
                    Color = _color,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                var id = await _noteService.CreateNoteAsync(note);
                return id > 0;
            }

            var updated = new NoteItem
            {
                Id = _noteId,
                Title = Title ?? string.Empty,
                Content = Content ?? string.Empty,
                Color = _color,
                CreatedAt = _createdAt,
                UpdatedAt = now
            };

            await _noteService.UpdateNoteAsync(updated);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
