using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

public class InGameManager
{
    private PoolManager _poolManager;
    private GameStateManager _gameStateManager;
    private DebugKeyController _debugKeyController;

    public async UniTask StartUP(PoolManager pool, GameStateManager gameStateManager)
    {
        _poolManager = pool;
        _gameStateManager = gameStateManager;

        // PoolManager を ExplorationState に設定.
        _gameStateManager.SetPoolManager(_poolManager);

        // デバッグキー初期化 (Z~Mキーのテスト機能).
        _debugKeyController = new DebugKeyController();
        _debugKeyController.Initialize(_gameStateManager);

        // 状態変更時のログ出力（デバッグ用）.
        _gameStateManager.CurrentStateType.Subscribe(stateType =>
        {
            Debug.Log($"GameState変更: {stateType}");
        });

        // 探索フェーズに切り替え (シーンロード完了を待つ).
        await _gameStateManager.SwitchToExplorationAsync();

        // 毎フレーム GameStateManager + DebugKeyController を更新.
        Observable.EveryUpdate().Subscribe(_ =>
        {
            _gameStateManager.Update();
            _debugKeyController.Update();
        });
    }
}
