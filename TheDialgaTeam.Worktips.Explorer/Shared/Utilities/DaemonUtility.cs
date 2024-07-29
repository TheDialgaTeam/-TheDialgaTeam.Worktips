namespace TheDialgaTeam.Worktips.Explorer.Shared.Utilities;

public static class DaemonUtility
{
    private static readonly string[] HashRateUnits = ["H/s", "KH/s", "MH/s", "GH/s", "TH/s", "PH/s"];
    private static readonly string[] DifficultyUnits = ["", "K", "M", "G", "T", "P"];

    public static string FormatHashRate(double hashRate, int decimalPlaces = 2)
    {
        var result = hashRate;
        var i = 0;

        while (result >= 1000)
        {
            result /= 1000;
            i++;
        }

        return $"{result.ToString($"N{decimalPlaces}")} {HashRateUnits[i]}";
    }

    public static string FormatHashRate(decimal hashRate, int decimalPlaces = 2)
    {
        var result = hashRate;
        var i = 0;

        while (result >= 1000)
        {
            result /= 1000;
            i++;
        }

        return $"{result.ToString($"N{decimalPlaces}")} {HashRateUnits[i]}";
    }

    public static string FormatDifficulty(ulong difficulty, int decimalPlaces = 2)
    {
        decimal result = difficulty;
        var i = 0;

        while (result >= 1000)
        {
            result /= 1000;
            i++;
        }

        return $"{result.ToString($"N{decimalPlaces}")} {DifficultyUnits[i]}";
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