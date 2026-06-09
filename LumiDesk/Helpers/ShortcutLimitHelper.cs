namespace LumiDesk.Helpers;

public static class ShortcutLimitHelper
{
    public const int DefaultLimit = 9;
    public const int AbsoluteMax = 18;

    public static readonly int[] AllowedLimits = [6, 9, 12, 15, 18];

    public static int ParseLimit(string? value)
    {
        if (int.TryParse(value, out var limit) && AllowedLimits.Contains(limit))
        {
            return limit;
        }

        return DefaultLimit;
    }
}
