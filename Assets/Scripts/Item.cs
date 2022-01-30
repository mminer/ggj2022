using UnityEngine;

public readonly struct Item
{
    public readonly ItemType itemType;
    public readonly Vector3Int originalPosition;
    public readonly PlayerType playerVisibility;

    public Item(ItemType itemType, Vector3Int originalPosition, PlayerType playerVisibility)
    {
        this.itemType = itemType;
        this.originalPosition = originalPosition;
        this.playerVisibility = playerVisibility;
    }
}

public enum ItemType
{
    Door,
    Exit,
    Key,
    Monster,
    Pit,
    Weapon,
    Passcode,
}
