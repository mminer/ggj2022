using RogueSharp.Random;
using UnityEngine;

/// <summary>
/// Wraps Unity's random number generator for use with RogueSharp.
/// </summary>
public class RandomNumberGenerator : IRandom
{
    int seed;
    long timesUsed;

    public RandomNumberGenerator(string gameCode)
    {
        seed = GameCodeUtility.GetSeedFromGameCode(gameCode);
        Random.InitState(seed);
    }

    public int Next(int maxValue)
    {
        return Random.Range(0, maxValue);
    }

    public int Next(int minValue, int maxValue)
    {
        return Random.Range(minValue, maxValue);
    }

    public void Restore(RandomState state)
    {
        seed = state.Seed[0];
        timesUsed = state.NumberGenerated;
        Random.InitState(seed);
    }

    public RandomState Save()
    {
        return new RandomState
        {
            NumberGenerated = timesUsed,
            Seed = new[] { seed },
        };
    }
}
