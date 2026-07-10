using UnityEngine;

/// <summary> JSON読み込みユーティリティ. </summary>
public static class JsonLoader
{
    /// <summary> StreamingAssetsからJSONを読み込みデシリアライズ. </summary>
    public static T LoadFromStreamingAssets<T>(string fileName)
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);

        if (!System.IO.File.Exists(path))
        {
            Debug.LogError($"JSONファイルが見つかりません: {path}");
            return default;
        }

        string json = System.IO.File.ReadAllText(path);
        return JsonUtility.FromJson<T>(json);
    }

    /// <summary> JSON文字列からデシリアライズ. </summary>
    public static T FromJson<T>(string json)
    {
        return JsonUtility.FromJson<T>(json);
    }

    /// <summary> オブジェクトをJSON文字列にシリアライズ. </summary>
    public static string ToJson<T>(T obj, bool prettyPrint = true)
    {
        return JsonUtility.ToJson(obj, prettyPrint);
    }
}
