using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

/// <summary> 敵ステータス管理の基底クラス. </summary>
/// <remarks> MonoBehaviourなし. OnDestroyAsObservableでGameObjectのライフサイクルを監視. </remarks>
public abstract class Enemy_Base_abstract : CharacterStatus_abstract
{
    public GameObject EnemyObj { get; private set; }

    private IDisposable _destroySubscription;

    /// <summary> 初期化. enemyobj の OnDestroy を UniRx で監視. </summary>
    public virtual void Init(string characterName, GameObject enemyObj)
    {
        base.Init(characterName);
        EnemyObj = enemyObj;

        _destroySubscription = EnemyObj.OnDestroyAsObservable().Subscribe(_ =>
        {
            Debug.Log($"Enemy '{Name}' のGameObjectが破棄されました.");
            OnEnemyDestroyed();
        });
    }

    /// <summary> EnemyObj 破棄時の処理. 継承先でオーバーライド可能. </summary>
    protected virtual void OnEnemyDestroyed()
    {
        EnemyObj = null;
    }

    /// <summary> 明示的な破棄. </summary>
    public void Dispose()
    {
        _destroySubscription?.Dispose();
        _destroySubscription = null;
    }
}
