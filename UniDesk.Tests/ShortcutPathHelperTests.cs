using UniDesk.Helpers;
using UniDesk.Models;

namespace UniDesk.Tests;

public class ShortcutPathHelperTests
{
    [Theory]
    [InlineData(@"C:\Apps\test.exe", ShortcutType.Application)]
    [InlineData(@"C:\Users\Public\Desktop\app.lnk", ShortcutType.Application)]
    [InlineData(@"C:\Users\Public\Desktop\site.url", ShortcutType.Application)]
    [InlineData(@"C:\temp\notes.txt", ShortcutType.File)]
    public void CreateFromPath_ShouldDetectShortcutType(string path, ShortcutType expectedType)
    {
        var item = ShortcutPathHelper.CreateFromPath(path, 0);

        Assert.Equal(expectedType, item.Type);
        Assert.Equal(path, item.Path);
        Assert.False(string.IsNullOrWhiteSpace(item.Name));
    }
}
