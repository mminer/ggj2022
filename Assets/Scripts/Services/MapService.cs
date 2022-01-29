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

    public Vector3Int playerSpawnPoint { get; private set; }

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

        // Set tiles for map cells.
        for (var x = 0; x < map.Width; x++)
        {
            for (var y = 0; y < map.Height; y++)
            {
                var cell = map[x, y];
                var tilePosition = new Vector3Int(x, y);
                tilemap.SetTile(tilePosition, cell.IsWalkable ? groundTile : notGroundTile);
            }
        }

        // Four corners.
        var bottomLeft = new Vector3Int(1, 1);
        var bottomRight = new Vector3Int(map.Width - 2, 1);
        var topLeft = new Vector3Int(1, map.Height - 2);
        var topRight = new Vector3Int(map.Width - 2, map.Height - 2);

        var playerSpawnPointPossibilities = new[] { bottomLeft, bottomRight, topLeft, topRight };
        playerSpawnPoint = playerSpawnPointPossibilities[rng.Next(playerSpawnPointPossibilities.Length)];

        // Choose the corner opposite the player spawn point for the exit.
        var exitPosition = playerSpawnPoint switch
        {
            _ when playerSpawnPoint == bottomLeft => topRight,
            _ when playerSpawnPoint == bottomRight => topLeft,
            _ when playerSpawnPoint == topLeft => bottomRight,
            _ when playerSpawnPoint == topRight => bottomLeft,
            _ => throw new ArgumentOutOfRangeException(),
        };

        tilemap.SetTile(exitPosition, exitTile);

        // TODO: carve path from spawn point
    }

    static string GetMapDebugString(Map map)
    {
        // RogueSharp's map origin starts in the top-left while Unity's starts at the bottom-left,
        // so we need to flip its output to see a representation that matches our tilemap.
        var rows = map.ToString().Split(Environment.NewLine).Reverse();
        return string.Join(Environment.NewLine, rows);
    }
}
