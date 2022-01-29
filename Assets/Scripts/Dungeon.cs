using System;
using System.Linq;
using RogueSharp;
using RogueSharp.MapCreation;
using UnityEngine;

/// <summary>
/// Represents an environment (a map + items).
/// </summary>
class Dungeon
{
    public readonly Vector3Int exitPosition;
    public readonly Vector3Int playerSpawnPosition;

    readonly Item[,] items;
    readonly Map map;
    readonly RandomNumberGenerator rng;

    public Cell this[Vector3Int position] => map[position.x, position.y];

    public Dungeon(string gameCode, int width, int height, int maxRooms, int roomMaxSize, int roomMinSize)
    {
        items = new Item[width, height];
        rng = new RandomNumberGenerator(gameCode);

        var mapCreationStrategy = new RandomRoomsMapCreationStrategy<Map>(
            width,
            height,
            maxRooms,
            roomMaxSize,
            roomMinSize,
            rng);

        map = Map.Create(mapCreationStrategy);

        // Four corners inside the walls.
        var bottomLeft = new Vector3Int(1, 1);
        var bottomRight = new Vector3Int(map.Width - 2, 1);
        var topLeft = new Vector3Int(1, map.Height - 2);
        var topRight = new Vector3Int(map.Width - 2, map.Height - 2);

        var playerSpawnPositionPossibilities = new[] { bottomLeft, bottomRight, topLeft, topRight };
        playerSpawnPosition = playerSpawnPositionPossibilities[rng.Next(playerSpawnPositionPossibilities.Length)];

        // Choose the corner opposite the player spawn position for the exit.
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

    public override string ToString()
    {
        // RogueSharp's map origin starts in the top-left while Unity's starts at the bottom-left,
        // so we need to flip its output to see a representation that matches our tilemap.
        var rows = map.ToString().Split(Environment.NewLine).Reverse();
        return string.Join(Environment.NewLine, rows);
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
}
