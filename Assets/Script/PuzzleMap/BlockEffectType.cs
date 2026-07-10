/// <summary> ブロックの効果種別. </summary>
public enum BlockEffectType
{
    /// <summary> 効果なし. </summary>
    None,
    /// <summary> 攻撃 — 攻撃どうしは連結しない. </summary>
    Attack,
    /// <summary> 付与 — 攻撃or付与に繋がる. 付与塊を形成. </summary>
    Grant,
    /// <summary> 補助 — つながらない. playerの基礎statusへのパッシブ効果. </summary>
    Support
}
