using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary> ステージの3Dオブジェクト描画 + コライダー管理. </summary>
public class StageView
{
    private Transform _root;
    private PoolManager _pool;
    private StageData _data;

    // 生成した描画オブジェクト.
    private readonly List<GameObject> _tileObjects = new List<GameObject>();

    // Addressablesでロードしたハンドル (解放用).
    private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _loadedPrefabs
        = new Dictionary<string, AsyncOperationHandle<GameObject>>();

    // コライダー用オブジェクト.
    private GameObject _floorColliderObj;
    private readonly List<GameObject> _wallColliderObjects = new List<GameObject>();

    // タイプ別フォールバック色.
    private static readonly Dictionary<StageTileType, Color> TileColors = new Dictionary<StageTileType, Color>
    {
        { StageTileType.RoomFloor,        new Color(0.8f, 0.8f, 0.7f) },
        { StageTileType.RoomWall,         new Color(0.4f, 0.4f, 0.4f) },
        { StageTileType.RoomCorner,       new Color(0.35f, 0.35f, 0.35f) },
        { StageTileType.RoomDoorWall,     new Color(0.6f, 0.5f, 0.3f) },
        { StageTileType.CorridorStraight, new Color(0.7f, 0.7f, 0.6f) },
        { StageTileType.CorridorL,        new Color(0.7f, 0.7f, 0.6f) },
        { StageTileType.CorridorT,        new Color(0.7f, 0.7f, 0.6f) },
        { StageTileType.CorridorCross,    new Color(0.7f, 0.7f, 0.6f) },
        { StageTileType.CorridorDeadEnd,  new Color(0.5f, 0.5f, 0.5f) },
    };

    /// <summary> ステージを描画. Addressables → Pool → フォールバック. </summary>
    public async UniTask BuildAsync(StageData data, Transform root, PoolManager pool)
    {
        _data = data;
        _root = root;
        _pool = pool;

        // タイプごとにプレハブを事前ロード試行.
        await PreloadPrefabs(data);

        // 各マスにオブジェクトを配置.
        for (int x = 0; x < data.GridSize.x; x++)
        for (int z = 0; z < data.GridSize.y; z++)
        {
            var tile = data.GetTile(x, z);
            if (tile == null || tile.Type == StageTileType.None) continue;

            var worldPos = data.GridToWorld(x, z);
            var rotation = Quaternion.Euler(0f, tile.Rotation * 90f, 0f);

            var obj = CreateTileObject(tile, worldPos, rotation);
            if (obj != null)
            {
                obj.transform.SetParent(_root);
                _tileObjects.Add(obj);
            }
        }

        // コライダー生成 (描画とは別).
        CreateFloorCollider(data);
        CreateWallColliders(data);

        Debug.Log($"StageView: 描画完了 ({_tileObjects.Count} タイル)");
    }

