using UnityEngine;
using UnityEngine.InputSystem;

class PlayerMovement : MonoBehaviour
{
    [SerializeField] InputAction moveAction;

    void Awake()
    {
        moveAction.performed += OnMove;
    }

    void OnEnable()
    {
        moveAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
    }

    void OnMove(InputAction.CallbackContext context)
    {
        var direction = context.ReadValue<Vector2>();
        var targetTilePosition = Vector3Int.FloorToInt(transform.position) + Vector3Int.FloorToInt(direction);
        var mapService = Services.Get<MapService>();

        if (!mapService.CanMoveToTile(targetTilePosition))
        {
            return;
        }

        Services.Get<AudioService>().PlayFootstep();

        transform.position = targetTilePosition;

        if (targetTilePosition == mapService.exitPosition)
        {
            Services.Get<GameService>().EndGame(true);
        }
    }
}
