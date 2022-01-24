using System.Globalization;
using UnityEngine;

static class GameCodeUtility
{
    const int maxSeed = 65_536; // 16^4 so that all seeds can be represented by 4 character hex string

    public static void ApplyGameCode(string gameCode)
    {
        var seed = int.Parse(gameCode, NumberStyles.HexNumber);
        Random.InitState(seed);
    }

    public static string GenerateGameCode()
    {
        var seed = Random.Range(0, maxSeed);
        Random.InitState(seed);
        return seed.ToString("X4");
    }
}
