using JetBrains.Annotations;
using UnityEngine;

class GameService : Services.Service
{
    [SerializeField] GameObject playerPrefab;

    public void StartGame(string code = null)
    {
        Services.Get<MapService>().GenerateMap();
        var player = Instantiate(playerPrefab);
    }
}
