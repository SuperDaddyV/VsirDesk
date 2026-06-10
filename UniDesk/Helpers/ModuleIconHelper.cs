using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace UniDesk.Helpers;

public static class ModuleIconHelper
{
    private static readonly Lazy<ImageSource> ShortcutHeaderIconLazy =
        new(() => LoadIcon("常规快捷方式-01.png"));

    private static readonly Lazy<ImageSource> TodoHeaderIconLazy =
        new(() => LoadIcon("pre_icon_待办事项.png"));

    public static ImageSource ShortcutHeaderIcon => ShortcutHeaderIconLazy.Value;

    public static ImageSource TodoHeaderIcon => TodoHeaderIconLazy.Value;

    private static ImageSource LoadIcon(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "icon", fileName);
        if (!File.Exists(path))
        {
            return new BitmapImage();
        }

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }
}