    /// <summary> 使用されるタイプのプレハブを事前にAddressablesからロード試行. </summary>
    private async UniTask PreloadPrefabs(StageData data)
    {
        var addresses = new HashSet<string>();
        for (int x = 0; x < data.GridSize.x; x++)
        for (int z = 0; z < data.GridSize.y; z++)
        {
            var tile = data.GetTile(x, z);
            if (tile != null && !string.IsNullOrEmpty(tile.ObjectAddress))
            {
                addresses.Add(tile.ObjectAddress);
            }
        }

        foreach (var address in addresses)
        {
            if (_loadedPrefabs.ContainsKey(address)) continue;

            try
            {
                var handle = Addressables.LoadAssetAsync<GameObject>(address);
                await handle.ToUniTask();

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _loadedPrefabs[address] = handle;
                }
                else
                {
                    Debug.Log($"StageView: Addressables 未登録: {address} (フォールバック使用)");
                }
            }
            catch
            {
                Debug.Log($"StageView: Addressables 未登録: {address} (フォールバック使用)");
            }
        }
    }

    /// <summary> タイル1マス分のオブジェクトを生成. </summary>
    private GameObject CreateTileObject(StageTile tile, Vector3 position, Quaternion rotation)
    {
        GameObject obj = null;

        // 1. Addressables プレハブが利用可能か.
        if (!string.IsNullOrEmpty(tile.ObjectAddress)
            && _loadedPrefabs.TryGetValue(tile.ObjectAddress, out var handle))
        {
            obj = Object.Instantiate(handle.Result, position, rotation);
        }

        // 2. Pool (Resources) フォールバック.
        if (obj == null && _pool != null && !string.IsNullOrEmpty(tile.ObjectAddress))
        {
            obj = _pool.Get(tile.ObjectAddress);
            if (obj != null)
            {
                bool isWallType = (tile.Type == StageTileType.RoomWall
                                || tile.Type == StageTileType.RoomCorner);
                obj.transform.localScale = new Vector3(_data.TileSize, 1f, _data.TileSize);
                obj.transform.position = isWallType
                    ? position + new Vector3(0f, 0.5f, 0f)
                    : position;
                obj.transform.rotation = rotation;
            }
        }

        // 3. Cube/Quad フォールバック.
        if (obj == null)
        {
            obj = CreateFallbackTile(tile, position, rotation);
        }

        obj.name = $"Tile_{tile.Type}_{(int)(position.x / _data.TileSize)}_{(int)(position.z / _data.TileSize)}";

        // 描画用オブジェクトのコライダーを無効化 (コライダーは別管理).
        var collider = obj.GetComponent<Collider>();
        if (collider != null) Object.Destroy(collider);

        return obj;
    }

    /// <summary> フォールバック: Cube/Quad で仮描画. </summary>
    private GameObject CreateFallbackTile(StageTile tile, Vector3 position, Quaternion rotation)
    {
        bool isWallType = (tile.Type == StageTileType.RoomWall
                        || tile.Type == StageTileType.RoomCorner);

        GameObject obj;
        if (isWallType)
        {
            // 壁: 高さのあるCube.
            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.localScale = new Vector3(_data.TileSize, 1f, _data.TileSize);
            obj.transform.position = position + new Vector3(0f, 0.5f, 0f);
        }
        else
        {
            // 床/通路: 平面Quad.
            obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.transform.localScale = new Vector3(_data.TileSize, 1f, _data.TileSize);
            obj.transform.position = position;
            obj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        // タイプ別の色を設定.
        if (TileColors.TryGetValue(tile.Type, out var color))
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = color;
        }

        return obj;
    }

    /// <summary> 床コライダー: 歩行可能マス全域を覆う1つの大きなBoxCollider. </summary>
    private void CreateFloorCollider(StageData data)
    {
        _floorColliderObj = new GameObject("FloorCollider");
        _floorColliderObj.transform.SetParent(_root);

        var center = new Vector3(
            (data.GridSize.x - 1) * data.TileSize * 0.5f,
            -0.05f,
            (data.GridSize.y - 1) * data.TileSize * 0.5f
        );
        var size = new Vector3(
            data.GridSize.x * data.TileSize,
            0.1f,
            data.GridSize.y * data.TileSize
        );

        _floorColliderObj.transform.position = center;
        var box = _floorColliderObj.AddComponent<BoxCollider>();
        box.size = size;
        box.center = Vector3.zero;
    }

    /// <summary> 壁コライダー: 壁マスごとに個別BoxCollider. </summary>
    private void CreateWallColliders(StageData data)
    {
        for (int x = 0; x < data.GridSize.x; x++)
        for (int z = 0; z < data.GridSize.y; z++)
        {
            if (!data.IsWall(x, z)) continue;
            var tile = data.GetTile(x, z);
            if (tile == null || tile.Type == StageTileType.None) continue;

            var obj = new GameObject($"WallCol_{x}_{z}");
            obj.transform.SetParent(_root);
            obj.transform.position = data.GridToWorld(x, z) + new Vector3(0f, data.TileSize * 0.5f, 0f);

            var box = obj.AddComponent<BoxCollider>();
            box.size = new Vector3(data.TileSize, data.TileSize, data.TileSize);

            _wallColliderObjects.Add(obj);
        }
    }

    /// <summary> 描画とコライダーを破棄. </summary>
    public void Dispose()
    {
        for (int i = 0; i < _tileObjects.Count; i++)
        {
            if (_tileObjects[i] != null) Object.Destroy(_tileObjects[i]);
        }
        _tileObjects.Clear();

        for (int i = 0; i < _wallColliderObjects.Count; i++)
        {
            if (_wallColliderObjects[i] != null) Object.Destroy(_wallColliderObjects[i]);
        }
        _wallColliderObjects.Clear();

        if (_floorColliderObj != null)
        {
            Object.Destroy(_floorColliderObj);
            _floorColliderObj = null;
        }

        // Addressablesプレハブを解放.
        foreach (var kvp in _loadedPrefabs)
        {
            Addressables.Release(kvp.Value);
        }
        _loadedPrefabs.Clear();
    }
}
