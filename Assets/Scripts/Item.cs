public readonly struct Item
{
    public readonly ItemType itemType;
    public readonly Player playerVisibility;

    public Item(ItemType itemType, Player playerVisibility)
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
    None,
}
