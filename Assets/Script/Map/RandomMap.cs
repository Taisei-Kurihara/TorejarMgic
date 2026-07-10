using System.Collections.Generic;
using UnityEngine;

/// <summary> ローグ的なランダムマップ. x*yサイズの敷き詰めでデータ管理. </summary>
/// <remarks> MonoBehaviourなし. 3Dオブジェはアドレサブルスで読み込み (名称のみ先行定義). </remarks>
public class RandomMap
{
    /// <summary> マップタイルの種別. </summary>
    public enum TileType
    {
        Empty,
        Floor,
        Wall,
        Start,
        Goal,
        Event
    }

    /// <summary> タイル1つ分のデータ. </summary>
    public class TileData
    {
        public TileType Type;
        /// <summary> タイルに配置する3Dオブジェのアドレス (Addressables用. 読み込み内容未定のため名称のみ). </summary>
        public string ObjectAddress;

        public TileData()
        {
            Type = TileType.Empty;
            ObjectAddress = "";
        }
    }

    private readonly TileData[,] _tiles;
    private readonly Vector2Int _size;

    /// <summary> マップサイズ. </summary>
    public Vector2Int Size => _size;

    // アドレサブルス用オブジェクト名称 (読み込み内容はまだなので名称だけ先に定義).
    public static class AddressableNames
    {
        public const string FloorTile = "Tile_Floor";
        public const string WallTile = "Tile_Wall";
        public const string StartTile = "Tile_Start";
        public const string GoalTile = "Tile_Goal";
        public const string EventTile = "Tile_Event";
    }

    public RandomMap(Vector2Int size)
    {
        _size = size;
        _tiles = new TileData[size.x, size.y];

        for (int x = 0; x < size.x; x++)
        for (int y = 0; y < size.y; y++)
        {
            _tiles[x, y] = new TileData();
        }
    }

    /// <summary> ランダムマップ生成. </summary>
    public void Initialize()
    {
        GenerateSimpleRooms();
        Debug.Log($"RandomMap: 生成完了 ({_size.x}x{_size.y})");
    }

    /// <summary> 座標が範囲内か. </summary>
    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < _size.x && y >= 0 && y < _size.y;
    }

    /// <summary> タイルデータ取得. </summary>
    public TileData GetTile(int x, int y)
    {
        if (!IsInBounds(x, y)) return null;
        return _tiles[x, y];
    }

    /// <summary> タイルデータ設定. </summary>
    public void SetTile(int x, int y, TileData data)
    {
        if (!IsInBounds(x, y)) return;
        _tiles[x, y] = data;
    }

    /// <summary> タイル種別設定. </summary>
    public void SetTileType(int x, int y, TileType type)
    {
        if (!IsInBounds(x, y)) return;
        _tiles[x, y].Type = type;
        _tiles[x, y].ObjectAddress = GetAddressForType(type);
    }

    /// <summary> スタート地点の座標を取得. </summary>
    public Vector2Int? FindStart()
    {
        for (int x = 0; x < _size.x; x++)
        for (int y = 0; y < _size.y; y++)
        {
            if (_tiles[x, y].Type == TileType.Start) return new Vector2Int(x, y);
        }
        return null;
    }

    /// <summary> 全Floor/Start/Goal/Eventタイル座標を取得. </summary>
    public List<Vector2Int> GetWalkableTiles()
    {
        var result = new List<Vector2Int>();
        for (int x = 0; x < _size.x; x++)
        for (int y = 0; y < _size.y; y++)
        {
            var type = _tiles[x, y].Type;
            if (type == TileType.Floor || type == TileType.Start
                || type == TileType.Goal || type == TileType.Event)
            {
                result.Add(new Vector2Int(x, y));
            }
        }
        return result;
    }

    /// <summary> 簡易部屋生成. 外周を壁、内部を床、Start/Goalを配置. </summary>
    private void GenerateSimpleRooms()
    {
        // 外周は壁.
        for (int x = 0; x < _size.x; x++)
        for (int y = 0; y < _size.y; y++)
        {
            bool isEdge = x == 0 || x == _size.x - 1 || y == 0 || y == _size.y - 1;
            SetTileType(x, y, isEdge ? TileType.Wall : TileType.Floor);
        }

        // ランダムに内部壁を配置.
        int wallCount = (_size.x * _size.y) / 8;
        for (int i = 0; i < wallCount; i++)
        {
            int wx = Random.Range(2, _size.x - 2);
            int wy = Random.Range(2, _size.y - 2);
            SetTileType(wx, wy, TileType.Wall);
        }

        // Start/Goal配置.
        SetTileType(1, 1, TileType.Start);
        SetTileType(_size.x - 2, _size.y - 2, TileType.Goal);
    }

    private string GetAddressForType(TileType type)
    {
        return type switch
        {
            TileType.Floor => AddressableNames.FloorTile,
            TileType.Wall => AddressableNames.WallTile,
            TileType.Start => AddressableNames.StartTile,
            TileType.Goal => AddressableNames.GoalTile,
            TileType.Event => AddressableNames.EventTile,
            _ => ""
        };
    }
}
