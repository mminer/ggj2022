using System;
using UnityEngine;
using Random = UnityEngine.Random;

public enum EndCondition
{
    Won,
    Quit,
    FellInPit,
    BadPasscode,
}

public class GameService : Services.Service
{
    [SerializeField] GameObject playerPrefab;

    public delegate void OnGameStartedHandler(string gameCode);
    public event OnGameStartedHandler OnGameStarted;

    public delegate void OnGameEndedHandler(EndCondition endCondition);
    public event OnGameEndedHandler OnGameEnded;

    public Player playerAssignment { get; private set; } = Player.None;

    GameObject player;

    void Awake()
    {
        Services.Get<UIService>().OnSubmitGlyphs += OnSubmitGlyphs;
    }

    public void StartGame(string code = null)
    {
        // If we don't reset the random number generator, the game uses the seed from the previous run.
        // The random number generator state persists between game sessions.
        // We don't want this to happen, so randomize it now.
        Random.InitState((int)DateTime.Now.Ticks);

        if (string.IsNullOrEmpty(code))
        {
            playerAssignment = Player.Player1;
            Debug.Log($"Playing as player 1");

            code = GameCodeUtility.GenerateGameCode();
            Debug.Log($"Generated code: {code}");
        }
        else
        {
            playerAssignment = Player.Player2;
            Debug.Log($"Playing as player 2");
        }

        var dungeonService = Services.Get<DungeonService>();
        dungeonService.GenerateDungeon(code);

        // Spawn player.

        if (player != null)
        {
            Destroy(player);
        }

        player = Instantiate(playerPrefab, dungeonService.dungeon.playerSpawnPosition, Quaternion.identity);

        OnGameStarted?.Invoke(code);
    }

    public void EndGame(EndCondition endCondition)
    {
        OnGameEnded?.Invoke(endCondition);
        Destroy(player);
    }

    private void OnSubmitGlyphs(int[] combo)
    {
        Debug.Log("TODO: check if combo is expected");
        EndGame(EndCondition.BadPasscode);
    }
}
