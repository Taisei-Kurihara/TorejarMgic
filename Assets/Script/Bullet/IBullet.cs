using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary> 弾のインターフェース. 6イベント + 各stateList を定義. </summary>
public interface IBullet
{
    /// <summary> 発射が押された時. </summary>
    void OnFirePressed(BulletContext context);

    /// <summary> 発射が押されている間 (毎フレーム). </summary>
    void OnFireHeld(BulletContext context);

    /// <summary> 発射が離された時. </summary>
    void OnFireReleased(BulletContext context);

    /// <summary> 弾が発射された時. </summary>
    void OnFired(BulletContext context);

    /// <summary> 弾が発射された後の毎フレーム更新. </summary>
    void OnFiredUpdate(BulletContext context);

    /// <summary> 弾がhitした時. </summary>
    void OnHit(BulletContext context);

    /// <summary> 発射が押された時 stateList. </summary>
    List<IBulletState> FirePressedStates { get; }

    /// <summary> 発射が押されている間 stateList. </summary>
    List<IBulletState> FireHeldStates { get; }

    /// <summary> 発射が離された時 stateList. </summary>
    List<IBulletState> FireReleasedStates { get; }

    /// <summary> 発射された時 stateList. </summary>
    List<IBulletState> FiredStates { get; }

    /// <summary> 発射された後update stateList. </summary>
    List<IBulletState> FiredUpdateStates { get; }

    /// <summary> hit時 stateList. </summary>
    List<IBulletState> HitStates { get; }

    /// <summary> 発射される数. </summary>
    int FireCount { get; }

    /// <summary> 弾の種類 (複数持てる). </summary>
    List<string> BulletTypes { get; }

    /// <summary> 弾の属性 (複数持てる). </summary>
    List<string> BulletAttributes { get; }

    /// <summary> 攻撃力. </summary>
    float AttackPower { get; set; }

    /// <summary> 弾の大きさ. </summary>
    float BulletSize { get; set; }

    /// <summary> 弾の速度. </summary>
    float BulletSpeed { get; set; }

    /// <summary> 弾の貫通回数. </summary>
    int PenetrationCount { get; set; }
}
