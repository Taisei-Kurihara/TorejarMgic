using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary> パズルマップ（ミノ配置）フェーズの状態クラス. </summary>
/// <remarks>
/// PuzzleScene を Additive で読み込み.
/// パズル用カメラを Overlay として探索カメラ (Base) のスタックに追加し、
/// 探索シーンを背景に表示しつつパズルを上から描画する.
/// </remarks>
public class PuzzleState : IGameState
{
    private PuzzleMapController _controller;
    private GameObject _puzzleCameraObj;
    private Camera _baseCameraRef;
    private bool _sceneLoaded;

    public async UniTask EnterAsync()
    {
        Debug.Log("PuzzleState: EnterAsync (Additive + Overlay)");

        // パズルシーンを Additive でロード.
        await SceneLoader.LoadSceneAdditiveAsync(SceneLoader.PuzzleSceneName);
        _sceneLoaded = true;

        // ベースカメラ (探索カメラ) を取得.
        _baseCameraRef = Camera.main;

        // パズル用 Overlay カメラをコード生成.
        _puzzleCameraObj = new GameObject("PuzzleCamera");
        var puzzleCamera = _puzzleCameraObj.AddComponent<Camera>();

        // URP: Overlay カメラとしてベースカメラのスタックに追加.
        var overlayCamData = puzzleCamera.GetUniversalAdditionalCameraData();
        overlayCamData.renderType = CameraRenderType.Overlay;

        if (_baseCameraRef != null)
        {
            var baseCamData = _baseCameraRef.GetUniversalAdditionalCameraData();
            baseCamData.cameraStack.Add(puzzleCamera);
        }

        var mapSize = new Vector3Int(5, 5, 5);

        // View用ルートオブジェクト.
        var root = new GameObject("PuzzleMapRoot");

        // PuzzleScene にオブジェクトを移動 (シーンアンロード時に自動破棄).
        var puzzleScene = SceneManager.GetSceneByName(SceneLoader.PuzzleSceneName);
        SceneManager.MoveGameObjectToScene(root, puzzleScene);
        SceneManager.MoveGameObjectToScene(_puzzleCameraObj, puzzleScene);

        // トップダウンカメラをファクトリ注入.
        var viewState = new PuzzleViewState3D(view => new PuzzleCameraControllerTopDown(view));

        _controller = new PuzzleMapController();
        _controller.Initialize(viewState, mapSize, puzzleCamera, root.transform);
    }

    public void Exit()
    {
        Debug.Log("PuzzleState: Exit");

        _controller?.Dispose();
        _controller = null;

        // ベースカメラのスタックから Overlay カメラを除去.
        RemoveOverlayFromStack();

        // パズルカメラを破棄.
        if (_puzzleCameraObj != null)
        {
            Object.Destroy(_puzzleCameraObj);
            _puzzleCameraObj = null;
        }

        _baseCameraRef = null;

        // Additive シーンをアンロード.
        if (_sceneLoaded)
        {
            _sceneLoaded = false;
            SceneLoader.UnloadSceneAsync(SceneLoader.PuzzleSceneName).Forget();
        }
    }

    public void Update()
    {
        _controller?.Update();
    }

    /// <summary> 明示的な破棄. </summary>
    public void Dispose()
    {
        _controller?.Dispose();
        _controller = null;

        RemoveOverlayFromStack();

        if (_puzzleCameraObj != null)
        {
            Object.Destroy(_puzzleCameraObj);
            _puzzleCameraObj = null;
        }

        _baseCameraRef = null;

        if (_sceneLoaded)
        {
            _sceneLoaded = false;
            SceneLoader.UnloadSceneAsync(SceneLoader.PuzzleSceneName).Forget();
        }
    }

    /// <summary> ベースカメラのスタックから Overlay カメラを除去. </summary>
    private void RemoveOverlayFromStack()
    {
        if (_baseCameraRef == null || _puzzleCameraObj == null) return;

        var puzzleCamera = _puzzleCameraObj.GetComponent<Camera>();
        if (puzzleCamera == null) return;

        var baseCamData = _baseCameraRef.GetUniversalAdditionalCameraData();
        baseCamData.cameraStack.Remove(puzzleCamera);
    }
}
