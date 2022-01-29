using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonService : Services.Service
{
    [SerializeField] Tilemap tilemap;

    [Header("Map")]
    [SerializeField] int width = 32;
    [SerializeField] int height = 32;
    [SerializeField] int maxRooms = 30;
    [SerializeField] int roomMaxSize = 10;
    [SerializeField] int roomMinSize = 3;

    [Header("Items")]
    [SerializeField] int pitCount = 5;

    [Header("Tiles")]
    [SerializeField] Tile exitTile;
    [SerializeField] Tile groundTile;
    [SerializeField] Tile pitTile;
    [SerializeField] Tile wallTile;

    public Dungeon dungeon { get; private set; }

    public void GenerateDungeon(string gameCode)
    {
        var itemCounts = new Dictionary<ItemType, int>
        {
            { ItemType.Pit, pitCount },
        };

        dungeon = new Dungeon(gameCode, width, height, maxRooms, roomMaxSize, roomMinSize, itemCounts);
        Debug.Log($"Generated dungeon: \n{dungeon}");
        SetTilesFromCells();
    }

    Tile GetTile(Vector3Int tilePosition, Player playerAssignment)
    {
        var (isWalkable, item) = dungeon[tilePosition];

        if (item.HasValue && item.Value.playerVisibility.HasFlag(playerAssignment))
        {
            return item.Value.itemType switch
            {
                ItemType.Door => throw new NotImplementedException(),
                ItemType.Exit => exitTile,
                ItemType.Key => throw new NotImplementedException(),
                ItemType.Monster => throw new NotImplementedException(),
                ItemType.Pit => pitTile,
                ItemType.Weapon => throw new NotImplementedException(),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
        else
        {
            return isWalkable ? groundTile : wallTile;
        }
    }

    void SetTilesFromCells()
    {
        var playerAssignment = Services.Get<GameService>().playerAssignment;

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var tilePosition = new Vector3Int(x, y);
                var tile = GetTile(tilePosition, playerAssignment);
                tilemap.SetTile(tilePosition, tile);
            }
        }
    }
}
