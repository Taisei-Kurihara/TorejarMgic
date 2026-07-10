using System.Collections.Generic;
using UnityEngine;

/// <summary> パズルマップの3Dグリッドデータ管理 (内部位置管理). </summary>
/// <remarks> Viewに依存しない純粋なデータ層. </remarks>
public class PuzzleMapModel
{
    private readonly BlockData[,,] _grid; // x, y, z.
    private readonly Vector3Int _size;

    // 付与ブロック座標 → 接続先攻撃座標 (1対1). 同一付与塊の全ブロックが同じ攻撃を指す.
    private readonly Dictionary<Vector3Int, Vector3Int> _grantConnections = new Dictionary<Vector3Int, Vector3Int>();

    /// <summary> グリッドサイズ. </summary>
    public Vector3Int Size => _size;

    public PuzzleMapModel(Vector3Int size)
    {
        _size = size;
        _grid = new BlockData[size.x, size.y, size.z];
    }

    /// <summary> 座標がグリッド範囲内か. </summary>
    public bool IsInBounds(int x, int y, int z)
    {
        return x >= 0 && x < _size.x
            && y >= 0 && y < _size.y
            && z >= 0 && z < _size.z;
    }

    /// <summary> 座標がグリッド範囲内か. </summary>
    public bool IsInBounds(Vector3Int pos)
    {
        return IsInBounds(pos.x, pos.y, pos.z);
    }

    /// <summary> 指定座標が空か. </summary>
    public bool IsEmpty(int x, int y, int z)
    {
        if (!IsInBounds(x, y, z)) return false;
        return _grid[x, y, z] == null;
    }

    /// <summary> 指定座標が空か. </summary>
    public bool IsEmpty(Vector3Int pos)
    {
        return IsEmpty(pos.x, pos.y, pos.z);
    }

    /// <summary> 指定座標のブロックデータを取得. 空ならnull. </summary>
    public BlockData GetBlock(int x, int y, int z)
    {
        if (!IsInBounds(x, y, z)) return null;
        return _grid[x, y, z];
    }

    /// <summary> 指定座標のブロックデータを取得. </summary>
    public BlockData GetBlock(Vector3Int pos)
    {
        return GetBlock(pos.x, pos.y, pos.z);
    }

    /// <summary> 指定座標にブロックデータを配置. </summary>
    public void SetBlock(int x, int y, int z, BlockData data)
    {
        if (!IsInBounds(x, y, z)) return;
        _grid[x, y, z] = data;
    }

    /// <summary> 指定座標にブロックデータを配置. </summary>
    public void SetBlock(Vector3Int pos, BlockData data)
    {
        SetBlock(pos.x, pos.y, pos.z, data);
    }

    /// <summary> 指定座標のブロックを除去. </summary>
    public void ClearBlock(int x, int y, int z)
    {
        if (!IsInBounds(x, y, z)) return;
        _grid[x, y, z] = null;
    }

    /// <summary> ミノが配置可能か判定. 全ブロックが範囲内かつ空であること. </summary>
    public bool CanPlaceMino(MinoInstance mino)
    {
        var positions = mino.GetWorldBlockPositions();
        for (int i = 0; i < positions.Count; i++)
        {
            if (!IsEmpty(positions[i])) return false;
        }
        return true;
    }

    /// <summary> ミノを配置. 各ブロック位置にBlockDataを書き込む. </summary>
    public void PlaceMino(MinoInstance mino)
    {
        var positions = mino.GetWorldBlockPositions();
        var effects = mino.ShapeData.BlockEffects;

        for (int i = 0; i < positions.Count; i++)
        {
            var effect = (i < effects.Count) ? effects[i].Clone() : new BlockData();
            SetBlock(positions[i], effect);
        }
    }

    /// <summary> ミノを除去. 各ブロック位置をクリア. </summary>
    public void RemoveMino(MinoInstance mino)
    {
        var positions = mino.GetWorldBlockPositions();
        for (int i = 0; i < positions.Count; i++)
        {
            var pos = positions[i];
            if (IsInBounds(pos))
            {
                ClearBlock(pos.x, pos.y, pos.z);
            }
        }
    }

    /// <summary> 指定y層の全ブロック位置を取得. </summary>
    public List<Vector3Int> GetBlocksAtLayer(int y)
    {
        var result = new List<Vector3Int>();
        if (y < 0 || y >= _size.y) return result;

        for (int x = 0; x < _size.x; x++)
        {
            for (int z = 0; z < _size.z; z++)
            {
                if (_grid[x, y, z] != null)
                {
                    result.Add(new Vector3Int(x, y, z));
                }
            }
        }
        return result;
    }

    /// <summary> 全配置済みブロック位置を取得. </summary>
    public List<Vector3Int> GetAllOccupiedPositions()
    {
        var result = new List<Vector3Int>();
        for (int x = 0; x < _size.x; x++)
        {
            for (int y = 0; y < _size.y; y++)
            {
                for (int z = 0; z < _size.z; z++)
                {
                    if (_grid[x, y, z] != null)
                    {
                        result.Add(new Vector3Int(x, y, z));
                    }
                }
            }
        }
        return result;
    }

    #region 付与→攻撃 接続管理

    /// <summary> 付与塊全体の接続先攻撃を設定. </summary>
    public void SetGrantConnection(List<Vector3Int> clusterPositions, Vector3Int attackPos)
    {
        for (int i = 0; i < clusterPositions.Count; i++)
        {
            _grantConnections[clusterPositions[i]] = attackPos;
        }
    }

    /// <summary> 付与塊全体の接続を解除. </summary>
    public void ClearGrantConnection(List<Vector3Int> clusterPositions)
    {
        for (int i = 0; i < clusterPositions.Count; i++)
        {
            _grantConnections.Remove(clusterPositions[i]);
        }
    }

    /// <summary> 指定付与ブロックの接続先攻撃を取得. 未接続ならnull. </summary>
    public Vector3Int? GetGrantConnection(Vector3Int grantPos)
    {
        if (_grantConnections.TryGetValue(grantPos, out var attackPos))
        {
            return attackPos;
        }
        return null;
    }

    /// <summary> 全接続マップを取得 (BlockLinkResolver用). </summary>
    public Dictionary<Vector3Int, Vector3Int> GetAllGrantConnections()
    {
        return new Dictionary<Vector3Int, Vector3Int>(_grantConnections);
    }

    /// <summary> 指定座標群に関連する接続をクリア (ミノ除去時用). </summary>
    public void ClearGrantConnectionsAt(List<Vector3Int> positions)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            _grantConnections.Remove(positions[i]);
        }
    }

    #endregion
}
