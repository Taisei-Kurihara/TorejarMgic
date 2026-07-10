using System;
using System.Collections.Generic;

/// <summary>
/// LoadRequestItemを生成するFactory
/// Strategy付きのリクエストアイテムを生成することで、処理方式をカプセル化
/// </summary>
public static class LoadRequestFactory
{
    /// <summary> プール用リクエストを生成 </summary>
    public static LoadRequestItem CreatePoolRequest(string key, PoolManager poolManager, Type type = null)
    {
        return new LoadRequestItem
        {
            Key = key,
            Type = type,
            Strategy = new LoadStrategy_Pool(poolManager)
        };
    }

    /// <summary> プール用リクエストリストを生成 </summary>
    public static LoadType CreatePoolLoadType(PoolManager poolManager, params (string key, Type type)[] requests)
    {
        var items = new List<LoadRequestItem>();
        foreach (var (key, type) in requests)
        {
            items.Add(CreatePoolRequest(key, poolManager, type));
        }
        return new LoadType(items);
    }

    /// <summary> 通常ロード用リクエストを生成 </summary>
    public static LoadRequestItem CreateNormalRequest(string key, Type type = null)
    {
        return new LoadRequestItem
        {
            Key = key,
            Type = type,
            Strategy = new LoadStrategy_Normal()
        };
    }

    /// <summary> 通常ロード用リクエストリストを生成 </summary>
    public static LoadType CreateNormalLoadType(params (string key, Type type)[] requests)
    {
        var items = new List<LoadRequestItem>();
        foreach (var (key, type) in requests)
        {
            items.Add(CreateNormalRequest(key, type));
        }
        return new LoadType(items);
    }
}
