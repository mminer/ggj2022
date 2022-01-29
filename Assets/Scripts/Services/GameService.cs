using System;
using UnityEngine;
using Random = UnityEngine.Random;

class GameService : Services.Service
{
    [SerializeField] GameObject playerPrefab;

    public delegate void OnGameStartedHandler(string gameCode);
    public event OnGameStartedHandler OnGameStarted;

    public delegate void OnGameEndedHandler(bool isWinner);
    public event OnGameEndedHandler OnGameEnded;

    bool isPlayer1 = true;

    public void StartGame(string code = null)
    {
        // If we don't reset the random number generator, the game uses the seed from the previous run.
        // The random number generator state persists between game sessions.
        // We don't want this to happen, so randomize it now.
        Random.InitState((int)DateTime.Now.Ticks);

        if (code != null)
        {
            isPlayer1 = false;
        }
        else
        {
            code = GameCodeUtility.GenerateGameCode();
            Debug.Log($"Generated code: {code}");
        }

        var mapService = Services.Get<MapService>();
        mapService.GenerateMap(code);

        // Spawn player.
        Instantiate(playerPrefab, mapService.playerSpawnPoint, Quaternion.identity);

        if (OnGameStarted != null)
        {
            OnGameStarted(code);
        }
    }

    public void EndGame(bool isWinner)
    {
        if (OnGameEnded != null)
        {
            OnGameEnded(isWinner);
        }
    }
}
