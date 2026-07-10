using System.Collections.Generic;
using UnityEngine;

/// <summary> パズルマップ上のブロック連結を探索し効果を解決する. </summary>
public static class BlockLinkResolver
{
    // 6方向隣接 (上下左右前後).
    private static readonly Vector3Int[] Directions =
    {
        Vector3Int.right, Vector3Int.left,
        Vector3Int.up, Vector3Int.down,
        new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
    };

    /// <summary> マップ全体の連結を解析し結果を返す. </summary>
    public static LinkResolveResult Resolve(PuzzleMapModel model)
    {
        var result = new LinkResolveResult();
        var visited = new HashSet<Vector3Int>();

        // 補助ブロック収集 (つながらない).
        CollectSupportBlocks(model, result);

        // 付与塊を探索.
        var grantClusters = FindAllGrantClusters(model, visited);

        // 攻撃ブロックを収集.
        var attackDataMap = FindAllAttacks(model);

        // 付与塊と攻撃の連結を構築.
        LinkClustersToAttacks(model, grantClusters, attackDataMap);

        result.Attacks.AddRange(attackDataMap.Values);
        return result;
    }

    /// <summary> マップ全体の連結を解析 (1対1接続マップ使用). </summary>
    public static LinkResolveResult Resolve(PuzzleMapModel model, Dictionary<Vector3Int, Vector3Int> grantConnections)
    {
        var result = new LinkResolveResult();
        var visited = new HashSet<Vector3Int>();

        // 補助ブロック収集.
        CollectSupportBlocks(model, result);

        // 付与塊を探索.
        var grantClusters = FindAllGrantClusters(model, visited);

        // 攻撃ブロックを収集.
        var attackDataMap = FindAllAttacks(model);

        // 付与塊と攻撃の1対1連結を構築.
        LinkClustersToAttacksOneToOne(model, grantClusters, attackDataMap, grantConnections);

        result.Attacks.AddRange(attackDataMap.Values);
        return result;
    }

    /// <summary> 解決結果に対して付与効果を適用し最終攻撃力を計算. </summary>
    /// <param name="clusterOrder"> 同一攻撃に複数の付与塊がある場合の適用順序 (インデックスリスト). nullなら検出順. </param>
    public static void ApplyGrantEffects(AttackResolveData attackData, List<int> clusterOrder = null)
    {
        float power = attackData.AttackEffect.EffectValue;

        var clusters = attackData.LinkedClusters;
        if (clusters.Count == 0)
        {
            attackData.ResolvedAttackPower = power;
            return;
        }

        // 適用順序.
        var order = clusterOrder ?? CreateDefaultOrder(clusters.Count);

        for (int i = 0; i < order.Count; i++)
        {
            int clusterIndex = order[i];
            if (clusterIndex < 0 || clusterIndex >= clusters.Count) continue;

            var cluster = clusters[clusterIndex];

            // 付与塊内は攻撃から遠い順に解決 (Positionsは遠い順にソート済).
            for (int e = 0; e < cluster.Effects.Count; e++)
            {
                power = ApplySingleGrantEffect(power, cluster.Effects[e]);
            }
        }

        attackData.ResolvedAttackPower = power;
    }

    /// <summary> 全攻撃の効果を一括解決. </summary>
    public static void ApplyAllGrantEffects(LinkResolveResult result)
    {
        for (int i = 0; i < result.Attacks.Count; i++)
        {
            ApplyGrantEffects(result.Attacks[i]);
        }
    }

    #region 内部処理

    /// <summary> 補助ブロックを収集. </summary>
    private static void CollectSupportBlocks(PuzzleMapModel model, LinkResolveResult result)
    {
        var size = model.Size;
        for (int x = 0; x < size.x; x++)
        for (int y = 0; y < size.y; y++)
        for (int z = 0; z < size.z; z++)
        {
            var block = model.GetBlock(x, y, z);
            if (block != null && block.EffectType == BlockEffectType.Support)
            {
                result.SupportEffects.Add(block);
            }
        }
    }

