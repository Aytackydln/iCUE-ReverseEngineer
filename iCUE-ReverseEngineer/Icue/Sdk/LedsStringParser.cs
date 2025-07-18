namespace iCUE_ReverseEngineer.Icue.Sdk;

public static class LedsStringParser
{

    public static IcueLedColor ParseLedColor(ReadOnlySpan<char> ledEntry)
    {
        var comma1Index = ledEntry.IndexOf(',');
        var comma2Index = ledEntry[(comma1Index + 1)..].IndexOf(',') + comma1Index + 1;
        var comma3Index = ledEntry[(comma2Index + 1)..].IndexOf(',') + comma2Index + 1;

        // Extract LED ID and RGB values
        var ledId = ExtractInt(ledEntry, 0, comma1Index);
        var red = ExtractByte(ledEntry, comma1Index + 1, comma2Index);
        var green = ExtractByte(ledEntry, comma2Index + 1, comma3Index);
        var blue = ExtractByte(ledEntry, comma3Index + 1, ledEntry.Length);
        return new IcueLedColor((IcueLedId)ledId, red, green, blue);
    }

    private static int ExtractInt(ReadOnlySpan<char> valueSpan, int startIndex, int endIndex)
    {
        // Convert the value from string to int
        if (int.TryParse(valueSpan.Slice(startIndex, endIndex - startIndex), out var value))
        {
            return value;
        }

        // If parsing fails, return a default value (e.g., 0)
        return 0;
    }

    private static byte ExtractByte(ReadOnlySpan<char> valueSpan, int startIndex, int endIndex)
    {
        // Convert the value from string to int
        if (byte.TryParse(valueSpan.Slice(startIndex, endIndex - startIndex), out var value))
        {
            return value;
        }

        // If parsing fails, return a default value (e.g., 0)
        return 0;
    }
}