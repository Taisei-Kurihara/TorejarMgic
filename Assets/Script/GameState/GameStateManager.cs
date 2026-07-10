using System;
using Cysharp.Threading.Tasks;
using UniRx;

/// <summary> ゲーム状態の種別. </summary>
public enum GameStateType
{
    Puzzle,
    Exploration
}

/// <summary> puzzle/探索の状態切り替えを管理するシングルトン. </summary>
/// <remarks> MonoBehaviourなし. 純粋C#シングルトン. Stateクラスパターン. シーンロードを含む非同期遷移. </remarks>
public class GameStateManager
{
    private static GameStateManager _instance;
    public static GameStateManager Instance => _instance ??= new GameStateManager();

    private readonly PuzzleState _puzzleState = new PuzzleState();
    private readonly ExplorationState _explorationState = new ExplorationState();

    private readonly ReactiveProperty<GameStateType> _currentStateType = new ReactiveProperty<GameStateType>(GameStateType.Puzzle);
    private IGameState _currentState;
    private bool _isTransitioning;

    /// <summary> 現在のゲーム状態種別. </summary>
    public IReadOnlyReactiveProperty<GameStateType> CurrentStateType => _currentStateType;

    /// <summary> 現在のStateクラスインスタンス. </summary>
    public IGameState CurrentState => _currentState;

    /// <summary> シーンロード中かどうか. </summary>
    public bool IsTransitioning => _isTransitioning;

    /// <summary> 状態変更時のイベント. 旧状態と新状態を通知. </summary>
    public IObservable<(GameStateType previous, GameStateType current)> OnStateChanged =>
        _currentStateType.Pairwise().Select(pair => (pair.Previous, pair.Current));

    private GameStateManager()
    {
        // 初期状態は InGameManager からの最初の ChangeStateAsync 呼び出しで設定.
        _currentState = null;
    }

    /// <summary> PoolManager を ExplorationState に設定. </summary>
    public void SetPoolManager(PoolManager pool)
    {
        _explorationState.SetPoolManager(pool);
    }

    /// <summary> パズルフェーズに切り替え (非同期). </summary>
    public UniTask SwitchToPuzzleAsync() => ChangeStateAsync(GameStateType.Puzzle);

    /// <summary> 探索フェーズに切り替え (非同期). </summary>
    public UniTask SwitchToExplorationAsync() => ChangeStateAsync(GameStateType.Exploration);

    /// <summary> 状態をトグル切り替え (非同期). </summary>
    public UniTask ToggleStateAsync()
    {
        var next = _currentStateType.Value == GameStateType.Puzzle
            ? GameStateType.Exploration
            : GameStateType.Puzzle;
        return ChangeStateAsync(next);
    }

    /// <summary> 毎フレーム呼び出し. 遷移中はスキップ. </summary>
    public void Update()
    {
        if (_isTransitioning) return;
        _currentState?.Update();
    }

    /// <summary> 現在パズルフェーズかどうか. </summary>
    public bool IsPuzzle => _currentStateType.Value == GameStateType.Puzzle;

    /// <summary> 現在探索フェーズかどうか. </summary>
    public bool IsExploration => _currentStateType.Value == GameStateType.Exploration;

    private bool _explorationInitialized;

    private async UniTask ChangeStateAsync(GameStateType newStateType)
    {
        if (_isTransitioning) return;

        // 初回は _currentState が null なので同一状態チェックをスキップ.
        if (_currentState != null && _currentStateType.Value == newStateType) return;

        _isTransitioning = true;

        if (_currentState == null)
        {
            // 初回: 探索を初期化.
            _currentStateType.Value = newStateType;
            var nextState = GetState(newStateType);
            await nextState.EnterAsync();
            _currentState = nextState;
            if (newStateType == GameStateType.Exploration) _explorationInitialized = true;
        }
        else if (newStateType == GameStateType.Puzzle)
        {
            // 探索 → Puzzle: 探索の入力/カメラを無効化, Puzzle を Additive ロード.
            _explorationState.SetInputActive(false);
            _currentStateType.Value = newStateType;
            await _puzzleState.EnterAsync();
            _currentState = _puzzleState;
        }
        else
        {
            // Puzzle → 探索: Puzzle をアンロード, 探索の入力/カメラを再有効化.
            _puzzleState.Exit();
            _currentStateType.Value = newStateType;
            _explorationState.SetInputActive(true);
            _currentState = _explorationState;
        }

        _isTransitioning = false;
    }

    private IGameState GetState(GameStateType stateType)
    {
        return stateType switch
        {
            GameStateType.Puzzle => _puzzleState,
            GameStateType.Exploration => _explorationState,
            _ => _puzzleState
        };
    }
}
