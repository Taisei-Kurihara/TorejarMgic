using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary> CSV読み込み/書き出しユーティリティ. </summary>
public static class CsvLoader
{
    /// <summary> StreamingAssetsからCSVを読み込み、行ごとの文字列配列リストとして返す. </summary>
    public static List<string[]> LoadFromStreamingAssets(string fileName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);

        if (!File.Exists(path))
        {
            Debug.LogError($"CSVファイルが見つかりません: {path}");
            return null;
        }

        return Parse(File.ReadAllText(path, Encoding.UTF8));
    }

    /// <summary> 任意のパスからCSVを読み込み. </summary>
    public static List<string[]> LoadFromPath(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"CSVファイルが見つかりません: {fullPath}");
            return null;
        }

        return Parse(File.ReadAllText(fullPath, Encoding.UTF8));
    }

    /// <summary> CSV文字列をパース. ヘッダー行含む全行を返す. </summary>
    public static List<string[]> Parse(string csvText)
    {
        var result = new List<string[]>();
        var lines = csvText.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim('\r', ' ');
            if (string.IsNullOrEmpty(line)) continue;

            var cells = line.Split(',');
            for (int j = 0; j < cells.Length; j++)
            {
                cells[j] = cells[j].Trim();
            }
            result.Add(cells);
        }

        return result;
    }

    /// <summary> ヘッダー付きCSVを辞書リストとして読み込み. 1行目をキーとする. </summary>
    public static List<Dictionary<string, string>> LoadAsDictionary(string fileName)
    {
        var rows = LoadFromStreamingAssets(fileName);
        if (rows == null || rows.Count < 2) return null;

        var result = new List<Dictionary<string, string>>();
        var header = rows[0];

        for (int i = 1; i < rows.Count; i++)
        {
            var dict = new Dictionary<string, string>();
            for (int j = 0; j < header.Length && j < rows[i].Length; j++)
            {
                dict[header[j]] = rows[i][j];
            }
            result.Add(dict);
        }

        return result;
    }

    /// <summary> StreamingAssetsにCSVを書き出し. </summary>
    public static void SaveToStreamingAssets(string fileName, List<string[]> data)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        SaveToPath(path, data);
    }

    /// <summary> 任意のパスにCSVを書き出し. </summary>
    public static void SaveToPath(string fullPath, List<string[]> data)
    {
        var directory = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var sb = new StringBuilder();
        for (int i = 0; i < data.Count; i++)
        {
            sb.AppendLine(string.Join(",", data[i]));
        }

        File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"CSV書き出し完了: {fullPath}");
    }

    /// <summary> ヘッダーとデータ行を指定してCSV書き出し. </summary>
    public static void SaveWithHeader(string fileName, string[] header, List<string[]> dataRows)
    {
        var allData = new List<string[]> { header };
        allData.AddRange(dataRows);
        SaveToStreamingAssets(fileName, allData);
    }
}
