using System;
using System.Linq;
using RogueSharp;
using RogueSharp.MapCreation;
using RogueSharp.Random;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

class MapService : Services.Service
{
    /// <summary>
    /// Wraps Unity's random number generator for use with RogueSharp.
    /// </summary>
    class RandomNumberGenerator : IRandom
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

        public RandomState Save()
        {
            return new RandomState
            {
                NumberGenerated = timesUsed,
                Seed = new[] { seed },
            };
        }

        public void Restore(RandomState state)
        {
            seed = state.Seed[0];
            timesUsed = state.NumberGenerated;
            Random.InitState(seed);
        }
    }

    [SerializeField] Tilemap tilemap;
    [SerializeField] int maxRooms = 30;
    [SerializeField] int roomMaxSize = 10;
    [SerializeField] int roomMinSize = 3;

    [Header("Map Size")]
    [SerializeField] int width = 32;
    [SerializeField] int height = 32;

    [Header("Tiles")]
    [SerializeField] Tile exitTile;
    [SerializeField] Tile groundTile;
    [SerializeField] Tile notGroundTile;

    public Vector3Int playerSpawnPosition { get; private set; }

    Vector3Int exitPosition;
    Map map;
    RandomNumberGenerator rng;

    public bool CanMoveToTile(Vector3Int targetTilePosition)
    {
        // TODO
        return true;
    }

    public void GenerateMap(string gameCode)
    {
        rng = new RandomNumberGenerator(gameCode);

        var mapCreationStrategy = new RandomRoomsMapCreationStrategy<Map>(
            width,
            height,
            maxRooms,
            roomMaxSize,
            roomMinSize,
            rng);

        map = Map.Create(mapCreationStrategy);
        Debug.Log($"Generated map: \n{GetMapDebugString(map)}");
        PlacePlayerAndExit();
        SetTilesForMapCells();
    }

    void CarvePathToEmptyTile(Vector3Int position, Vector3Int direction)
    {
        var iteration = 0;

        while (!map[position.x, position.y].IsWalkable)
        {
            map[position.x, position.y].IsWalkable = true;

            if (iteration % 2 == 0)
            {
                position.x += direction.x;
            }
            else
            {
                position.y += direction.y;
            }

            iteration++;
        }
    }

    void PlacePlayerAndExit()
    {
        // Four corners.
        var bottomLeft = new Vector3Int(1, 1);
        var bottomRight = new Vector3Int(map.Width - 2, 1);
        var topLeft = new Vector3Int(1, map.Height - 2);
        var topRight = new Vector3Int(map.Width - 2, map.Height - 2);

        var playerSpawnPositionPossibilities = new[] { bottomLeft, bottomRight, topLeft, topRight };
        playerSpawnPosition = playerSpawnPositionPossibilities[rng.Next(playerSpawnPositionPossibilities.Length)];

        // Choose the corner opposite the player spawn point for the exit.
        exitPosition = playerSpawnPosition switch
        {
            _ when playerSpawnPosition == bottomLeft => topRight,
            _ when playerSpawnPosition == bottomRight => topLeft,
            _ when playerSpawnPosition == topLeft => bottomRight,
            _ when playerSpawnPosition == topRight => bottomLeft,
            _ => throw new ArgumentOutOfRangeException(),
        };

        Vector3Int GetPathCarveDirection(Vector3Int position)
        {
            return position switch
            {
                _ when position == bottomLeft => Vector3Int.up + Vector3Int.right,
                _ when position == bottomRight => Vector3Int.up + Vector3Int.left,
                _ when position == topLeft => Vector3Int.down + Vector3Int.right,
                _ when position == topRight => Vector3Int.down + Vector3Int.left,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        CarvePathToEmptyTile(playerSpawnPosition, GetPathCarveDirection(playerSpawnPosition));
        CarvePathToEmptyTile(exitPosition, GetPathCarveDirection(exitPosition));
    }

    void SetTilesForMapCells()
    {
        for (var x = 0; x < map.Width; x++)
        {
            for (var y = 0; y < map.Height; y++)
            {
                var cell = map[x, y];
                var tilePosition = new Vector3Int(x, y);
                tilemap.SetTile(tilePosition, cell.IsWalkable ? groundTile : notGroundTile);
            }
        }

        tilemap.SetTile(exitPosition, exitTile);
    }

    static string GetMapDebugString(Map map)
    {
        // RogueSharp's map origin starts in the top-left while Unity's starts at the bottom-left,
        // so we need to flip its output to see a representation that matches our tilemap.
        var rows = map.ToString().Split(Environment.NewLine).Reverse();
        return string.Join(Environment.NewLine, rows);
    }
}
