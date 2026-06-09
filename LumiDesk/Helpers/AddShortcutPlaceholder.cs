namespace LumiDesk.Helpers;

/// <summary>
/// 快捷方式列表末尾「添加」占位标记，用于与真实快捷项共用 WrapPanel 布局。
/// </summary>
public sealed class AddShortcutPlaceholder
{
    public static readonly AddShortcutPlaceholder Instance = new();

    private AddShortcutPlaceholder()
    {
    }
}
