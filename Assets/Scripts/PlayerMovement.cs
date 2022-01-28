using UnityEngine;
using UnityEngine.InputSystem;

class PlayerMovement : MonoBehaviour
{
    void Update()
    {
        // TODO: also support gamepads and WASD
        var keyboard = Keyboard.current;

        if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            Move(Vector2Int.down);
        }

        if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            Move(Vector2Int.left);
        }

        if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            Move(Vector2Int.right);
        }

        if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            Move(Vector2Int.up);
        }
    }

    void Move(Vector2Int delta)
    {
        var targetTilePosition = Vector2Int.FloorToInt(transform.position) + delta;

        if (!Services.Get<MapService>().CanMoveToTile(targetTilePosition))
        {
            return;
        }

        transform.position = (Vector2)targetTilePosition;
    }
}
