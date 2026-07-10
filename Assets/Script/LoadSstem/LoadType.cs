using System;
using System.Collections.Generic;
using System.Linq;

#region LoadRequestItem

public class LoadRequestItem
{
    public string Key;
    public Type Type;

    // ロード結果
    public object Result;

    // Addressables等用
    public object Handle;

    // Strategy: 処理内容を決定
    public ILoadStrategy Strategy;
}

#endregion

#region LoadType

public class LoadType
{
    public List<LoadRequestItem> Requests { get; }

    public LoadType(List<LoadRequestItem> requests = null)
    {
        Requests = requests ?? new List<LoadRequestItem>();
    }

    #region Getter

    /// <summary> 結果を取得 </summary>
    public T Get<T>(string key)
    {
        var item = Requests.FirstOrDefault(x => x.Key == key);

        if (item == null)
            throw new Exception($"Key not found: {key}");

        return (T)item.Result;
    }

    /// <summary> 安全に取得 </summary>
    public bool TryGet<T>(string key, out T value)
    {
        var item = Requests.FirstOrDefault(x => x.Key == key);

        if (item != null && item.Result is T t)
        {
            value = t;
            return true;
        }

        value = default;
        return false;
    }

    #endregion

    #region Setter（内部用）

    public void SetResult(string key, object result, object handle = null)
    {
        var item = Requests.FirstOrDefault(x => x.Key == key);

        if (item == null)
            throw new Exception($"Key not found: {key}");

        item.Result = result;
        item.Handle = handle;
    }

    #endregion
}

#endregion
