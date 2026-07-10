using System.Collections.Generic;
using UnityEngine;

/// <summary> ランタイム上のミノインスタンス. 配置位置と回転状態を持つ. </summary>
public class MinoInstance
{
    /// <summary> 形状データへの参照. </summary>
    public MinoShapeData ShapeData { get; private set; }

    /// <summary> グリッド上の配置位置. </summary>
    public Vector3Int Position { get; set; }

    /// <summary> XZ平面上の回転 (0=0度, 1=90度, 2=180度, 3=270度). </summary>
    public int Rotation { get; set; }

    /// <summary> 配置済みかどうか. </summary>
    public bool IsPlaced { get; set; }

    public MinoInstance(MinoShapeData shapeData)
    {
        ShapeData = shapeData;
        Position = Vector3Int.zero;
        Rotation = 0;
        IsPlaced = false;
    }

    /// <summary> 回転・位置オフセット適用後のワールドブロック座標リストを取得. </summary>
    public List<Vector3Int> GetWorldBlockPositions()
    {
        var result = new List<Vector3Int>(ShapeData.BlockCount);
        for (int i = 0; i < ShapeData.BlockOffsets.Count; i++)
        {
            var rotated = RotateOffset(ShapeData.BlockOffsets[i], Rotation);
            result.Add(Position + rotated);
        }
        return result;
    }

    /// <summary> XZ平面で90度単位の回転を適用. y軸はそのまま. </summary>
    private Vector3Int RotateOffset(Vector3Int offset, int rotation)
    {
        int r = ((rotation % 4) + 4) % 4;
        return r switch
        {
            0 => offset,
            1 => new Vector3Int(offset.z, offset.y, -offset.x),
            2 => new Vector3Int(-offset.x, offset.y, -offset.z),
            3 => new Vector3Int(-offset.z, offset.y, offset.x),
            _ => offset
        };
    }

    /// <summary> 左回転 (反時計回り). </summary>
    public void RotateLeft()
    {
        Rotation = ((Rotation - 1) % 4 + 4) % 4;
    }

    /// <summary> 右回転 (時計回り). </summary>
    public void RotateRight()
    {
        Rotation = (Rotation + 1) % 4;
    }

    /// <summary> y軸上方向に移動. </summary>
    public void MoveUp()
    {
        Position += Vector3Int.up;
    }

    /// <summary> y軸下方向に移動. </summary>
    public void MoveDown()
    {
        Position += Vector3Int.down;
    }

    /// <summary> XZ平面で移動. </summary>
    public void MoveXZ(Vector2Int delta)
    {
        Position += new Vector3Int(delta.x, 0, delta.y);
    }
}
