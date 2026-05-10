using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles the Main Menu scene.
/// Attach to a persistent GameObject in the MainMenu scene.
/// Wire the PlayGame() method to your Play button's OnClick event.
///
/// Win and Lose states are handled as UI panels inside MainGame — not separate scenes.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    // Scene names centralised here to avoid magic strings scattered around.
    // If you rename a scene, change it in one place only.
    public const string SCENE_MAIN_MENU = "MainMenu";
    public const string SCENE_MAIN_GAME = "MainGame";
    public const string SCENE_TUTORIAL = "Tutorial";

    /// <summary>
    /// Called by the Play button's OnClick event in the Inspector.
    /// </summary>
    public void PlayGame()
    {
        SceneManager.LoadScene(SCENE_TUTORIAL);
    }

    /// <summary>
    /// Called by a Quit button if you add one later.
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
