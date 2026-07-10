using System.Collections.Generic;
using UnityEngine;

/// <summary> 弾の実体ごとの管理データ. </summary>
public class BulletInstance
{
    /// <summary> 弾のGameObject. </summary>
    public GameObject BulletObject { get; set; }

    /// <summary> 参照元のBullet定義. </summary>
    public IBullet BulletDef { get; private set; }

    /// <summary> コンテキスト. </summary>
    public BulletContext Context { get; private set; }

    /// <summary> 現在の残り貫通回数. </summary>
    public int CurrentPenetration { get; set; }

    /// <summary> hitした対象 (重複hit防止). </summary>
    public HashSet<GameObject> HitTargets { get; } = new HashSet<GameObject>();

    /// <summary> 生存中かどうか. </summary>
    public bool IsAlive { get; set; } = true;

    public BulletInstance(IBullet bulletDef, BulletContext context)
    {
        BulletDef = bulletDef;
        Context = context;
        CurrentPenetration = bulletDef.PenetrationCount;
    }

    /// <summary> hit処理. 重複チェック + 貫通管理. </summary>
    /// <returns> hitが有効だったか. </returns>
    public bool TryHit(GameObject target)
    {
        if (!IsAlive) return false;
        if (target == null) return false;
        if (HitTargets.Contains(target)) return false;

        HitTargets.Add(target);
        Context.HitTarget = target;
        BulletDef.OnHit(Context);

        CurrentPenetration--;
        if (CurrentPenetration <= 0)
        {
            IsAlive = false;
        }

        return true;
    }

    /// <summary> 毎フレーム更新. </summary>
    public void Update()
    {
        if (!IsAlive) return;
        BulletDef.OnFiredUpdate(Context);
    }
}
