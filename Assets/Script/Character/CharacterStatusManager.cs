using System.Collections.Generic;
using UnityEngine;

/// <summary> CSVから読み込んだキャラクターステータスを保持するシングルトン. </summary>
/// <remarks> MonoBehaviourなし. 純粋C#シングルトン. </remarks>
public class CharacterStatusManager
{
    private static CharacterStatusManager _instance;
    public static CharacterStatusManager Instance => _instance ??= new CharacterStatusManager();

    /// <summary> CSV1行分のステータス定義. </summary>
    [System.Serializable]
    public class StatusData
    {
        public string name;
        public int maxHp;
        public int baseAttack;
        public int baseDefense;
    }

    private readonly Dictionary<string, StatusData> _statusMap = new Dictionary<string, StatusData>();

    private CharacterStatusManager() { }

    /// <summary> CSVファイルからステータスデータを読み込み. </summary>
    public void LoadFromCsv(string csvFileName)
    {
        var rows = CsvLoader.LoadAsDictionary(csvFileName);
        if (rows == null)
        {
            Debug.LogWarning($"CharacterStatus CSV読み込み失敗: {csvFileName}");
            return;
        }

        _statusMap.Clear();
        for (int i = 0; i < rows.Count; i++)
        {
            var data = new StatusData();
            var row = rows[i];

            data.name = row.ContainsKey("name") ? row["name"] : "";
            data.maxHp = row.ContainsKey("maxHp") ? int.Parse(row["maxHp"]) : 0;
            data.baseAttack = row.ContainsKey("baseAttack") ? int.Parse(row["baseAttack"]) : 0;
            data.baseDefense = row.ContainsKey("baseDefense") ? int.Parse(row["baseDefense"]) : 0;

            if (!string.IsNullOrEmpty(data.name))
            {
                _statusMap[data.name] = data;
            }
        }

        Debug.Log($"CharacterStatus読み込み完了: {_statusMap.Count}件");
    }

    /// <summary> 名前でステータスデータを取得. </summary>
    public StatusData GetStatusData(string name)
    {
        if (_statusMap.TryGetValue(name, out var data))
        {
            return data;
        }

        Debug.LogWarning($"ステータスデータが見つかりません: {name}");
        return null;
    }

    /// <summary> 全ステータスデータを取得. </summary>
    public IReadOnlyDictionary<string, StatusData> AllStatusData => _statusMap;
}
