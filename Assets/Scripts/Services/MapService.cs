using RogueSharp;
using RogueSharp.MapCreation;
using UnityEngine;
using UnityEngine.Tilemaps;

class MapService : Services.Service
{
    [SerializeField] Tilemap tilemap;
    [SerializeField] int maxRooms = 30;
    [SerializeField] int roomMaxSize = 10;
    [SerializeField] int roomMinSize = 3;

    [Header("Tiles")]
    [SerializeField] Tile groundTile;
    [SerializeField] Tile notGroundTile;

    Map map;

    public void Start()
    {
        var mapCreationStrategy = new RandomRoomsMapCreationStrategy<Map>(32, 32, maxRooms, roomMaxSize, roomMinSize);
        map = Map.Create(mapCreationStrategy);
        Debug.Log($"Generated map: \n{map}");

        var offset = new Vector3Int(map.Width / 2, -map.Height / 2);

        for (var x = 0; x < map.Width; x++)
        {
            for (var y = 0; y < map.Height; y++)
            {
                var cell = map[x, y];
                // TODO: create functions for this conversion; we're going to be flipping between Unity and RogueSharp's coordinates often
                var tilePosition = new Vector3Int(x, -y - 1) - offset;
                tilemap.SetTile(tilePosition, cell.IsWalkable ? groundTile : notGroundTile);
            }
        }
    }

    public bool CanMoveToTile(Vector3Int targetTilePosition)
    {
        // TODO
        return true;
    }
}
