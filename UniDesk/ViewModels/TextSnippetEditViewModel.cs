using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniDesk.Models;
using UniDesk.Services;

namespace UniDesk.ViewModels;

public partial class TextSnippetEditViewModel : ObservableObject
{
    private readonly IQuickTextService _quickTextService;
    private readonly bool _isNew;
    private readonly int _id;
    private readonly DateTime _createdAt;
    private readonly int _sortOrder;
    private readonly int _useCount;
    private readonly DateTime? _lastUsedAt;

    public IReadOnlyList<string> Categories { get; } = ["默认", "工作", "开发", "生活", "AI"];

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private string _category = "默认";

    [ObservableProperty]
    private bool _isPinned;

    public string WindowTitle => _isNew ? "新增常用短语" : "编辑常用短语";

    public TextSnippetEditViewModel(IQuickTextService quickTextService, TextSnippet? snippet = null)
    {
        _quickTextService = quickTextService;

        if (snippet == null)
        {
            _isNew = true;
            _createdAt = DateTime.UtcNow;
            return;
        }

        _id = snippet.Id;
        _createdAt = snippet.CreatedAt == default ? DateTime.UtcNow : snippet.CreatedAt;
        _sortOrder = snippet.SortOrder;
        _useCount = snippet.UseCount;
        _lastUsedAt = snippet.LastUsedAt;
        Title = snippet.Title;
        Content = snippet.Content;
        Category = string.IsNullOrWhiteSpace(snippet.Category) ? "默认" : snippet.Category;
        IsPinned = snippet.IsPinned;
    }

    [RelayCommand]
    public async Task<bool> SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Content))
        {
            return false;
        }

        var now = DateTime.UtcNow;
        var snippet = new TextSnippet
        {
            Id = _id,
            Title = Title?.Trim() ?? string.Empty,
            Content = Content ?? string.Empty,
            Category = string.IsNullOrWhiteSpace(Category) ? "默认" : Category.Trim(),
            IsPinned = IsPinned,
            SortOrder = _sortOrder,
            UseCount = _useCount,
            CreatedAt = _createdAt,
            UpdatedAt = now,
            LastUsedAt = _lastUsedAt
        };

        if (_isNew)
        {
            return await _quickTextService.CreateTextSnippetAsync(snippet) > 0;
        }

        await _quickTextService.UpdateTextSnippetAsync(snippet);
        return true;
    }
}
