using System.IO;
using Microsoft.Win32;

namespace LumiDesk.Helpers;

/// <summary>
/// 选择要添加快捷方式的文件或程序。
/// </summary>
public static class ShortcutPickDialogHelper
{
    public static string? PickFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择要添加的文件或程序",
            Filter = "所有文件 (*.*)|*.*|程序与快捷方式 (*.exe;*.lnk;*.url)|*.exe;*.lnk;*.url;*.bat;*.cmd;*.com;*.msc;*.msi|应用程序 (*.exe)|*.exe;*.com;*.bat;*.cmd|快捷方式 (*.lnk;*.url)|*.lnk;*.url",
            FilterIndex = 1,
            CheckFileExists = true,
            DereferenceLinks = false,
            Multiselect = false,
            InitialDirectory = GetInitialDirectory()
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static string GetInitialDirectory()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (Directory.Exists(desktop))
        {
            return desktop;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }
}
