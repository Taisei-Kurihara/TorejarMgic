using System.Collections.Generic;
using UnityEngine;

/// <summary> ミノ形状マスターCSVの読み込み/書き出し (ビジュアルグリッド形式). </summary>
/// <remarks>
/// CSV先頭行のA1セルに説明行数 (この行を含む) を記載.
/// 説明行の後にデータヘッダー + 各ミノの5×5グリッドが続く.
///
/// グリッド値:
///   0 = ブロックなし
///   1 = ブロック (効果なし / None)
///   2 = 攻撃 (Attack)
///   3 = 付与 (Grant)
///   4 = 補助 (Support)
///
/// 効果ID・効果値は効果ありブロック (値2~4) に適用. ミノごとに1種類1個のみ.
/// </remarks>
public static class MinoShapeMasterCsv
{
    private const int GridSize = 5;

    /// <summary> 説明ヘッダー行テンプレート. {0}には行数が入る. </summary>
    private static readonly string[] DescriptionTemplate =
    {
        "{0},説明行数(この行を含む)",
        "[ミノ形状マスター]",
        "配置場所: StreamingAssets/MinoShapeMaster.csv (ビルド後は <ビルド名>_Data/StreamingAssets/)",
        "グリッド値: 0=なし  1=ブロック(効果なし)  2=攻撃  3=付与  4=補助",
        "効果ありブロック(2~4)はミノごとに1種類1個のみ",
        "効果IDと効果値は効果ありブロックに適用される"
    };

    /// <summary> CSVから全ミノ形状データを読み込み. </summary>
    public static List<MinoShapeData> LoadFromCsv(string csvFileName)
    {
        var rows = CsvLoader.LoadFromStreamingAssets(csvFileName);
        if (rows == null || rows.Count == 0) return new List<MinoShapeData>();

        // ヘッダー行を "\" セルで検出 (空行スキップに依存しない).
        int headerRow = -1;
        int gridColOffset = 4; // フォールバック.
        for (int r = 0; r < rows.Count; r++)
        {
            for (int c = 0; c < rows[r].Length; c++)
            {
                if (rows[r][c].Trim() == "\\")
                {
                    headerRow = r;
                    gridColOffset = c + 1;
                    break;
                }
            }
            if (headerRow >= 0) break;
        }

        if (headerRow < 0) return new List<MinoShapeData>();

        int dataStart = headerRow + 1;
        if (dataStart >= rows.Count) return new List<MinoShapeData>();

        var shapes = new List<MinoShapeData>();
        int i = dataStart;

        while (i < rows.Count)
        {
            var firstRow = rows[i];

            // 名前列 (col 0) が空 → 前のミノの継続行がずれた場合 → スキップ.
            string shapeName = (firstRow.Length > 0) ? firstRow[0].Trim() : "";
            if (string.IsNullOrEmpty(shapeName))
            {
                i++;
                continue;
            }

            // 効果ID・効果値 (col 1, 2).
            string effectId = (firstRow.Length > 1) ? firstRow[1].Trim() : "";
            float effectValue = 0f;
            if (firstRow.Length > 2) float.TryParse(firstRow[2].Trim(), out effectValue);

            // 5行分のグリッドを読み取り.
            var grid = new int[GridSize, GridSize]; // [z, x]
            for (int z = 0; z < GridSize; z++)
            {
                int rowIdx = i + z;
                if (rowIdx >= rows.Count) break;

                var gridRow = rows[rowIdx];
                for (int x = 0; x < GridSize; x++)
                {
                    int cellIdx = gridColOffset + x;
                    if (cellIdx < gridRow.Length)
                    {
                        int.TryParse(gridRow[cellIdx].Trim(), out grid[z, x]);
                    }
                }
            }

            // グリッドから MinoShapeData を構築.
            var shape = new MinoShapeData { ShapeId = shapeName };
            for (int z = 0; z < GridSize; z++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    int val = grid[z, x];
                    if (val <= 0) continue;

                    shape.BlockOffsets.Add(new Vector3Int(x, 0, z));

                    var effectType = CsvValueToEffectType(val);
                    if (val >= 2)
                    {
                        shape.BlockEffects.Add(new BlockData(effectType, effectId, effectValue));
                    }
                    else
                    {
                        shape.BlockEffects.Add(new BlockData(effectType, "", 0f));
                    }
                }
            }

            if (shape.BlockCount > 0)
            {
                shapes.Add(shape);
            }

            i += GridSize;
        }

