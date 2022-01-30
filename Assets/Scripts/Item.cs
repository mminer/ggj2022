using UnityEngine;

public class Item
{
    public readonly int id;
    public readonly ItemType itemType;
    public Vector3Int movementDirection;
    public readonly PlayerType playerVisibility;

    public Item(ItemType itemType, PlayerType playerVisibility, Vector3Int movementDirection = default)
    {
        this.id = Random.Range(0, int.MaxValue);
        this.itemType = itemType;
        this.movementDirection = movementDirection;
        this.playerVisibility = playerVisibility;
    }

    public void ReverseMovementDirection()
    {
        movementDirection = movementDirection switch
        {
            _ when movementDirection == Vector3Int.down => Vector3Int.up,
            _ when movementDirection == Vector3Int.left => Vector3Int.right,
            _ when movementDirection == Vector3Int.right => Vector3Int.left,
            _ when movementDirection == Vector3Int.up => Vector3Int.down,
            _ => Vector3Int.zero,
        };
    }
}

public enum ItemType
{
    Door,
    Exit,
    Key,
    Monster,
    Pit,
    Blood,
    Weapon,
    Monument,
}
