using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

/// <summary> CSV/ScriptableObject併用のマスターデータ基底クラス. </summary>
/// <remarks>
/// プログラマ: SOのInspectorから直接編集.
/// 非プログラマ: CSVで値を編集.
/// NaughtyAttributesのボタンで個別/全体の読み書きを提供.
/// </remarks>
public abstract class MasterDataScriptableBase<T> : ScriptableObject where T : class, new()
{
    [Header("CSVファイル名 (StreamingAssets以下)")]
    [SerializeField] protected string _csvFileName = "";

    [Header("データ一覧")]
    [SerializeField] protected List<T> _dataList = new List<T>();

    /// <summary> 読み込み済みデータ一覧. </summary>
    public IReadOnlyList<T> DataList => _dataList;

    /// <summary> CSVファイル名. </summary>
    public string CsvFileName => _csvFileName;

    /// <summary> CSVからデータを全体読み込み. </summary>
    [Button("CSV全体読み込み")]
    public void LoadAllFromCsv()
    {
        var rows = CsvLoader.LoadAsDictionary(_csvFileName);
        if (rows == null)
        {
            Debug.LogWarning($"CSV読み込み失敗: {_csvFileName}");
            return;
        }

        _dataList.Clear();
        for (int i = 0; i < rows.Count; i++)
        {
            var item = new T();
            ApplyCsvRow(item, rows[i]);
            _dataList.Add(item);
        }

        Debug.Log($"CSV全体読み込み完了: {_csvFileName} ({_dataList.Count}件)");
    }

    /// <summary> 現在のデータをCSVに全体書き出し. </summary>
    [Button("CSV全体書き出し")]
    public void SaveAllToCsv()
    {
        var header = GetCsvHeader();
        var dataRows = new List<string[]>();

        for (int i = 0; i < _dataList.Count; i++)
        {
            dataRows.Add(ToCsvRow(_dataList[i]));
        }

        CsvLoader.SaveWithHeader(_csvFileName, header, dataRows);
    }

    /// <summary> CSV1行分のデータをTに適用. 継承先で実装. </summary>
    protected abstract void ApplyCsvRow(T target, Dictionary<string, string> row);

    /// <summary> CSVヘッダー配列を返す. 継承先で実装. </summary>
    protected abstract string[] GetCsvHeader();

    /// <summary> T1件分をCSV行データに変換. 継承先で実装. </summary>
    protected abstract string[] ToCsvRow(T data);
}
