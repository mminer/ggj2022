using System.Globalization;
using UnityEngine;

public static class GameCodeUtility
{
    const int maxSeed = 65_536; // 16^4 so that all seeds can be represented by 4 character hex string

    public static string GenerateGameCode()
    {
        var seed = Random.Range(0, maxSeed);
        Random.InitState(seed);
        var hexCode = seed.ToString("X4");
        return ConvertHexToUnambiguousCharacters(hexCode);
    }

    public static int GetSeedFromGameCode(string gameCode)
    {
        var hexCode = ConvertUnambiguousCharactersToHex(gameCode);
        return int.Parse(hexCode, NumberStyles.HexNumber);
    }

    private static string ConvertHexToUnambiguousCharacters(string hexCode)
    {
        var unambiguous = hexCode.Replace('0', 'X');
        unambiguous = unambiguous.Replace('B', 'Y');
        unambiguous = unambiguous.Replace('5', 'W');
        return unambiguous;
    }

    private static string ConvertUnambiguousCharactersToHex(string unambiguous)
    {
        var hexCode = unambiguous.Replace('X', '0');
        hexCode = hexCode.Replace('Y', 'B');
        hexCode = hexCode.Replace('W', '5');
        return hexCode;
    }
}
