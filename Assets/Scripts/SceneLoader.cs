using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("在这里填写要切换的场景名")]
    public string sceneName;

    // 切换到 Inspector 里填写的场景
    public void LoadSceneByName()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("Scene name is empty. Please enter a scene name in Inspector.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    // 重新加载当前场景
    public void ReloadCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    // 退出游戏，可选
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }
}