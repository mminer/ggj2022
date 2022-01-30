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
    [SerializeField] int monsterCount = 5;
    [SerializeField] int pitCount = 5;

    [Header("Lights")]
    [SerializeField] int maxLights = 5;
    [SerializeField] private Color lightColor = Color.white;

    [Header("Tiles")]
    [SerializeField] TileBase exitTile;
    [SerializeField] TileBase groundTile;
    [SerializeField] TileBase monsterTile;
    [SerializeField] TileBase pitTile;
    [SerializeField] TileBase wallTile;
    [SerializeField] TileBase waterTile;

    [SerializeField] private int visibleRadius = 3;

    [Header("Debug")]
    [SerializeField] bool disableLighting;
    [SerializeField] bool overrideItemVisibility;
    [SerializeField] PlayerType itemVisibilityOverride = PlayerType.Both;

    public Dungeon dungeon { get; private set; }

    public void GenerateDungeon(string gameCode)
    {
        var itemCounts = new Dictionary<ItemType, int>
        {
            { ItemType.Monster, monsterCount },
            { ItemType.Pit, pitCount },
        };

        dungeon = new Dungeon(gameCode, width, height, maxRooms, roomMaxSize, roomMinSize, itemCounts, maxLights);
        Debug.Log($"Generated dungeon: \n{dungeon}");

        RegenerateVisible(dungeon.entrancePosition);
    }

    public int GetGlyphByPlayer(PlayerType playerAssignment)
    {
        return playerAssignment == PlayerType.Player1 ? dungeon.glyphs[0] : dungeon.glyphs[1];
    }

    TileBase GetTile(Vector3Int tilePosition, PlayerType playerAssignment)
    {
        var (isWalkable, ground) = dungeon[tilePosition];

        if (ground?.item is Item item && IsItemVisible(item, playerAssignment))
        {
            return item.itemType switch
            {
                ItemType.Door => throw new NotImplementedException(),
                ItemType.Exit => exitTile,
                ItemType.Key => throw new NotImplementedException(),
                ItemType.Monster => monsterTile,
                ItemType.Pit => pitTile,
                ItemType.Weapon => throw new NotImplementedException(),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
        else if(ground is Ground nonNullGround)
        {
            return nonNullGround.groundType switch
            {
                GroundType.Wall => wallTile,
                GroundType.Water => waterTile,
                GroundType.Grass => groundTile,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
        else {
            return null;
        }

    }

    bool IsItemVisible(Item item, PlayerType playerAssignment)
    {
        return overrideItemVisibility
            ? itemVisibilityOverride.HasFlag(playerAssignment)
            : item.playerVisibility.HasFlag(playerAssignment);
    }

    void SetTilesFromCells(Vector3Int visiblePosition)
    {
        var playerAssignment = Services.Get<GameService>().playerAssignment;

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var tilePosition = new Vector3Int(x, y);
                var tile = GetTile(tilePosition, playerAssignment);
                tilemap.SetTile(tilePosition, tile);
                tilemap.SetTileFlags(tilePosition, TileFlags.None); // needed to change the color of a tile...

                if (disableLighting)
                {
                    continue;
                }

                var tileColor = Color.black;

                void mixLight (bool isVisible, Color color, Vector3Int fromPoint, float radius)
                {
                    float distanceFromPoint = Vector3Int.Distance(tilePosition, fromPoint);

                    float hue, saturation, tileBrightness;

                    Color.RGBToHSV(tileColor, out _, out _, out tileBrightness);
                    Color.RGBToHSV(color, out hue, out saturation, out _);

                    // add the brightness of the lit tiles (an unlit black tile will have a brightness of 0)
                    var brightness = (isVisible ? (1.0f - (distanceFromPoint - 1) / radius) : 0);
                    tileColor = Color.HSVToRGB(hue, saturation, brightness + tileBrightness);

                };

                bool lightVisible = false;

                foreach(Dungeon.Light l in dungeon.lights)
                {
                    var vis = l.isVisible(tilePosition);

                    if(vis) {
                        mixLight(vis, lightColor, l.point, l.radius);
                    }

                    lightVisible = lightVisible || vis;
                }

                mixLight(dungeon.isVisible(tilePosition), lightColor, visiblePosition, visibleRadius);

                tilemap.SetColor(tilePosition, tileColor);
            }
        }
    }

    public void RegenerateVisible(Vector3Int position) {
        dungeon.RegenerateVisible(position, visibleRadius);
        SetTilesFromCells(position);
    }
}
