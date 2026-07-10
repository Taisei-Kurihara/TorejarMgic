using System.Collections.Generic;
using UnityEngine;

/// <summary> ステージ全体の二次元配列データ. </summary>
public class StageData
{
    /// <summary> マスの二次元配列 [x, z]. </summary>
    public StageTile[,] Tiles;

    /// <summary> グリッドのマス数. </summary>
    public Vector2Int GridSize;

    /// <summary> 1マスのUnity上のサイズ (meter). 正方形 (X=Z). </summary>
    public float TileSize;

    /// <summary> スタート地点 (グリッド座標). </summary>
    public Vector2Int StartPos;

    /// <summary> ゴール地点 (グリッド座標). </summary>
    public Vector2Int GoalPos;

    public StageData(Vector2Int gridSize, float tileSize)
    {
        GridSize = gridSize;
        TileSize = tileSize;
        Tiles = new StageTile[gridSize.x, gridSize.y];

        for (int x = 0; x < gridSize.x; x++)
        for (int z = 0; z < gridSize.y; z++)
        {
            Tiles[x, z] = new StageTile();
        }
    }

    /// <summary> 座標が範囲内か. </summary>
    public bool IsInBounds(int x, int z)
    {
        return x >= 0 && x < GridSize.x && z >= 0 && z < GridSize.y;
    }

    /// <summary> タイル取得. </summary>
    public StageTile GetTile(int x, int z)
    {
        if (!IsInBounds(x, z)) return null;
        return Tiles[x, z];
    }

    /// <summary> タイル設定. </summary>
    public void SetTile(int x, int z, StageTile tile)
    {
        if (!IsInBounds(x, z)) return;
        Tiles[x, z] = tile;
    }

    /// <summary> グリッド座標 => ワールド座標 (マス中心). </summary>
    public Vector3 GridToWorld(int x, int z)
    {
        return new Vector3(x * TileSize, 0f, z * TileSize);
    }

    /// <summary> グリッド座標 => ワールド座標. </summary>
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return GridToWorld(gridPos.x, gridPos.y);
    }

    /// <summary> 歩行可能マスかどうか (None以外). </summary>
    public bool IsWalkable(int x, int z)
    {
        if (!IsInBounds(x, z)) return false;
        var type = Tiles[x, z].Type;
        return type == StageTileType.RoomFloor
            || type == StageTileType.RoomDoorWall
            || type == StageTileType.CorridorStraight
            || type == StageTileType.CorridorL
            || type == StageTileType.CorridorT
            || type == StageTileType.CorridorCross;
    }

    /// <summary> 壁マスかどうか. </summary>
    public bool IsWall(int x, int z)
    {
        if (!IsInBounds(x, z)) return true;
        var type = Tiles[x, z].Type;
        return type == StageTileType.RoomWall
            || type == StageTileType.RoomCorner
            || type == StageTileType.None;
    }

    /// <summary> 全歩行可能マスのグリッド座標リストを取得. </summary>
    public List<Vector2Int> GetWalkableTiles()
    {
        var result = new List<Vector2Int>();
        for (int x = 0; x < GridSize.x; x++)
        for (int z = 0; z < GridSize.y; z++)
        {
            if (IsWalkable(x, z))
            {
                result.Add(new Vector2Int(x, z));
            }
        }
        return result;
    }
}
