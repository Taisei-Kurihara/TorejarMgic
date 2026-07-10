using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// GameObjectプール管理クラス
/// キャッシュしたGameObjectを再利用して、生成/破棄のオーバーヘッドを削減
/// </summary>
public class PoolManager
{
    private Dictionary<string, Queue<GameObject>> _pool = new();

    /// <summary> オブジェクトを取得（キャッシュがなければ新規生成） </summary>
    public GameObject Get(string key)
    {
        if (_pool.TryGetValue(key, out var queue) && queue.Count > 0)
        {
            var obj = queue.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        return CreateNew(key);
    }

    /// <summary> オブジェクトをプールに返却 </summary>
    public void Release(string key, GameObject obj)
    {
        if (obj == null)
            return;

        if (!_pool.ContainsKey(key))
        {
            _pool[key] = new Queue<GameObject>();
        }

        obj.SetActive(false);
        _pool[key].Enqueue(obj);
    }

    /// <summary> 新規オブジェクトを生成 </summary>
    private GameObject CreateNew(string key)
    {
        // Resources.Loadから取得.
        var prefab = Resources.Load<GameObject>(key);
        if (prefab != null)
        {
            return Object.Instantiate(prefab);
        }

        // フォールバック: プリミティブキューブを生成.
        Debug.Log($"Prefab not found: {key} / フォールバックでCube生成.");
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = key;
        return obj;
    }

    /// <summary> プールを完全にクリア </summary>
    public void Clear()
    {
        foreach (var queue in _pool.Values)
        {
            foreach (var obj in queue)
            {
                Object.Destroy(obj);
            }
            queue.Clear();
        }
        _pool.Clear();
    }
}
