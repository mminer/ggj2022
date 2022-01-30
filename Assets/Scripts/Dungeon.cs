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
    public class Cell : RogueSharp.Cell
    {
        public GroundType GroundType { get; set; }
        public Item Item { get; set; }
        public Vector3Int Position => new(X, Y);
    }

    public readonly Vector3Int entrancePosition;
    public readonly int[] glyphs;
    public Light[] lights { private set; get; }

    readonly FieldOfView<Cell> fov;
    readonly GoalMap<Cell> goalMap;
    readonly Map<Cell> map;
    readonly RandomNumberGenerator rng;

    IEnumerable<Cell> cellsWithItems => map.GetAllCells().Where(cell => cell.Item != null);

    // Four corners inside the walls.
    Vector3Int bottomLeft => new(1, 1);
    Vector3Int bottomRight => new(map.Width - 2, 1);
    Vector3Int topLeft => new(1, map.Height - 2);
    Vector3Int topRight => new(map.Width - 2, map.Height - 2);

    public struct Light {
        private FieldOfView<Cell> fov;
        public readonly Vector3Int point;
        public readonly int radius;

        public Light(Vector3Int point, int radius, Map<Cell> map) {
            fov = new FieldOfView<Cell>(map);
            this.radius = radius;
            this.point = point;
            fov.ComputeFov(point.x, point.y, radius, true);
        }

        public bool isVisible(Vector3Int at) {
            return fov.IsInFov(at.x, at.y);
        }
    }

    public Cell this[Vector3Int position] => map[position];

    public Dungeon(string gameCode, int width, int height, int maxRooms, int roomMaxSize, int roomMinSize, Dictionary<ItemType, int> itemCounts, int maxLights)
    {
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

        var mapCreationStrategy = new RandomRoomsMapCreationStrategy<Map<Cell>, Cell>(
            width,
            height,
            maxRooms,
            roomMaxSize,
            roomMinSize,
            rng);

        map = Map.Create(mapCreationStrategy);
        goalMap = new GoalMap<Cell>(map);

        foreach (var cell in map.GetAllCells())
        {
            cell.GroundType = cell.IsWalkable ? GroundType.Grass : GroundType.Wall;
        }

        // Entrance position.
        entrancePosition = GetRandomCornerPosition();
        CarvePathToEmptyTile(entrancePosition, GetPathCarveDirection(entrancePosition));

        var riverStart = GetRandomCornerPosition();
        CarveRiver(riverStart, GetPathCarveDirection(riverStart));
        CarveRiver(riverStart, GetPathCarveDirection(riverStart));

        // Exit position.

        Vector3Int exitPosition;

        do
        {
            exitPosition = GetRandomCornerPosition();
        } while (exitPosition == entrancePosition);

        CarvePathToEmptyTile(exitPosition, GetPathCarveDirection(exitPosition));
        goalMap.AddGoal(exitPosition.x, exitPosition.y, 1);

        var exitCell = map[exitPosition];
        exitCell.GroundType = GroundType.Grass;
        exitCell.Item = new Item(ItemType.Exit, PlayerType.Both);

        // Item positions:

        foreach (var (itemType, count) in itemCounts)
        {
            switch (itemType)
            {
                case ItemType.Monster:
                    PlaceMonsters(count);
                    break;

                case ItemType.Pit:
                    PlacePits(count);
                    break;

                case ItemType.Monument:
                    PlaceMonument();
                    break;
            }
        }

        fov = new FieldOfView<Cell>(map);

        PlaceLights(rng.Next(maxLights));
    }

    public override string ToString() => map.ToString();

    public void UpdateMovableItems()
    {
        var movableCells = cellsWithItems
            .Where(cell => cell.Item.movementDirection != Vector3Int.zero)
            .ToArray();

        foreach (var cell in movableCells)
        {
            var destinationCell = map[cell.Position + cell.Item.movementDirection];

            // If the destination is a wall or an item, try going the opposite way.
            if (!IsCellEmpty(destinationCell))
            {
                cell.Item.ReverseMovementDirection();
                destinationCell = map[cell.Position + cell.Item.movementDirection];
            }

            // Still a wall or another item? If so, stay put.
            if (!IsCellEmpty(destinationCell))
            {
                continue;
            }

            if (cell.Y == 24)
            {
                Debug.Log($"MOVE {destinationCell.X}; {cell.X}");
            }

            destinationCell.Item = cell.Item;
            cell.Item = null;
        }
    }

    void CarvePathToEmptyTile(Vector3Int position, Vector3Int direction)
    {
        var iteration = 0;

        while (!map.IsWalkable(position))
        {
            var cell = map[position];
            cell.GroundType = GroundType.Grass;
            cell.IsTransparent = true;
            cell.IsWalkable = true;

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
            if (!map.IsWalkable(position))
            {
                var cell = map[position];
                cell.GroundType = GroundType.Water;
                cell.IsTransparent = true;
            }

            position.x += rng.Next(2) * direction.x;
            position.y += rng.Next(2) * direction.y;

            iteration++;
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

    Cell GetRandomEmptyCell()
    {
        var emptyCells = map.GetAllCells().Where(IsCellEmpty).ToArray();
        return emptyCells[rng.Next(emptyCells.Length)];
    }

    /// <summary>
    /// Finds an empty spot on the map that the player can walk around to reach the exit.
    /// </summary>
    Cell GetRandomEmptyCellThatPlayerCanCircumvent()
    {
        while (true)
        {
            var cell = GetRandomEmptyCell();

            var obstacles = cellsWithItems
                .Where(c => c.Item!.itemType != ItemType.Exit)
                .Select(c => new Point(c.X, c.Y))
                .Concat(new[] { new Point(cell.X, cell.Y) });

            goalMap.ClearObstacles();
            goalMap.AddObstacles(obstacles);
            var path = goalMap.TryFindPath(entrancePosition.x, entrancePosition.y);

            if (path == null)
            {
                continue;
            }

            return cell;
        }
    }

    /// <summary>
    /// Finds an empty spot on the map that the player can reach.
    /// </summary>
    Cell GetRandomEmptyCellThatPlayerCanReach()
    {
        while (true)
        {
            var cell = GetRandomEmptyCell();

            var obstacles = cellsWithItems
                .Where(c => c.Item!.itemType != ItemType.Exit)
                .Select(c => new Point(c.X, c.Y));

            goalMap.ClearObstacles();
            goalMap.AddObstacles(obstacles);
            var monumentPosition = cell.Position;
            var path = goalMap.TryFindPath(monumentPosition.x, monumentPosition.y);

            if (path == null)
            {
                continue;
            }

            return cell;
        }
    }

    bool IsCellEmpty(Cell cell)
    {
        return cell.IsWalkable && cell.Item == null && cell.Position != entrancePosition;
    }

    void PlaceMonsters(int monsterCount)
    {
        var placedMonsters = 0;

        while (placedMonsters < monsterCount)
        {
            var cell = GetRandomEmptyCellThatPlayerCanCircumvent();
            var playerVisibility = rng.NextBool() ? PlayerType.Player1 : PlayerType.Player2;
            var movementDirections = new[] { Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.up };
            var randomMovementDirection = movementDirections[rng.Next(movementDirections.Length)];
            cell.Item = new Item(ItemType.Monster, playerVisibility, randomMovementDirection);
            placedMonsters++;
        }
    }

    void PlacePits(int pitCount)
    {
        var placedPits = 0;

        while (placedPits < pitCount)
        {
            var cell = GetRandomEmptyCellThatPlayerCanCircumvent();
            var visibleToPlayer1 = rng.NextBool();
            cell.Item = new Item(ItemType.Pit, visibleToPlayer1 ? PlayerType.Player1 : PlayerType.Player2);
            placedPits++;
        }
    }

    private void PlaceLights(int lightCount)
    {
        lights = new Dungeon.Light[] {};

        var placedLights = 0;

        while (placedLights < lightCount)
        {
            var cell = GetRandomEmptyCell();
            var light = new Dungeon.Light(cell.Position, rng.Next(5) + 2, map);

            lights = lights.Append(light).ToArray();

            placedLights++;
        }
    }

    void PlaceMonument()
    {
        var cell = GetRandomEmptyCellThatPlayerCanReach();
        cell.GroundType = GroundType.Grass;
        cell.Item = new Item(ItemType.Monument, PlayerType.Both);
    }

    public void RegenerateVisible(Vector3Int at, int radius)
    {
        fov.ComputeFov(at.x, at.y, radius, true);
    }

    public void BloodSplat(Vector3Int at)
    {
        map[at].Item = new Item(ItemType.Blood, PlayerType.Both);
    }

    public bool isVisible(Vector3Int at)
    {
        return fov.IsInFov(at.x, at.y);
    }
}
