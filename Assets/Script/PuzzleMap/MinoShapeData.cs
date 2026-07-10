using System.Collections.Generic;
using UnityEngine;

/// <summary> ミノ形状の定義データ (CSVから読み込み). </summary>
[System.Serializable]
public class MinoShapeData
{
    /// <summary> 形状ID. </summary>
    public string ShapeId;

    /// <summary> 各ブロックの中心からのオフセット (XZ平面, y=0固定: ミノはy軸に厚さを持たない). </summary>
    public List<Vector3Int> BlockOffsets = new List<Vector3Int>();

    /// <summary> 各ブロックに対応する効果データ. BlockOffsetsと同じインデックス. </summary>
    public List<BlockData> BlockEffects = new List<BlockData>();

    /// <summary> ブロック数. </summary>
    public int BlockCount => BlockOffsets.Count;
}
