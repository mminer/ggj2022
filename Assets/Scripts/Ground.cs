public readonly struct Ground
{
    public readonly GroundType groundType;
    public readonly Item? item;

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
    Bridge,
    Wall
}
