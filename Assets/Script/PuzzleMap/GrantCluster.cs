using System.Collections.Generic;
using UnityEngine;

/// <summary> 連結した付与ブロックの塊. 攻撃から遠い順に効果を解決する. </summary>
public class GrantCluster
{
    /// <summary> この塊に属する付与ブロックの座標リスト (攻撃から遠い順にソート済). </summary>
    public List<Vector3Int> Positions = new List<Vector3Int>();

    /// <summary> 各座標に対応する効果データ (Positionsと同インデックス). </summary>
    public List<BlockData> Effects = new List<BlockData>();

    /// <summary> この塊に隣接する攻撃ブロックの座標. </summary>
    public List<Vector3Int> AdjacentAttacks = new List<Vector3Int>();
}

/// <summary> 攻撃ブロック1つ分の解決データ. </summary>
public class AttackResolveData
{
    /// <summary> 攻撃ブロックの座標. </summary>
    public Vector3Int Position;

    /// <summary> 攻撃ブロック自身の効果. </summary>
    public BlockData AttackEffect;

    /// <summary> この攻撃に連結された付与塊のリスト. </summary>
    public List<GrantCluster> LinkedClusters = new List<GrantCluster>();

    /// <summary> 付与効果適用後の最終攻撃力. </summary>
    public float ResolvedAttackPower;
}

/// <summary> 連結解決の全体結果. </summary>
public class LinkResolveResult
{
    /// <summary> 攻撃ブロックごとの解決データ. </summary>
    public List<AttackResolveData> Attacks = new List<AttackResolveData>();

    /// <summary> 補助ブロック (パッシブ効果) のリスト. </summary>
    public List<BlockData> SupportEffects = new List<BlockData>();
}
