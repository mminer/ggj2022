using UnityEngine;
using UnityEngine.InputSystem;

class PlayerMovement : MonoBehaviour
{
    [SerializeField] InputAction moveAction;

    [SerializeField] private float holdWait = 0.2f;
    private float currentHoldWait = 0.0f;

    private Vector2 direction = Vector2.zero;

    void Awake()
    {
        moveAction.performed += OnMove;
        moveAction.canceled += OnDoneMove;
    }

    void OnEnable()
    {
        moveAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
    }

    void OnDoneMove(InputAction.CallbackContext context)
    {
        // clear the wait so that the next button press is instant
        currentHoldWait = 0; 
        direction = Vector2.zero;
    }

    void OnMove(InputAction.CallbackContext context)
    {
        direction = context.ReadValue<Vector2>();
    }

    private void Update() 
    {
        MoveLogic();
        currentHoldWait -= Time.deltaTime;
    }

    private void MoveLogic() 
    {
        if(direction != Vector2.zero) {
            if(currentHoldWait > 0) { return; } // WAIT
            currentHoldWait = holdWait; // reset the waiting time when there's a successful movement.
            
            var targetTilePosition = Vector3Int.FloorToInt(transform.position) + Vector3Int.FloorToInt(direction);
            var dungeonService = Services.Get<DungeonService>();

            if (!dungeonService.CanMoveToTile(targetTilePosition))
            {

                Services.Get<AudioService>().PlayFootstep();

                transform.position = targetTilePosition;

                if (targetTilePosition == dungeonService.dungeon.exitPosition)
                {
                    Services.Get<GameService>().EndGame(true);
                }
            }
        }
    }

}
