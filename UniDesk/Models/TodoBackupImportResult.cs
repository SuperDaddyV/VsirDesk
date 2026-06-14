namespace UniDesk.Models;

public sealed class TodoBackupImportResult
{
    public int SettingCount { get; set; }
    public int ShortcutCount { get; set; }
    public int TodoCount { get; set; }
    public int QuickNoteCount { get; set; }
    public int ClipboardHistoryCount { get; set; }
    public int TextSnippetCount { get; set; }
}
