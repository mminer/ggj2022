using JetBrains.Annotations;
using UnityEngine;

class GameService : Services.Service
{
    [SerializeField] GameObject playerPrefab;

    public void StartGame(string code = null)
    {
        var mapService = Services.Get<MapService>();
        mapService.GenerateMap();

        // Spawn player.
        Instantiate(playerPrefab, mapService.playerSpawnPoint, Quaternion.identity);
    }
}
