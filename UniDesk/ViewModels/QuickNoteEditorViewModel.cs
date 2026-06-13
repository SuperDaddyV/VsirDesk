using System.Threading;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using UniDesk.Helpers;
using UniDesk.Models;
using UniDesk.Services;

namespace UniDesk.ViewModels;

public partial class QuickNoteEditorViewModel : ObservableObject, IDisposable
{
    private readonly IQuickNoteService _quickNoteService;
    private readonly INotificationService _notificationService;
    private readonly DispatcherTimer _saveTimer;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private readonly DateTime _createdAt;
    private readonly int _sortOrder;
    private bool _isLoading;
    private bool _isDeleted;
    private int _noteId;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private bool _isPinned;

    [ObservableProperty]
    private string _saveStatus = "自动保存";

    public string WindowTitle => _noteId > 0 ? "编辑便签" : "新建便签";

    public QuickNoteEditorViewModel(
        IQuickNoteService quickNoteService,
        INotificationService notificationService,
        QuickNote? note = null)
    {
        _quickNoteService = quickNoteService;
        _notificationService = notificationService;
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
        _saveTimer.Tick += SaveTimer_OnTick;

        _isLoading = true;
        try
        {
            if (note == null)
            {
                _createdAt = DateTime.UtcNow;
                _sortOrder = 0;
                return;
            }

            _noteId = note.Id;
            _createdAt = note.CreatedAt == default ? DateTime.UtcNow : note.CreatedAt;
            _sortOrder = note.SortOrder;
            Title = note.Title;
            Content = note.Content;
            IsPinned = note.IsPinned;
            SaveStatus = "已保存";
        }
        finally
        {
            _isLoading = false;
        }
    }

    partial void OnTitleChanged(string value) => ScheduleSave();

    partial void OnContentChanged(string value) => ScheduleSave();

    partial void OnIsPinnedChanged(bool value) => ScheduleSave();

    public async Task FlushAndCleanupAsync()
    {
        _saveTimer.Stop();
        await SaveNowAsync();

        if (!_isDeleted && _noteId > 0 && IsEmpty)
        {
            await _quickNoteService.DeleteQuickNoteAsync(_noteId);
            _noteId = 0;
        }
    }

    public async Task<bool> DeleteAsync()
    {
        if (!_notificationService.ShowConfirmDialog("确定删除这条便签？", "删除确认"))
        {
            return false;
        }

        _saveTimer.Stop();
        if (_noteId > 0)
        {
            await _quickNoteService.DeleteQuickNoteAsync(_noteId);
        }

        _isDeleted = true;
        _notificationService.ShowSuccessMessage("便签已删除。");
        return true;
    }

    public void CopyContent()
    {
        var text = string.IsNullOrWhiteSpace(Content) ? Title : Content;
        if (string.IsNullOrWhiteSpace(text))
        {
            _notificationService.ShowWarningMessage("便签内容为空。");
            return;
        }

        try
        {
            Clipboard.SetText(text);
            _notificationService.ShowSuccessMessage("已复制。");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "QuickNoteEditorViewModel.CopyContent");
            _notificationService.ShowWarningMessage("复制失败，请稍后重试。");
        }
    }

    private bool IsEmpty =>
        string.IsNullOrWhiteSpace(Title) &&
        string.IsNullOrWhiteSpace(Content);

    private void ScheduleSave()
    {
        if (_isLoading || _isDeleted)
        {
            return;
        }

        SaveStatus = "正在保存...";
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private async void SaveTimer_OnTick(object? sender, EventArgs e)
    {
        _saveTimer.Stop();
        await SaveNowAsync();
    }

    private async Task SaveNowAsync()
    {
        if (_isDeleted || IsEmpty)
        {
            SaveStatus = "自动保存";
            return;
        }

        await _saveLock.WaitAsync();
        try
        {
            var now = DateTime.UtcNow;
            if (_noteId <= 0)
            {
                var id = await _quickNoteService.CreateQuickNoteAsync(new QuickNote
                {
                    Title = Title ?? string.Empty,
                    Content = Content ?? string.Empty,
                    IsPinned = IsPinned,
                    SortOrder = _sortOrder,
                    CreatedAt = _createdAt,
                    UpdatedAt = now
                });

                if (id <= 0)
                {
                    SaveStatus = "保存失败";
                    return;
                }

                _noteId = id;
                OnPropertyChanged(nameof(WindowTitle));
            }
            else
            {
                await _quickNoteService.UpdateQuickNoteAsync(new QuickNote
                {
                    Id = _noteId,
                    Title = Title ?? string.Empty,
                    Content = Content ?? string.Empty,
                    IsPinned = IsPinned,
                    SortOrder = _sortOrder,
                    CreatedAt = _createdAt,
                    UpdatedAt = now
                });
            }

            SaveStatus = "已保存";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "QuickNoteEditorViewModel.SaveNowAsync");
            SaveStatus = "保存失败";
        }
        finally
        {
            _saveLock.Release();
        }
    }

    public void Dispose()
    {
        _saveTimer.Stop();
        _saveTimer.Tick -= SaveTimer_OnTick;
        _saveLock.Dispose();
    }
}
