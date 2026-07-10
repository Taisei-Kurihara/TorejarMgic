using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GamePresenter : Singleton<GamePresenter>
{
    #region ローカル変数

    private PoolManager _poolManager;
    private ILoadsystem _loadSystem;
    private InGameManager _inGameManager;
    private GameStateManager _gameStateManager;
    private CharacterStatusManager _characterStatusManager;

    #endregion

    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary> InGameManagerの初期化とゲーム開始. </summary>
    public void StartInGame()
    {
        InitGame(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTask InitGame(CancellationToken cancellationToken)
    {
        // シングルトン初期化.
        _gameStateManager = GameStateManager.Instance;
        _characterStatusManager = CharacterStatusManager.Instance;

        // CSVからステータスデータ読み込み.
        _characterStatusManager.LoadFromCsv("CharacterStatus.csv");

        _poolManager = new PoolManager();
        _loadSystem = new Loadsystem_AddressablesManager();

        _inGameManager = new InGameManager();
        await _inGameManager.StartUP(_poolManager, _gameStateManager);
    }
}
