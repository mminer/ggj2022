public struct Ground
{
    public readonly GroundType groundType;
    public Item? item { get; set; }

    public Ground(GroundType type, Item? item)
    {
        this.groundType = type;
        this.item = item;
    }
}

public enum GroundType
{
    Grass,
    Water,
    Wall
}
