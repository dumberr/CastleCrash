using UnityEngine;

// Temporar
public class LevelEndPlaceholder : MonoBehaviour
{
    private GameManager game;
    private GUIStyle titleStyle;

    private void Start() => game = GameManager.Instance;

    private void OnGUI()
    {
        if (game == null || game.State == GameState.Playing) return;

        titleStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 32, alignment = TextAnchor.MiddleCenter };

        LevelResult r = game.Result;
        var box = new Rect(Screen.width / 2f - 200, Screen.height / 2f - 130, 400, 260);
        GUI.Box(box, GUIContent.none);
        GUILayout.BeginArea(new Rect(box.x + 20, box.y + 15, box.width - 40, box.height - 30));

        GUILayout.Label(r.Won ? "VICTORIE!" : "INFRANGERE", titleStyle);
        GUILayout.Label($"Scor: {r.Score}{(r.NewBest ? "  (record nou)" : "")}", titleStyle);
        if (r.Won) GUILayout.Label(new string('*', r.Stars).PadRight(3, '-'), titleStyle);

        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Retry", GUILayout.Height(36))) game.Retry();
        if (r.Won && game.HasNextLevel && GUILayout.Button("Next", GUILayout.Height(36))) game.LoadNextLevel();
        if (GUILayout.Button("Menu", GUILayout.Height(36))) game.GoToMainMenu();
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }
}
