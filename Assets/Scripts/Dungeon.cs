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
    public readonly Vector3Int entrancePosition;
    public readonly Vector3Int exitPosition;
    public readonly int[] glyphs;

    readonly List<Cell> emptyCells;
    readonly Item?[,] items;
    readonly Map map;
    readonly FieldOfView fov;
    readonly RandomNumberGenerator rng;

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
        emptyCells = map.GetAllCells().Where(cell => cell.IsWalkable).ToList();

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

        // Entrance position:

        var entrancePositionPossibilities = new[] { bottomLeft, bottomRight, topLeft, topRight };
        entrancePosition = entrancePositionPossibilities[rng.Next(entrancePositionPossibilities.Length)];
        CarvePathToEmptyTile(entrancePosition, GetPathCarveDirection(entrancePosition));
        RemoveEmptyCell(entrancePosition);

        // Exit position:

        // Choose the corner opposite the entrance for the exit.
        exitPosition = entrancePosition switch
        {
            _ when entrancePosition == bottomLeft => topRight,
            _ when entrancePosition == bottomRight => topLeft,
            _ when entrancePosition == topLeft => bottomRight,
            _ when entrancePosition == topRight => bottomLeft,
            _ => throw new ArgumentOutOfRangeException(),
        };

        CarvePathToEmptyTile(exitPosition, GetPathCarveDirection(exitPosition));
        SetItem(ItemType.Exit, exitPosition, PlayerType.Both);

        // Item positions:

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
            var cell = map[position.x, position.y];
            cell.IsTransparent = true;
            cell.IsWalkable = true;
            emptyCells.Add(cell);

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
            var cell = emptyCells[rng.Next(emptyCells.Count)];
            return new Vector3Int(cell.X, cell.Y);
        }
    }

    /// <summary>
    /// Finds an empty spot on the map that the player can walk around to reach the exit.
    /// </summary>
    Vector3Int GetRandomWalkablePositionThatPlayerCanCircumvent()
    {
        while (true)
        {
            using var temporaryMap = new PathfindingMap(map, items);
            var position = GetRandomWalkablePosition();
            map[position.x, position.y].IsWalkable = false;

            var pathFinder = new PathFinder(map);
            var entranceCell = map[entrancePosition.x, entrancePosition.y];
            var exitCell = map[exitPosition.x, exitPosition.y];
            var path = pathFinder.TryFindShortestPath(entranceCell, exitCell);

            if (path == null)
            {
                continue;
            }

            return position;
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
            var position = GetRandomWalkablePositionThatPlayerCanCircumvent();
            SetItem(ItemType.Pit, position, PlayerType.Player2);
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

    void RemoveEmptyCell(Vector3Int position)
    {
        var cell = map[position.x, position.y];
        emptyCells.Remove(cell);
    }

    void SetItem(ItemType itemType, Vector3Int position, PlayerType playerVisibility)
    {
        items[position.x, position.y] = new Item(itemType, playerVisibility);
        RemoveEmptyCell(position);
    }
}