    /// <summary> 全付与塊をBFS探索 (visited自動生成). </summary>
    public static List<GrantCluster> FindAllGrantClusters(PuzzleMapModel model)
    {
        return FindAllGrantClusters(model, new HashSet<Vector3Int>());
    }

    /// <summary> 全付与塊をBFS探索. </summary>
    public static List<GrantCluster> FindAllGrantClusters(PuzzleMapModel model, HashSet<Vector3Int> visited)
    {
        var clusters = new List<GrantCluster>();
        var size = model.Size;

        for (int x = 0; x < size.x; x++)
        for (int y = 0; y < size.y; y++)
        for (int z = 0; z < size.z; z++)
        {
            var pos = new Vector3Int(x, y, z);
            var block = model.GetBlock(pos);
            if (block == null || block.EffectType != BlockEffectType.Grant) continue;
            if (visited.Contains(pos)) continue;

            var cluster = BfsGrantCluster(model, pos, visited);
            if (cluster.Positions.Count > 0)
            {
                clusters.Add(cluster);
            }
        }

        return clusters;
    }

    /// <summary> 1つの付与塊をBFS探索. </summary>
    private static GrantCluster BfsGrantCluster(PuzzleMapModel model, Vector3Int start, HashSet<Vector3Int> visited)
    {
        var cluster = new GrantCluster();
        var queue = new Queue<Vector3Int>();
        var adjacentAttacks = new HashSet<Vector3Int>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var block = model.GetBlock(current);

            cluster.Positions.Add(current);
            cluster.Effects.Add(block);

            for (int d = 0; d < Directions.Length; d++)
            {
                var neighbor = current + Directions[d];
                var neighborBlock = model.GetBlock(neighbor);
                if (neighborBlock == null) continue;

                // 隣接が攻撃ならリンク候補に追加.
                if (neighborBlock.EffectType == BlockEffectType.Attack)
                {
                    adjacentAttacks.Add(neighbor);
                    continue;
                }

                // 隣接が付与なら塊に追加.
                if (neighborBlock.EffectType == BlockEffectType.Grant && !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        cluster.AdjacentAttacks = new List<Vector3Int>(adjacentAttacks);
        return cluster;
    }

    /// <summary> 全攻撃ブロックを収集. </summary>
    private static Dictionary<Vector3Int, AttackResolveData> FindAllAttacks(PuzzleMapModel model)
    {
        var attacks = new Dictionary<Vector3Int, AttackResolveData>();
        var size = model.Size;

        for (int x = 0; x < size.x; x++)
        for (int y = 0; y < size.y; y++)
        for (int z = 0; z < size.z; z++)
        {
            var pos = new Vector3Int(x, y, z);
            var block = model.GetBlock(pos);
            if (block != null && block.EffectType == BlockEffectType.Attack)
            {
                attacks[pos] = new AttackResolveData
                {
                    Position = pos,
                    AttackEffect = block,
                    ResolvedAttackPower = block.EffectValue
                };
            }
        }

        return attacks;
    }

    /// <summary> 付与塊と攻撃の連結を構築. 攻撃から遠い順にソート. </summary>
    private static void LinkClustersToAttacks(
        PuzzleMapModel model,
        List<GrantCluster> clusters,
        Dictionary<Vector3Int, AttackResolveData> attackMap)
    {
        for (int c = 0; c < clusters.Count; c++)
        {
            var cluster = clusters[c];

            // 隣接する攻撃それぞれに対してこの付与塊を登録.
            for (int a = 0; a < cluster.AdjacentAttacks.Count; a++)
            {
                var attackPos = cluster.AdjacentAttacks[a];
                if (!attackMap.ContainsKey(attackPos)) continue;

                // 攻撃からの距離でソート (遠い順).
                var sorted = SortByDistanceFromAttack(cluster, attackPos);
                var sortedCluster = new GrantCluster
                {
                    Positions = sorted.positions,
                    Effects = sorted.effects,
                    AdjacentAttacks = cluster.AdjacentAttacks
                };

                attackMap[attackPos].LinkedClusters.Add(sortedCluster);
            }
        }
    }

    /// <summary> 攻撃ブロックからのBFS距離で付与塊内を遠い順にソート. </summary>
    private static (List<Vector3Int> positions, List<BlockData> effects) SortByDistanceFromAttack(
        GrantCluster cluster, Vector3Int attackPos)
    {
        var posSet = new HashSet<Vector3Int>(cluster.Positions);
        var distMap = new Dictionary<Vector3Int, int>();

        // 攻撃隣接のGrantからBFS開始.
        var queue = new Queue<Vector3Int>();
        for (int i = 0; i < cluster.Positions.Count; i++)
        {
            var pos = cluster.Positions[i];
            for (int d = 0; d < Directions.Length; d++)
            {
                if (pos + Directions[d] == attackPos)
                {
                    if (!distMap.ContainsKey(pos))
                    {
                        distMap[pos] = 0;
                        queue.Enqueue(pos);
                    }
                    break;
                }
            }
        }

        // BFSで全Grantの距離を計算.
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int currentDist = distMap[current];

            for (int d = 0; d < Directions.Length; d++)
            {
                var neighbor = current + Directions[d];
                if (posSet.Contains(neighbor) && !distMap.ContainsKey(neighbor))
                {
                    distMap[neighbor] = currentDist + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        // 距離が遠い順にソート.
        var indexed = new List<(int index, int dist)>();
        for (int i = 0; i < cluster.Positions.Count; i++)
        {
            int dist = distMap.ContainsKey(cluster.Positions[i]) ? distMap[cluster.Positions[i]] : 999;
            indexed.Add((i, dist));
        }
        indexed.Sort((a, b) => b.dist.CompareTo(a.dist));

        var sortedPositions = new List<Vector3Int>();
        var sortedEffects = new List<BlockData>();
        for (int i = 0; i < indexed.Count; i++)
        {
            sortedPositions.Add(cluster.Positions[indexed[i].index]);
            sortedEffects.Add(cluster.Effects[indexed[i].index]);
        }

        return (sortedPositions, sortedEffects);
    }

    /// <summary> 付与効果1つを攻撃力に適用. effectIdで処理分岐. </summary>
    private static float ApplySingleGrantEffect(float currentPower, BlockData grantEffect)
    {
        // effectIdに基づく処理分岐 (拡張可能).
        var id = grantEffect.EffectId.ToLower();

        if (id.Contains("multiply") || id.Contains("*"))
        {
            return currentPower * grantEffect.EffectValue;
        }

        // デフォルトは加算.
        return currentPower + grantEffect.EffectValue;
    }

    /// <summary> 付与塊と攻撃の1対1連結を構築 (grantConnections登録ありのみリンク). </summary>
    private static void LinkClustersToAttacksOneToOne(
        PuzzleMapModel model,
        List<GrantCluster> clusters,
        Dictionary<Vector3Int, AttackResolveData> attackMap,
        Dictionary<Vector3Int, Vector3Int> grantConnections)
    {
        for (int c = 0; c < clusters.Count; c++)
        {
            var cluster = clusters[c];
            if (cluster.Positions.Count == 0) continue;

            // grantConnectionsにこのクラスタの登録があるか確認.
            Vector3Int? connectedAttack = null;
            if (grantConnections.TryGetValue(cluster.Positions[0], out var attackPos))
            {
                connectedAttack = attackPos;
            }

            // 未接続 → どの攻撃にもリンクしない.
            if (!connectedAttack.HasValue) continue;

            // 接続先が攻撃マップにない → スキップ.
            if (!attackMap.ContainsKey(connectedAttack.Value)) continue;

            // 接続先攻撃に対してのみリンク.
            var sorted = SortByDistanceFromAttack(cluster, connectedAttack.Value);
            var sortedCluster = new GrantCluster
            {
                Positions = sorted.positions,
                Effects = sorted.effects,
                AdjacentAttacks = cluster.AdjacentAttacks
            };

            attackMap[connectedAttack.Value].LinkedClusters.Add(sortedCluster);
        }
    }

    private static List<int> CreateDefaultOrder(int count)
    {
        var order = new List<int>(count);
        for (int i = 0; i < count; i++) order.Add(i);
        return order;
    }

    #endregion
}