        return shapes;
    }

    /// <summary> ミノ形状データをCSVに書き出し. </summary>
    public static void SaveToCsv(string csvFileName, List<MinoShapeData> shapes)
    {
        var allRows = new List<string[]>();

        // 説明行 (空行なし).
        int descCount = DescriptionTemplate.Length;
        for (int d = 0; d < descCount; d++)
        {
            string line = (d == 0)
                ? string.Format(DescriptionTemplate[d], descCount)
                : DescriptionTemplate[d];
            allRows.Add(new string[] { line });
        }

        // ヘッダー行.
        var header = new List<string> { "名", "効果ID", "効果値", "\\" };
        for (int x = 0; x < GridSize; x++) header.Add(x.ToString());
        allRows.Add(header.ToArray());

        // 各ミノのグリッド.
        for (int s = 0; s < shapes.Count; s++)
        {
            var shape = shapes[s];
            var grid = BuildGrid(shape);

            // 効果ありブロックから effectId / effectValue を取得.
            string effectId = "";
            float effectValue = 0f;
            for (int b = 0; b < shape.BlockEffects.Count; b++)
            {
                if (shape.BlockEffects[b].EffectType != BlockEffectType.None)
                {
                    effectId = shape.BlockEffects[b].EffectId;
                    effectValue = shape.BlockEffects[b].EffectValue;
                    break;
                }
            }

            for (int z = 0; z < GridSize; z++)
            {
                var row = new List<string>();

                if (z == 0)
                {
                    row.Add(shape.ShapeId);
                    row.Add(effectId);
                    row.Add(effectValue.ToString("G"));
                }
                else
                {
                    row.Add("");
                    row.Add("");
                    row.Add("");
                }

                row.Add(z.ToString());

                for (int x = 0; x < GridSize; x++)
                {
                    row.Add(grid[z, x].ToString());
                }

                allRows.Add(row.ToArray());
            }
        }

        CsvLoader.SaveToStreamingAssets(csvFileName, allRows);
    }

    /// <summary> デフォルトのCSVテンプレートを書き出し. </summary>
    public static void ExportTemplate(string csvFileName)
    {
        var templateShapes = new List<MinoShapeData>
        {
            CreateTemplateShape("shape_L", new Vector3Int[]
            {
                new(0, 0, 0), new(1, 0, 0), new(2, 0, 0), new(2, 0, 1)
            }),
            CreateTemplateShape("shape_T", new Vector3Int[]
            {
                new(0, 0, 0), new(1, 0, 0), new(2, 0, 0), new(1, 0, 1)
            }),
            CreateTemplateShape("shape_I", new Vector3Int[]
            {
                new(0, 0, 0), new(1, 0, 0), new(2, 0, 0), new(3, 0, 0)
            })
        };

        SaveToCsv(csvFileName, templateShapes);
        Debug.Log($"ミノ形状テンプレートCSV書き出し完了: {csvFileName}");
    }

    #region 内部処理

    /// <summary> MinoShapeData から5×5グリッド配列を構築. </summary>
    private static int[,] BuildGrid(MinoShapeData shape)
    {
        var grid = new int[GridSize, GridSize]; // [z, x]

        for (int b = 0; b < shape.BlockCount; b++)
        {
            var offset = shape.BlockOffsets[b];
            int x = offset.x;
            int z = offset.z;
            if (x < 0 || x >= GridSize || z < 0 || z >= GridSize) continue;

            var effect = (b < shape.BlockEffects.Count) ? shape.BlockEffects[b] : new BlockData();
            grid[z, x] = EffectTypeToCsvValue(effect.EffectType);
        }

        return grid;
    }

    /// <summary> CSV値 → BlockEffectType. </summary>
    private static BlockEffectType CsvValueToEffectType(int csvValue)
    {
        return csvValue switch
        {
            1 => BlockEffectType.None,
            2 => BlockEffectType.Attack,
            3 => BlockEffectType.Grant,
            4 => BlockEffectType.Support,
            _ => BlockEffectType.None
        };
    }

    /// <summary> BlockEffectType → CSV値. </summary>
    private static int EffectTypeToCsvValue(BlockEffectType type)
    {
        return type switch
        {
            BlockEffectType.None => 1,
            BlockEffectType.Attack => 2,
            BlockEffectType.Grant => 3,
            BlockEffectType.Support => 4,
            _ => 1
        };
    }

    private static MinoShapeData CreateTemplateShape(string shapeId, Vector3Int[] offsets)
    {
        var shape = new MinoShapeData { ShapeId = shapeId };
        for (int i = 0; i < offsets.Length; i++)
        {
            shape.BlockOffsets.Add(offsets[i]);
            if (i == 0)
            {
                shape.BlockEffects.Add(new BlockData(BlockEffectType.Attack, "default", 10f));
            }
            else
            {
                shape.BlockEffects.Add(new BlockData(BlockEffectType.None, "", 0f));
            }
        }
        return shape;
    }

    #endregion
}
