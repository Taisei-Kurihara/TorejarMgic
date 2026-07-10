using UnityEngine;

/// <summary> 弾のStateに渡されるコンテキスト. </summary>
public class BulletContext
{
    /// <summary> 弾の実体オブジェクト. </summary>
    public GameObject BulletObject;

    /// <summary> 現在の攻撃力. </summary>
    public float AttackPower;

    /// <summary> 弾の大きさ. </summary>
    public float BulletSize;

    /// <summary> 弾の速度. </summary>
    public float BulletSpeed;

    /// <summary> 弾の貫通回数. </summary>
    public int PenetrationCount;

    /// <summary> 発射元のTransform. </summary>
    public Transform OwnerTransform;

    /// <summary> hit対象 (OnHit時に設定). </summary>
    public GameObject HitTarget;
}
