using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

/// <summary> プレイヤーステータス管理. </summary>
/// <remarks> MonoBehaviourなし. OnDestroyAsObservableでGameObjectのライフサイクルを監視. </remarks>
public class Player : CharacterStatus_abstract
{
    public GameObject PlayerObj { get; private set; }

    private IDisposable _destroySubscription;

    /// <summary> 初期化. playerobj の OnDestroy を UniRx で監視. </summary>
    public void Init(string characterName, GameObject playerObj)
    {
        base.Init(characterName);
        PlayerObj = playerObj;

        _destroySubscription = PlayerObj.OnDestroyAsObservable().Subscribe(_ =>
        {
            Debug.Log($"Player '{Name}' のGameObjectが破棄されました.");
            OnPlayerDestroyed();
        });
    }

    /// <summary> PlayerObj 破棄時の処理. 必要に応じてオーバーライド. </summary>
    protected virtual void OnPlayerDestroyed()
    {
        PlayerObj = null;
    }

    /// <summary> 明示的な破棄. </summary>
    public void Dispose()
    {
        _destroySubscription?.Dispose();
        _destroySubscription = null;
    }
}
