using JetBrains.Annotations;
using UnityEngine;

class GameService : Services.Service
{
    [SerializeField] GameObject playerPrefab;

    bool isPlayer1 = true;

    public void StartGame(string code = null)
    {
        if (code != null)
        {
            isPlayer1 = false;
        }
        else
        {
            code = GameCodeUtility.GenerateGameCode();
        }

        Services.Get<UIService>().ShowGameCode(code);
        var mapService = Services.Get<MapService>();
        mapService.GenerateMap(code);

        // Spawn player.
        Instantiate(playerPrefab, mapService.playerSpawnPoint, Quaternion.identity);
    }
}
