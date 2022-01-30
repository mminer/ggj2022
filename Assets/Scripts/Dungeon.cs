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
    readonly GoalMap goalMap;
    readonly Ground[,] ground;
    readonly Map map;
    readonly FieldOfView fov;
    public Dungeon.Light[] lights { private set; get; }
    readonly RandomNumberGenerator rng;

    // Four corners inside the walls.
    Vector3Int bottomLeft => new(1, 1);
    Vector3Int bottomRight => new(map.Width - 2, 1);
    Vector3Int topLeft => new(1, map.Height - 2);
    Vector3Int topRight => new(map.Width - 2, map.Height - 2);

    public struct Light {
        private FieldOfView fov;
        public readonly Vector3Int point;
        public readonly int radius;

        public Light(Vector3Int point, int radius, Map map) {
            fov = new FieldOfView(map);
            this.radius = radius;
            this.point = point;
            fov.ComputeFov(point.x, point.y, radius, true);
        }

        public bool isVisible(Vector3Int at) {
            return fov.IsInFov(at.x, at.y);
        }
    }

    public (bool isWalkable, Ground? ground) this[Vector3Int position] => (
        map[position.x, position.y].IsWalkable,
        ground[position.x, position.y]);

    public Dungeon(string gameCode, int width, int height, int maxRooms, int roomMaxSize, int roomMinSize, Dictionary<ItemType, int> itemCounts, int maxLights)
    {
        ground = new Ground[width, height];
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
        emptyCells = new List<Cell>();
        goalMap = new GoalMap(map);

        foreach(var cell in map.GetAllCells()) {
            SetGround(new Vector3Int(cell.X, cell.Y, 0), cell.IsWalkable ? GroundType.Grass : GroundType.Wall, null);
        }

        // Entrance position.
        entrancePosition = GetRandomCornerPosition();
        CarvePathToEmptyTile(entrancePosition, GetPathCarveDirection(entrancePosition));
        RemoveEmptyCell(entrancePosition);

        var riverStart = GetRandomCornerPosition();
        CarveRiver(riverStart, GetPathCarveDirection(riverStart));
        CarveRiver(riverStart, GetPathCarveDirection(riverStart));

        // Exit position.
        do
        {
            exitPosition = GetRandomCornerPosition();
        } while (exitPosition == entrancePosition);

        CarvePathToEmptyTile(exitPosition, GetPathCarveDirection(exitPosition));
        var item = new Item(ItemType.Exit, exitPosition, PlayerType.Both);
        SetGround(exitPosition, GroundType.Grass, item);
        goalMap.AddGoal(exitPosition.x, exitPosition.y, 1);

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

        PlaceLights(rng.Next(maxLights));
    }

    public override string ToString()
    {
        // RogueSharp's map origin starts in the top-left while Unity's starts at the bottom-left,
        // so we need to flip its output to see a representation that matches our tilemap.
        var rows = map.ToString().Split(Environment.NewLine).Reverse();
        return string.Join(Environment.NewLine, rows);
    }

    public void UpdateMovableItems()
    {
        foreach (var (item, position) in EnumerateItems())
        {
            if (item.itemType != ItemType.Monster)
            {
                continue;
            }

            if (position != item.originalPosition)
            {
                ground[item.originalPosition.x, item.originalPosition.y].item = item;
                ground[position.x, position.y].item = null;
            }
            else
            {
                var adjacentCells = map
                    .GetAdjacentCells(position.x, position.y)
                    .Where(cell => cell.IsWalkable)
                    .ToArray();

                var randomAdjacentCell = adjacentCells[rng.Next(adjacentCells.Length)];

                if (ground[randomAdjacentCell.X, randomAdjacentCell.Y].item.HasValue)
                {
                    continue;
                }

                ground[item.originalPosition.x, item.originalPosition.y].item = null;
                ground[randomAdjacentCell.X, randomAdjacentCell.Y].item = item;
            }
        }
    }

    void CarvePathToEmptyTile(Vector3Int position, Vector3Int direction)
    {
        var iteration = 0;

        while (!map[position.x, position.y].IsWalkable)
        {
            var cell = map[position.x, position.y];
            cell.IsTransparent = true;
            cell.IsWalkable = true;

            SetGround(position, GroundType.Grass, null);

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

    void CarveRiver(Vector3Int position, Vector3Int direction)
    {
        var iteration = 0;

        while (iteration < 100 && position.x < map.Width && position.x >= 0 && position.y < map.Height && position.y >= 0)
        {
            var cell = map[position.x, position.y];

            if(!cell.IsWalkable){
                cell.IsTransparent = true;
                SetGround(position, GroundType.Water, null);
            }

            position.x += rng.Next(2) * direction.x;
            position.y += rng.Next(2) * direction.y;

            iteration++;
        }
    }

    IEnumerable<(Item item, Vector3Int position)> EnumerateItems()
    {
        for (var x = 0; x < map.Width; x++)
        {
            for (var y = 0; y < map.Height; y++)
            {
                var item = ground[x, y].item;

                if (!item.HasValue)
                {
                    continue;
                }

                var position = new Vector3Int(x, y);
                yield return (item.Value, position);
            }
        }
    }

    Vector3Int GetPathCarveDirection(Vector3Int cornerPosition)
    {
        // Diagonal toward the opposite corner.
        return cornerPosition switch
        {
            _ when cornerPosition == bottomLeft => Vector3Int.up + Vector3Int.right,
            _ when cornerPosition == bottomRight => Vector3Int.up + Vector3Int.left,
            _ when cornerPosition == topLeft => Vector3Int.down + Vector3Int.right,
            _ when cornerPosition == topRight => Vector3Int.down + Vector3Int.left,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    Vector3Int GetRandomCornerPosition()
    {
        var cornerPositions = new[] { bottomLeft, bottomRight, topLeft, topRight };
        return cornerPositions[rng.Next(cornerPositions.Length)];
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
            var position = GetRandomWalkablePosition();

            var obstacles = EnumerateItems()
                .Where(entry => entry.item.itemType != ItemType.Exit)
                .Select(entry => new Point(entry.position.x, entry.position.y))
                .Concat(new[] { new Point(position.x, position.y) });

            goalMap.ClearObstacles();
            goalMap.AddObstacles(obstacles);
            var path = goalMap.TryFindPath(entrancePosition.x, entrancePosition.y);

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
        var placedMonsters = 0;

        while (placedMonsters < monsterCount)
        {
            var position = GetRandomWalkablePositionThatPlayerCanCircumvent();
            var visibleToPlayer1 = rng.NextBool();
            var item = new Item(ItemType.Monster, position, visibleToPlayer1 ? PlayerType.Player1 : PlayerType.Player2);
            SetGround(position, GroundType.Grass, item);
            placedMonsters++;
        }
    }

    void PlacePits(int pitCount)
    {
        var placedPits = 0;

        while (placedPits < pitCount)
        {
            var position = GetRandomWalkablePositionThatPlayerCanCircumvent();
            var visibleToPlayer1 = rng.NextBool();
            var item = new Item(ItemType.Pit, position, visibleToPlayer1 ? PlayerType.Player1 : PlayerType.Player2);
            SetGround(position, GroundType.Grass, item);
            placedPits++;
        }
    }

    void PlaceWeapons(int weaponCount)
    {
        throw new NotImplementedException();
    }

    private void PlaceLights(int lightCount)
    {
        lights = new Dungeon.Light[] {};

        var placedLights = 0;

        while (placedLights < lightCount)
        {
            var position = GetRandomWalkablePosition();
            var light = new Dungeon.Light(position, rng.Next(5) + 2, map);

            lights = lights.Append(light).ToArray();

            placedLights++;
        }
    }

    public void RegenerateVisible(Vector3Int at, int radius)
    {
        fov.ComputeFov(at.x, at.y, radius, true);
    }

    public bool isVisible(Vector3Int at)
    {
        return fov.IsInFov(at.x, at.y);
    }

    void RemoveEmptyCell(Vector3Int position)
    {
        var cell = map[position.x, position.y];
        if(cell != null) emptyCells.Remove(cell);
    }

    void SetGround(Vector3Int position, GroundType groundType, Item? item)
    {
        var cell = map.GetCell(position.x, position.y);
        ground[cell.X, cell.Y] = new Ground(groundType, item);

        RemoveEmptyCell(position);
        if(item == null && cell.IsWalkable) { emptyCells.Add(cell); }
    }

}
