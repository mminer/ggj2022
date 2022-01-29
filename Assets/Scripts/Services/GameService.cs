using System;
using UnityEngine;
using Random = UnityEngine.Random;

class GameService : Services.Service
{
    [SerializeField] GameObject playerPrefab;

    bool isPlayer1 = true;

    void Awake()
    {
        // If we don't reset the random number generator, the game uses the seed from the previous run.
        // The random number generator state persists between game sessions.
        // We don't want this to happen, so randomize it now.
        Random.InitState((int)DateTime.Now.Ticks);
    }

    public void StartGame(string code = null)
    {
        if (code != null)
        {
            isPlayer1 = false;
        }
        else
        {
            code = GameCodeUtility.GenerateGameCode();
            Debug.Log($"Generated code: {code}");
        }

        Services.Get<UIService>().ShowGameCode(code);
        var mapService = Services.Get<MapService>();
        mapService.GenerateMap(code);

        // Spawn player.
        Instantiate(playerPrefab, mapService.playerSpawnPoint, Quaternion.identity);
    }
}
