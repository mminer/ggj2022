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
        Services.Get<MapService>().GenerateMap();
        var player = Instantiate(playerPrefab);
    }
}
