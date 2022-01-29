using UnityEngine;

class GameService : Services.Service
{
    [SerializeField] GameObject playerPrefab;

    void Start()
    {
        // TODO: call this from UI
        StartGame();
    }

    public void StartGame()
    {
        var mapService = Services.Get<MapService>();
        mapService.GenerateMap();

        // Spawn player.
        Instantiate(playerPrefab, mapService.playerSpawnPoint, Quaternion.identity);
    }
}
