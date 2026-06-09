using System.Windows;

namespace LumiDesk.Services;

public interface IHotkeyService
{
    void Initialize(Window window);
    bool RegisterHotkey(string hotkeyString, Action callback);
    void UnregisterHotkey(string hotkeyString);
    void UnregisterAll();
}