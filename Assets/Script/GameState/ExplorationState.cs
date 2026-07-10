using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary> ランダムマップ探索（アクション）フェーズの状態クラス. </summary>
public class ExplorationState : IGameState
{
    /// <summary> Playerプレハブのアドレサブルスアドレス. </summary>
    public const string PlayerAddress = "Player";

    private PoolManager _poolManager;
    private PlayerController _playerController;
    private StageData _stageData;
    private StageView _stageView;
    private ExplorationCameraController _cameraController;
    private GameObject _playerObj;

    /// <summary> PoolManager を設定. GameStateManager 経由で呼ばれる. </summary>
    public void SetPoolManager(PoolManager pool)
    {
        _poolManager = pool;
    }

    public async UniTask EnterAsync()
    {
        Debug.Log("ExplorationState: EnterAsync");

        // 探索シーンをロード (Single: 前シーンは破棄される).
        await SceneLoader.LoadSceneAsync(SceneLoader.ExplorationSceneName);

        // ステージ生成.
        var gridSize = new Vector2Int(75, 75);
        float tileSize = 3f;
        _stageData = StageGenerator.Generate(gridSize, tileSize,10);

        // ステージ描画.
        var stageRoot = new GameObject("StageRoot");
        _stageView = new StageView();
        await _stageView.BuildAsync(_stageData, stageRoot.transform, _poolManager);

        // Player をアドレサブルスからインスタンス化.
        _playerObj = await Addressables.InstantiateAsync(PlayerAddress).ToUniTask();
        _playerObj.name = "Player";

        // Player をスタート地点に配置.
        var startWorld = _stageData.GridToWorld(_stageData.StartPos);
        _playerObj.transform.position = startWorld + new Vector3(0f, 0.5f, 0f);

        _playerController = new PlayerController(_playerObj);
        _playerController.Initialize();

        // カメラ: シーンのMainCameraを取得して後方追尾.
        var camera = Camera.main;
        if (camera != null)
        {
            _cameraController = new ExplorationCameraController();
            _cameraController.Initialize(camera, _playerObj.transform);
        }
        else
        {
            Debug.LogWarning("ExplorationState: Camera.main が見つかりません. ExplorationScene に MainCamera タグ付きカメラを配置してください.");
        }
    }

    public void Exit()
    {
        Debug.Log("ExplorationState: Exit");

        _cameraController?.Dispose();
        _cameraController = null;
        _playerController?.Dispose();
        _playerController = null;
        _stageView?.Dispose();
        _stageView = null;
        _stageData = null;

        if (_playerObj != null)
        {
            Addressables.ReleaseInstance(_playerObj);
            _playerObj = null;
        }
    }

    /// <summary> 探索の入力/カメラを有効化・無効化. Puzzle切り替え時に使用. </summary>
    public void SetInputActive(bool active)
    {
        _playerController?.SetInputEnabled(active);
        _cameraController?.SetEnabled(active);
    }

    public void Update()
    {
        _playerController?.Update();
        _cameraController?.Update();
    }

    /// <summary> 明示的な破棄. </summary>
    public void Dispose()
    {
        _cameraController?.Dispose();
        _cameraController = null;
        _playerController?.Dispose();
        _playerController = null;
        _stageView?.Dispose();
        _stageView = null;
        _stageData = null;

        if (_playerObj != null)
        {
            Addressables.ReleaseInstance(_playerObj);
            _playerObj = null;
        }
    }
}
