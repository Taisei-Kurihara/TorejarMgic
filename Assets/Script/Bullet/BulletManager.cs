using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary> 攻撃ブロックとそれに連結された付与の管理 = 弾の発射管理. </summary>
/// <remarks>
/// 連結効果の解決結果を受けて弾のパラメータを設定し発射を制御する.
/// 発射ボタンの押下/保持/離しの3段階を管理.
/// </remarks>
public class BulletManager
{
    /// <summary> 弾定義. </summary>
    public IBullet BulletDef { get; private set; }

    /// <summary> 攻撃力 (連結効果解決後). </summary>
    public float AttackPower { get; set; }

    /// <summary> 弾の大きさ. </summary>
    public float BulletSize { get; set; } = 1f;

    /// <summary> 弾の速度. </summary>
    public float BulletSpeed { get; set; } = 10f;

    /// <summary> 弾の貫通回数. </summary>
    public int PenetrationCount { get; set; } = 1;

    // 発射前イベント用stateList (付与効果から追加).
    public List<IBulletState> FirePressedStates { get; } = new List<IBulletState>();
    public List<IBulletState> FireHeldStates { get; } = new List<IBulletState>();
    public List<IBulletState> FireReleasedStates { get; } = new List<IBulletState>();

    // 発射後イベント用stateList (付与効果から追加).
    public List<IBulletState> FiredStates { get; } = new List<IBulletState>();
    public List<IBulletState> FiredUpdateStates { get; } = new List<IBulletState>();
    public List<IBulletState> HitStates { get; } = new List<IBulletState>();

    // 発射済みの弾インスタンス.
    private readonly List<BulletInstance> _activeInstances = new List<BulletInstance>();

    private Transform _ownerTransform;
    private bool _isFireHeld;

    public BulletManager(IBullet bulletDef, Transform ownerTransform)
    {
        BulletDef = bulletDef;
        _ownerTransform = ownerTransform;
    }

    /// <summary> 連結効果の解決結果からパラメータを設定. </summary>
    public void ApplyResolvedEffect(AttackResolveData resolveData)
    {
        AttackPower = resolveData.ResolvedAttackPower;
    }

    /// <summary> 発射が押された時. </summary>
    public void OnFirePressed()
    {
        var context = CreateContext();
        _isFireHeld = true;

        // Manager自身のstateList実行.
        ExecuteStates(FirePressedStates, context);

        // Bullet定義のイベント実行.
        BulletDef.OnFirePressed(context);
    }

    /// <summary> 発射が押されている間 (毎フレーム呼び出し). </summary>
    public async UniTaskVoid OnFireHeldAsync()
    {
        while (_isFireHeld)
        {
            var context = CreateContext();
            ExecuteStates(FireHeldStates, context);
            BulletDef.OnFireHeld(context);
            await UniTask.Yield();
        }
    }

    /// <summary> 発射が離された時. </summary>
    public void OnFireReleased()
    {
        _isFireHeld = false;
        var context = CreateContext();

        ExecuteStates(FireReleasedStates, context);
        BulletDef.OnFireReleased(context);

        // 発射実行.
        Fire(context);
    }

    /// <summary> 弾を発射. </summary>
    private void Fire(BulletContext context)
    {
        for (int i = 0; i < BulletDef.FireCount; i++)
        {
            var instance = CreateInstance(context);
            _activeInstances.Add(instance);

            // 発射イベント.
            ExecuteStates(FiredStates, instance.Context);
            BulletDef.OnFired(instance.Context);
        }
    }

    /// <summary> 毎フレーム更新. アクティブな弾インスタンスを更新. </summary>
    public void Update()
    {
        for (int i = _activeInstances.Count - 1; i >= 0; i--)
        {
            var instance = _activeInstances[i];
            if (!instance.IsAlive)
            {
                if (instance.BulletObject != null)
                {
                    Object.Destroy(instance.BulletObject);
                }
                _activeInstances.RemoveAt(i);
                continue;
            }

            ExecuteStates(FiredUpdateStates, instance.Context);
            instance.Update();
        }
    }

    /// <summary> 弾インスタンスを生成. </summary>
    private BulletInstance CreateInstance(BulletContext baseContext)
    {
        // パラメータ設定.
        BulletDef.AttackPower = AttackPower;
        BulletDef.BulletSize = BulletSize;
        BulletDef.BulletSpeed = BulletSpeed;
        BulletDef.PenetrationCount = PenetrationCount;

        var context = CreateContext();
        return new BulletInstance(BulletDef, context);
    }

    private BulletContext CreateContext()
    {
        return new BulletContext
        {
            AttackPower = AttackPower,
            BulletSize = BulletSize,
            BulletSpeed = BulletSpeed,
            PenetrationCount = PenetrationCount,
            OwnerTransform = _ownerTransform
        };
    }

    private void ExecuteStates(List<IBulletState> states, BulletContext context)
    {
        for (int i = 0; i < states.Count; i++)
        {
            states[i].Execute(context);
        }
    }

    /// <summary> 全弾インスタンスを破棄. </summary>
    public void Dispose()
    {
        _isFireHeld = false;
        for (int i = 0; i < _activeInstances.Count; i++)
        {
            if (_activeInstances[i].BulletObject != null)
            {
                Object.Destroy(_activeInstances[i].BulletObject);
            }
        }
        _activeInstances.Clear();
    }
}
