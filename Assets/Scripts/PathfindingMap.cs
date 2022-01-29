using System;
using RogueSharp;

/// <summary>
/// Temporarily modifies a map, marking cells with items as unwalkable for the purpose of pathfinding.
/// When disposed it reverts the map to its original state.
/// </summary>
public class PathfindingMap : IDisposable
{
    readonly Map map;
    readonly MapState originalState;

    public PathfindingMap(Map map, Item?[,] items)
    {
        this.map = map;
        this.originalState = map.Save();
        MarkItemCellsUnwalkable(items);
    }

    public void Dispose()
    {
        map.Restore(originalState);
    }

    void MarkItemCellsUnwalkable(Item?[,] items)
    {
        for (var x = 0; x < map.Width; x++)
        {
            for (var y = 0; y < map.Height; y++)
            {
                var item = items[x, y];

                if (item.HasValue && item.Value.itemType != ItemType.Exit)
                {
                    map[x, y].IsWalkable = false;
                }
            }
        }
    }
}
