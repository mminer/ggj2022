using UnityEngine;
using UnityEngine.Tilemaps;

class DungeonService : Services.Service
{
    [SerializeField] Tilemap tilemap;

    [Header("Map")]
    [SerializeField] int width = 32;
    [SerializeField] int height = 32;
    [SerializeField] int maxRooms = 30;
    [SerializeField] int roomMaxSize = 10;
    [SerializeField] int roomMinSize = 3;

    [Header("Tiles")]
    [SerializeField] Tile exitTile;
    [SerializeField] Tile groundTile;
    [SerializeField] Tile notGroundTile;

    public Dungeon dungeon { get; private set; }

    public bool CanMoveToTile(Vector3Int targetTilePosition)
    {
        return dungeon[targetTilePosition].IsWalkable;
    }

    public void GenerateDungeon(string gameCode)
    {
        dungeon = new Dungeon(gameCode, width, height, maxRooms, roomMaxSize, roomMinSize);
        Debug.Log($"Generated dungeon: \n{dungeon}");
        SetTilesFromCells();
    }

    void SetTilesFromCells()
    {
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var tilePosition = new Vector3Int(x, y);
                var cell = dungeon[tilePosition];
                tilemap.SetTile(tilePosition, cell.IsWalkable ? groundTile : notGroundTile);
            }
        }

        tilemap.SetTile(dungeon.exitPosition, exitTile);
    }
}
