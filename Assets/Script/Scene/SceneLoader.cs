using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary> シーン読み込みユーティリティ. UniTask で非同期ロード. </summary>
public static class SceneLoader
{
    public const string ExplorationSceneName = "ExplorationScene";
    public const string PuzzleSceneName = "PuzzleScene";

    /// <summary> 指定シーンを Single モードで非同期ロード. </summary>
    public static async UniTask LoadSceneAsync(string sceneName)
    {
        Debug.Log($"SceneLoader: {sceneName} をロード中...");
        await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single).ToUniTask();
        Debug.Log($"SceneLoader: {sceneName} ロード完了");
    }

    /// <summary> 指定シーンを Additive モードで非同期ロード. </summary>
    public static async UniTask LoadSceneAdditiveAsync(string sceneName)
    {
        Debug.Log($"SceneLoader: {sceneName} を Additive ロード中...");
        await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive).ToUniTask();
        Debug.Log($"SceneLoader: {sceneName} Additive ロード完了");
    }

    /// <summary> 指定シーンをアンロード. </summary>
    public static async UniTask UnloadSceneAsync(string sceneName)
    {
        Debug.Log($"SceneLoader: {sceneName} をアンロード中...");
        await SceneManager.UnloadSceneAsync(sceneName).ToUniTask();
        Debug.Log($"SceneLoader: {sceneName} アンロード完了");
    }
}
