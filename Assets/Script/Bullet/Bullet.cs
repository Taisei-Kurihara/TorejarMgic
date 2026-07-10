using System.Collections.Generic;
using UnityEngine;

/// <summary> 弾の実装. prefab/飛び方 + stateListで挙動を定義. </summary>
/// <remarks>
/// 種類/属性はこのクラスで定義.
/// 攻撃力/大きさ/速度/貫通回数は初期化時に外部(BulletManager)から設定.
/// </remarks>
public class Bullet : IBullet
{
    #region stateList

    public List<IBulletState> FirePressedStates { get; } = new List<IBulletState>();
    public List<IBulletState> FireHeldStates { get; } = new List<IBulletState>();
    public List<IBulletState> FireReleasedStates { get; } = new List<IBulletState>();
    public List<IBulletState> FiredStates { get; } = new List<IBulletState>();
    public List<IBulletState> FiredUpdateStates { get; } = new List<IBulletState>();
    public List<IBulletState> HitStates { get; } = new List<IBulletState>();

    #endregion

    #region パラメータ

    public int FireCount { get; set; } = 1;
    public List<string> BulletTypes { get; } = new List<string>();
    public List<string> BulletAttributes { get; } = new List<string>();

    // 以下は初期化時に外部から設定される.
    public float AttackPower { get; set; }
    public float BulletSize { get; set; } = 1f;
    public float BulletSpeed { get; set; } = 10f;
    public int PenetrationCount { get; set; } = 1;

    /// <summary> 弾prefabのアドレス (Addressables用). </summary>
    public string PrefabAddress { get; set; } = "";

    #endregion

    public void OnFirePressed(BulletContext context)
    {
        ExecuteStates(FirePressedStates, context);
    }

    public void OnFireHeld(BulletContext context)
    {
        ExecuteStates(FireHeldStates, context);
    }

    public void OnFireReleased(BulletContext context)
    {
        ExecuteStates(FireReleasedStates, context);
    }

    public void OnFired(BulletContext context)
    {
        ExecuteStates(FiredStates, context);
    }

    public void OnFiredUpdate(BulletContext context)
    {
        ExecuteStates(FiredUpdateStates, context);
    }

    public void OnHit(BulletContext context)
    {
        ExecuteStates(HitStates, context);
    }

    /// <summary> stateListを順次実行. </summary>
    private void ExecuteStates(List<IBulletState> states, BulletContext context)
    {
        for (int i = 0; i < states.Count; i++)
        {
            states[i].Execute(context);
        }
    }
}
