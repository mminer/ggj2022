public readonly struct Item
{
    public readonly ItemType itemType;
    public readonly PlayerType playerVisibility;

    public Item(ItemType itemType, PlayerType playerVisibility)
    {
        this.itemType = itemType;
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
    None,
}
