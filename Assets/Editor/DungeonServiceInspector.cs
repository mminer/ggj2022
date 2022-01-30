using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DungeonService))]
public class DungeonServiceInspector : Editor
{
    string gameCode = "aaaa";

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        using (new EditorGUILayout.HorizontalScope())
        {
            gameCode = EditorGUILayout.TextField("Game Code", gameCode);

            if (GUILayout.Button("Regenerate", EditorStyles.miniButton))
            {
                Services.Get<GameService>().StartGame(gameCode);
            }
        }
    }
}
