namespace TheDialgaTeam.Worktips.Explorer.Shared;

public static class DaemonUtility
{
    private static readonly string[] Units = { "H/s", "KH/s", "MH/s", "GH/s", "TH/s", "PH/s" };

    public static string FormatHashrate(double hashrate, int decimalPlaces = 2)
    {
        var i = 0;
        
        while (hashrate >= 1000)
        {
            hashrate /= 1000;
            i++;
        }

        return $"{hashrate.ToString($"N{decimalPlaces}")} {Units[i]}";
    }

    public static string FormatAtomicUnit(ulong value, ulong atomicUnit)
    {
        if (atomicUnit == 0) return value.ToString("N0");

        var result = (decimal) value / atomicUnit;
        var decimalCount = 0;

        while (atomicUnit > 1)
        {
            atomicUnit /= 10;
            decimalCount++;
        }

        return result.ToString($"N{decimalCount}");
    }
}