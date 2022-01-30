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
        public Item? Item { get; set; }
        public Vector3Int Position => new(X, Y);
    }

    public readonly Vector3Int entrancePosition;
    public readonly int[] glyphs;
    public Light[] lights { private set; get; }

    readonly FieldOfView<Cell> fov;
    readonly GoalMap<Cell> goalMap;
    readonly Map<Cell> map;
    readonly RandomNumberGenerator rng;

    // Four corners inside the walls.
    Vector3Int bottomLeft => new(1, 1);
    Vector3Int bottomRight => new(map.Width - 2, 1);
    Vector3Int topLeft => new(1, map.Height - 2);
    Vector3Int topRight => new(map.Width - 2, map.Height - 2);
    
    Vector3Int middleLeft => new(1, map.Height / 2 - 2);
    Vector3Int middleRight => new(map.Width - 2, map.Height / 2 - 2);
    Vector3Int middleTop => new(map.Width / 2 - 2, map.Height - 2);
    Vector3Int middleBottom => new(map.Width / 2 - 2, 1);

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

        var riverStart = GetRandomSidePosition();
        CarveRiver(riverStart, GetPathCarveDirection(riverStart));
        riverStart = GetRandomCornerPosition();
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
        exitCell.Item = new Item(ItemType.Exit, exitPosition, PlayerType.Both);

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
        var cellsWithMonsters = map
            .GetAllCells()
            .Where(cell => cell.Item is { itemType: ItemType.Monster });

        foreach (var cell in cellsWithMonsters)
        {
            var item = cell.Item!.Value;

            if (cell.Position != item.originalPosition)
            {
                map[item.originalPosition].Item = item;
                cell.Item = null;
            }
            else
            {
                var emptyAdjacentCell = GetRandomEmptyAdjacentCell(cell);

                if (emptyAdjacentCell == null)
                {
                    continue;
                }

                map[item.originalPosition].Item = null;
                emptyAdjacentCell.Item = item;
            }
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

        var offset = direction;
        if(rng.NextBool()) {
            offset.x = 0;
        } else {
            offset.y = 0;
        }

        bool posWithin(Vector3Int pos) {
            return pos.x < map.Width && pos.x >= 0 && pos.y < map.Height && pos.y >= 0;
        }

        int randomOffset() {
            return (rng.NextBool() ? rng.NextBool() ? 0 : -1 : 1);
        }

        while (iteration < 50)
        {
            void set(Vector3Int pos) {
                if (posWithin(pos) && !map.IsWalkable(pos))
                {
                    var cell = map[pos];
                    cell.GroundType = GroundType.Water;
                    cell.IsTransparent = true;
                }

            }

            set(position);

            // make it 3 wide
            var offsetPos = position + offset;
            var offsetPos2 = position - offset;

            if(posWithin(offsetPos)){ set(offsetPos); }
            if(posWithin(offsetPos2)){ set(offsetPos2); }
            
            position.x += Mathf.Clamp(randomOffset() * direction.x + randomOffset(), -1, 1);
            position.y += Mathf.Clamp(randomOffset() * direction.y + randomOffset(), -1, 1);

            // position += direction;

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
            _ when cornerPosition == middleLeft => Vector3Int.right,
            _ when cornerPosition == middleRight => Vector3Int.left,
            _ when cornerPosition == middleBottom => Vector3Int.up,
            _ when cornerPosition == middleTop => Vector3Int.down,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    Vector3Int GetRandomCornerPosition()
    {
        var cornerPositions = new[] { bottomLeft, bottomRight, topLeft, topRight };
        return cornerPositions[rng.Next(cornerPositions.Length)];
    }
    
    Vector3Int GetRandomSidePosition()
    {
        var cornerPositions = new[] { bottomLeft, bottomRight, topLeft, topRight, middleLeft, middleRight, middleBottom, middleTop };
        return cornerPositions[rng.Next(cornerPositions.Length)];
    }

    Cell GetRandomEmptyAdjacentCell(Cell cell)
    {
        var emptyAdjacentCells = map
            .GetAdjacentCells(cell.Position)
            .Where(IsCellEmpty)
            .ToArray();

        return emptyAdjacentCells.Length > 0
            ? emptyAdjacentCells[rng.Next(emptyAdjacentCells.Length)]
            : null;
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

            var obstacles = map
                .GetAllCells()
                .Where(c => c.Item.HasValue && c.Item.Value.itemType != ItemType.Exit)
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

            var obstacles = map
                .GetAllCells()
                .Where(c => c.Item.HasValue && c.Item.Value.itemType != ItemType.Exit)
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
        return cell.IsWalkable && !cell.Item.HasValue && cell.Position != entrancePosition;
    }

    void PlaceMonsters(int monsterCount)
    {
        var placedMonsters = 0;

        while (placedMonsters < monsterCount)
        {
            var cell = GetRandomEmptyCellThatPlayerCanCircumvent();
            var visibleToPlayer1 = rng.NextBool();
            cell.Item = new Item(ItemType.Monster, cell.Position, visibleToPlayer1 ? PlayerType.Player1 : PlayerType.Player2);
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
            cell.Item = new Item(ItemType.Pit, cell.Position, visibleToPlayer1 ? PlayerType.Player1 : PlayerType.Player2);
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
        cell.Item = new Item(ItemType.Monument, cell.Position, PlayerType.Both);
    }

    public void RegenerateVisible(Vector3Int at, int radius)
    {
        fov.ComputeFov(at.x, at.y, radius, true);
    }
    
    public void BloodSplat(Vector3Int at)
    {
        map[at].Item = new Item(ItemType.Blood, at, PlayerType.Both);
    }

    public bool isVisible(Vector3Int at)
    {
        return fov.IsInFov(at.x, at.y);
    }
}
