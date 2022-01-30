using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] InputAction moveAction;

    [SerializeField] private float holdWait = 0.2f;
    private float currentHoldWait = 0.0f;

    private Vector2 direction = Vector2.zero;

    [SerializeField] GameObject playerVisual;

    void Awake()
    {
        moveAction.performed += OnMove;
        moveAction.canceled += OnDoneMove;

        playerVisual.GetComponentInChildren<AnimationCallback>().OnAnimationComplete += AnimationComplete;
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

            var targetTilePosition = Vector3Int.FloorToInt(transform.position) + Vector3Int.RoundToInt(direction);
            var cell = Services.Get<DungeonService>().dungeon[targetTilePosition];

            if (!cell.IsWalkable) { return; }

            var dungeonService = Services.Get<DungeonService>();
            dungeonService.dungeon.UpdateMovableItems();
            dungeonService.RegenerateVisible(targetTilePosition);

            Services.Get<AudioService>().PlayFootstep();

            transform.position = targetTilePosition;

            if (cell.Item.HasValue)
            {
                switch (cell.Item.Value.itemType)
                {
                    case ItemType.Exit:
                        Services.Get<UIService>().ShowGlyphScreen();
                        break;

                    case ItemType.Monument:
                        Services.Get<AudioService>().PlayMonumentJingle();
                        Services.Get<UIService>().ShowMonumentScreen();
                        break;

                    case ItemType.Monster:
                        StartCoroutine(MonsterDeath(cell.Item.Value.playerVisibility, 0.4f));
                        break;

                    case ItemType.Pit:
                        Services.Get<AudioService>().PlayTumble();
                        playerVisual.GetComponentInChildren<Animator>().Play("Fall");
                        break;
                }
            }
        }
    }

    IEnumerator MonsterDeath(PlayerType enemyVisibility, float bloodyDelay)
    {
        var position = Vector3Int.FloorToInt(transform.position);

        playerVisual.SetActive(false);

        Services.Get<AudioService>().PlayTrap(ItemType.Monster);

        var dungeonService = Services.Get<DungeonService>();

        if(enemyVisibility == Services.Get<GameService>().playerAssignment || enemyVisibility == PlayerType.Both) {
            Services.Get<UIService>().monsterSprite = dungeonService.SpriteAt(position);
        }

        dungeonService.BloodSplat(position);

        yield return new WaitForSeconds(bloodyDelay);

        Services.Get<GameService>().EndGame(EndCondition.AteByMonster);
    }

    void AnimationComplete(string name) {
        if(name == "Fall") {
            Services.Get<AudioService>().PlayTrap(ItemType.Pit);
            Services.Get<GameService>().EndGame(EndCondition.FellInPit);
        }
    }
}
