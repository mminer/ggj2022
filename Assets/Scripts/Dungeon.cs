using System;
using System.Collections.Generic;
using System.Linq;
using RogueSharp;
using RogueSharp.MapCreation;
using UnityEngine;

/// <summary>
/// Represents an environment (a map + items).
/// </summary>
public class Dungeon
{
    public readonly Vector3Int playerSpawnPosition;
    public readonly int[] glyphs;

    readonly Item?[,] items;
    readonly Map map;
    readonly FieldOfView fov;
    readonly RandomNumberGenerator rng;
    readonly Cell[] walkableCells;

    public (bool isWalkable, Item? item) this[Vector3Int position] => (
        map[position.x, position.y].IsWalkable,
        items[position.x, position.y]);

    public Dungeon(string gameCode, int width, int height, int maxRooms, int roomMaxSize, int roomMinSize, Dictionary<ItemType, int> itemCounts)
    {
        items = new Item?[width, height];
        rng = new RandomNumberGenerator(gameCode);

        // Glyphs
        var maxGlyphIndex = Services.Get<UIService>().GlyphSpriteCount() - 1;
        glyphs = new int[]
        {
            // Skip index 0 because it's a blank glyph and that's no fun
            rng.Next(1, maxGlyphIndex),
            rng.Next(1, maxGlyphIndex)
        };

        Debug.Log($"Required glyphs: {glyphs[0]}, {glyphs[1]}");

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
        var bottomRight = new Vector3Int(width - 2, 1);
        var topLeft = new Vector3Int(1, height - 2);
        var topRight = new Vector3Int(width - 2, height - 2);

        Vector3Int GetPathCarveDirection(Vector3Int position)
        {
            // Diagonal away from the wall.
            return position switch
            {
                _ when position == bottomLeft => Vector3Int.up + Vector3Int.right,
                _ when position == bottomRight => Vector3Int.up + Vector3Int.left,
                _ when position == topLeft => Vector3Int.down + Vector3Int.right,
                _ when position == topRight => Vector3Int.down + Vector3Int.left,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        // Player spawn position:

        var playerSpawnPositionPossibilities = new[] { bottomLeft, bottomRight, topLeft, topRight };
        playerSpawnPosition = playerSpawnPositionPossibilities[rng.Next(playerSpawnPositionPossibilities.Length)];
        CarvePathToEmptyTile(playerSpawnPosition, GetPathCarveDirection(playerSpawnPosition));

        // Exit position:

        // Choose the corner opposite the player spawn position for the exit.
        var exitPosition = playerSpawnPosition switch
        {
            _ when playerSpawnPosition == bottomLeft => topRight,
            _ when playerSpawnPosition == bottomRight => topLeft,
            _ when playerSpawnPosition == topLeft => bottomRight,
            _ when playerSpawnPosition == topRight => bottomLeft,
            _ => throw new ArgumentOutOfRangeException(),
        };

        items[exitPosition.x, exitPosition.y] = new Item(ItemType.Exit, Player.Both);
        CarvePathToEmptyTile(exitPosition, GetPathCarveDirection(exitPosition));

        // Item positions:

        // Cache for later to speed up finding positions to place items.
        walkableCells = map.GetAllCells().Where(cell => cell.IsWalkable).ToArray();

        foreach (var kvp in itemCounts)
        {
            switch (kvp.Key)
            {
                case ItemType.Door:
                    PlaceDoors(kvp.Value);
                    break;

                case ItemType.Key:
                    PlaceKeys(kvp.Value);
                    break;

                case ItemType.Monster:
                    PlaceMonsters(kvp.Value);
                    break;

                case ItemType.Pit:
                    PlacePits(kvp.Value);
                    break;

                case ItemType.Weapon:
                    PlaceWeapons(kvp.Value);
                    break;
            }
        }

        fov = new FieldOfView(map);
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
            map[position.x, position.y].IsTransparent = true;

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

    Vector3Int GetRandomWalkablePosition()
    {
        while (true)
        {
            var cell = walkableCells[rng.Next(walkableCells.Length)];
            return new Vector3Int(cell.X, cell.Y);
        }
    }

    void PlaceDoors(int doorCount)
    {
        throw new NotImplementedException();
    }

    void PlaceKeys(int keyCount)
    {
        throw new NotImplementedException();
    }

    void PlaceMonsters(int monsterCount)
    {
        throw new NotImplementedException();
    }

    void PlacePits(int pitCount)
    {
        var placedPits = 0;

        while (placedPits < pitCount)
        {
            var position = GetRandomWalkablePosition();

            // Don't place a pit where another item has been placed.
            if (items[position.x, position.y].HasValue)
            {
                continue;
            }

            // Ensure it's possible to walk around a pit.
            var isPassable = map
                .GetAdjacentCells(position.x, position.y, true)
                .All(cell => cell.IsWalkable && !items[cell.X, cell.Y].HasValue);

            if (!isPassable)
            {
                continue;
            }

            items[position.x, position.y] = new Item(ItemType.Pit, Player.Player2);
            placedPits++;
        }
    }

    void PlaceWeapons(int weaponCount)
    {
        throw new NotImplementedException();
    }

    public void RegenerateVisible(Vector3Int at, int radius) 
    {
        fov?.ComputeFov(at.x, at.y, radius, true).ToArray();
    }

    public bool isVisible(Vector3Int at) 
    {
        return fov.IsInFov(at.x, at.y);
    }
}
