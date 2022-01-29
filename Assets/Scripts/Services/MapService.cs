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
    [SerializeField] Tile exitTile;
    [SerializeField] Tile groundTile;
    [SerializeField] Tile notGroundTile;

    public Vector3Int playerSpawnPoint { get; private set; }

    Map map;

    public bool CanMoveToTile(Vector3Int targetTilePosition)
    {
        // TODO
        return true;
    }

    public void GenerateMap()
    {
        var mapCreationStrategy = new RandomRoomsMapCreationStrategy<Map>(32, 32, maxRooms, roomMaxSize, roomMinSize);
        //var mapCreationStrategy = new CaveMapCreationStrategy<Map>(32, 32, 50, 4, 3);
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

        // Four corners.
        var bottomLeft = new Vector3Int(-map.Height / 2 + 1, -map.Width / 2 + 1);
        var bottomRight = new Vector3Int(map.Height / 2 - 2, -map.Width / 2 + 1);
        var topLeft = new Vector3Int(-map.Height / 2 + 1, map.Width / 2 - 2);
        var topRight = new Vector3Int(map.Height / 2 - 2, map.Width / 2 - 2);

        var playerSpawnPointPossibilities = new[] { bottomLeft, bottomRight, topLeft, topRight };
        playerSpawnPoint = playerSpawnPointPossibilities[Random.Range(0, playerSpawnPointPossibilities.Length)];

        Vector3Int exitPosition;

        // Choose the corner opposite the player spawn point for the exit.
        if (playerSpawnPoint == bottomLeft)
        {
            exitPosition = topRight;
        }
        else if (playerSpawnPoint == bottomRight)
        {
            exitPosition = topLeft;
        }
        else if (playerSpawnPoint == bottomRight)
        {
            exitPosition = bottomLeft;
        }
        else
        {
            exitPosition = bottomLeft;
        }

        tilemap.SetTile(exitPosition, exitTile);
    }
}
