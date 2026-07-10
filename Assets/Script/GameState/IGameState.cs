using Cysharp.Threading.Tasks;

/// <summary> ゲーム状態の基底インターフェース. </summary>
public interface IGameState
{
    /// <summary> この状態に入った時に呼ばれる (シーンロード等の非同期処理を含む). </summary>
    UniTask EnterAsync();

    /// <summary> この状態から出る時に呼ばれる. </summary>
    void Exit();

    /// <summary> 毎フレーム更新. </summary>
    void Update();
}
