using RogueSharp;
using UnityEngine;

/// <summary>
/// Miscellaneous RogueSharp map utility functions.
/// </summary>
static class MapUtility
{
    public static Vector3Int GetRandomWalkablePosition(Map map, RandomNumberGenerator rng)
    {
        while (true)
        {
            var x = rng.Next(map.Width);
            var y = rng.Next(map.Height);

            if (map[x, y].IsWalkable)
            {
                return new Vector3Int(x, y);
            }
        }
    }
}
