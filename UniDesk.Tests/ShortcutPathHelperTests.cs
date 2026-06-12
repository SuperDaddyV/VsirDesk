using UniDesk.Helpers;
using UniDesk.Models;
using System;
using System.IO;

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

    [Fact]
    public void IsSupportedPath_ShouldReturnFalse_WhenPathDoesNotExist()
    {
        var path = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.txt");

        Assert.False(ShortcutPathHelper.IsSupportedPath(path));
    }

    [Fact]
    public void IsSupportedPath_ShouldReturnTrue_ForExistingFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"快捷方式测试-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, "test");

        try
        {
            Assert.True(ShortcutPathHelper.IsSupportedPath(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
