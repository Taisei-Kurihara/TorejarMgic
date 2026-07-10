using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary> パズルマップの3Dオブジェクト描画実装. </summary>
public class PuzzleMapView3D : IPuzzleMapView
{
    private Vector3Int _mapSize;
    private Transform _rootTransform;
    private Transform _worldContentRoot; // 不透明盤面+配置ブロック+コネクタの共通親 (層移動で下降).
    private Transform _previewRoot;

    // グリッド座標 => 生成済みブロックオブジェクト.
    private readonly Dictionary<Vector3Int, GameObject> _blockObjects = new Dictionary<Vector3Int, GameObject>();

    // プレビュー用オブジェクト.
    private readonly List<GameObject> _previewObjects = new List<GameObject>();

    // ミノ一覧表示用.
    private Transform _minoListRoot;
    private readonly List<GameObject> _minoListObjects = new List<GameObject>();

    // 盤面グリッド (最下層, 配置場所の可視化).
    private Transform _boardRoot;
    private readonly List<GameObject> _boardObjects = new List<GameObject>();

    // フォーカス層盤面 (半透明, 現在配置可能な層).
    private Transform _focusLayerBoardRoot;
    private readonly List<GameObject> _focusLayerBoardObjects = new List<GameObject>();
    private float _focusBoardTargetY;
    private float _focusBoardCurrentY;
    private const float FocusBoardLerpSpeed = 8f;

    // 操作方法ガイド.
    private GameObject _controlGuideObj;

    // パズル用ディレクショナルライト (影生成用).
    private GameObject _puzzleLightObj;

    // レイヤーごとの透明度管理.
    private readonly Dictionary<int, float> _layerAlphas = new Dictionary<int, float>();

    // グリッドテクスチャキャッシュ (セル数 => Texture2D).
    private readonly Dictionary<Vector2Int, Texture2D> _gridTextureCache = new Dictionary<Vector2Int, Texture2D>();

    // 配置済みブロックのデータ記録 (コネクタ連結判定用).
    private readonly Dictionary<Vector3Int, BlockData> _blockDataMap = new Dictionary<Vector3Int, BlockData>();

    // 連結コネクタオブジェクト (正規化キー => GameObject).
    private readonly Dictionary<(Vector3Int, Vector3Int), GameObject> _connectorObjects
        = new Dictionary<(Vector3Int, Vector3Int), GameObject>();

    // パルスアニメーション (未接続付与塊).
    private readonly HashSet<Vector3Int> _pulsingBlocks = new HashSet<Vector3Int>();
    private const float PulseSpeed = 2.5f;
    private const float PulseScaleMin = 0.7f;
    private const float PulseScaleMax = 1.0f;
    private const float NormalBlockScale = 0.8f;

    // ホバーハイライト (複数ブロック対応).
    private readonly HashSet<Vector3Int> _highlightedPositions = new HashSet<Vector3Int>();

    // ミノ一覧スクロールオフセット.
    private float _minoListScrollOffset;

    // 付与→攻撃 接続マップ (コネクタ表示判定用).
    private Dictionary<Vector3Int, Vector3Int> _grantConnectionMap = new Dictionary<Vector3Int, Vector3Int>();

    // 攻撃選択メニュー.
    private Transform _menuRoot;
    private readonly List<GameObject> _menuObjects = new List<GameObject>();
    private List<Vector3Int> _menuAttackPositions;
    private int _menuSelectedIndex = -1;

    // 6方向隣接 (BlockLinkResolver と同じ).
    private static readonly Vector3Int[] ConnectDirs =
    {
        Vector3Int.right, Vector3Int.left,
        Vector3Int.up, Vector3Int.down,
        new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
    };

    // 効果種別ごとの色.
    private static readonly Dictionary<BlockEffectType, Color> EffectColors = new Dictionary<BlockEffectType, Color>
    {
        { BlockEffectType.None,    Color.gray },
        { BlockEffectType.Attack,  new Color(0.9f, 0.2f, 0.2f) },
        { BlockEffectType.Grant,   new Color(0.2f, 0.6f, 0.9f) },
        { BlockEffectType.Support, new Color(0.2f, 0.9f, 0.3f) }
    };

    public PuzzleMapView3D(Transform rootTransform)
    {
        _rootTransform = rootTransform;
    }

    public void Initialize(Vector3Int mapSize)
    {
        _mapSize = mapSize;

        // ワールドコンテンツルート (不透明盤面+ブロック+コネクタ. 層移動で下降).
        var contentObj = new GameObject("WorldContentRoot");
        contentObj.transform.SetParent(_rootTransform);
        _worldContentRoot = contentObj.transform;

        // プレビュー用ルート (コンテンツルートの子, 一緒に移動).
        var previewObj = new GameObject("PreviewRoot");
        previewObj.transform.SetParent(_worldContentRoot);
        _previewRoot = previewObj.transform;

        // ミノ一覧用ルート (画面固定, _rootTransform直下).
        var minoListObj = new GameObject("MinoListRoot");
        minoListObj.transform.SetParent(_rootTransform);
        _minoListRoot = minoListObj.transform;

        // 盤面グリッド生成 (_worldContentRoot の子).
        CreateBoard(mapSize);

        // フォーカス層盤面 (半透明, 現在配置層を示す).
        CreateFocusLayerBoard(mapSize);

        // 操作方法ガイド.
        CreateControlGuide();

        // パズル用ライト (盤面+ミノの影生成用).
        CreatePuzzleLight();
    }

    /// <summary> 盤面グリッドを生成. ミノブロックと同サイズの薄いCubeを敷き詰め. </summary>
    private void CreateBoard(Vector3Int mapSize)
    {
        var boardObj = new GameObject("BoardRoot");
        boardObj.transform.SetParent(_worldContentRoot);
        _boardRoot = boardObj.transform;

        // 市松模様の2色.
        var colorA = new Color(0.18f, 0.18f, 0.22f);
        var colorB = new Color(0.25f, 0.25f, 0.30f);

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int z = 0; z < mapSize.z; z++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(_boardRoot);
                // y=0 の真下に薄く配置 (ミノブロックと重ならないようにわずかに下).
                cube.transform.localPosition = new Vector3(x, -0.05f, z);
                cube.transform.localScale = new Vector3(0.95f, 0.1f, 0.95f);
                cube.name = $"Board_{x}_{z}";

                // コライダー不要.
                var collider = cube.GetComponent<Collider>();
                if (collider != null) Object.Destroy(collider);

                var renderer = cube.GetComponent<Renderer>();
                renderer.material.color = (x + z) % 2 == 0 ? colorA : colorB;

                _boardObjects.Add(cube);
            }
        }
    }

    /// <summary> フォーカス層盤面を生成. 半透明で現在配置可能な層を示す. </summary>
    private void CreateFocusLayerBoard(Vector3Int mapSize)
    {
        var boardObj = new GameObject("FocusLayerBoardRoot");
        boardObj.transform.SetParent(_rootTransform);
        boardObj.transform.localPosition = new Vector3(0f, -0.03f, 0f);
        _focusLayerBoardRoot = boardObj.transform;

        // 半透明の市松模様 (視認しやすいアルファ値).
        var colorA = new Color(0.5f, 0.5f, 0.65f, 0.4f);
        var colorB = new Color(0.6f, 0.6f, 0.75f, 0.4f);

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int z = 0; z < mapSize.z; z++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(_focusLayerBoardRoot);
                cube.transform.localPosition = new Vector3(x, 0f, z);
                cube.transform.localScale = new Vector3(0.95f, 0.06f, 0.95f);
                cube.name = $"FocusBoard_{x}_{z}";

                // コライダー不要.
                var collider = cube.GetComponent<Collider>();
                if (collider != null) Object.Destroy(collider);

                var color = (x + z) % 2 == 0 ? colorA : colorB;
                var renderer = cube.GetComponent<Renderer>();
                renderer.material.color = color;
                SetRendererAlpha(renderer, color.a);

                // 半透明盤面は影を落とさない.
                var mr = cube.GetComponent<MeshRenderer>();
                if (mr != null) mr.shadowCastingMode = ShadowCastingMode.Off;

                _focusLayerBoardObjects.Add(cube);
            }
        }
    }

    /// <summary> 操作方法ガイドを盤面左側に表示. </summary>
    private void CreateControlGuide()
    {
        _controlGuideObj = new GameObject("ControlGuide");
        _controlGuideObj.transform.SetParent(_rootTransform);
        _controlGuideObj.transform.localPosition = new Vector3(-1.5f, 0f, _mapSize.z - 0.5f);
        _controlGuideObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        var tm = _controlGuideObj.AddComponent<TextMesh>();
        tm.text = "[操作]\nWASD: 移動\nQ/E: 回転\nT/G: 階層\nTab: ミノ切替\nSpace: 配置\nRClick: 解除\nWheel: スクロール";
        tm.fontSize = 24;
        tm.characterSize = 0.12f;
        tm.anchor = TextAnchor.UpperRight;
        tm.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);
    }

    /// <summary> パズル用ディレクショナルライトを生成 (影生成用). </summary>
    private void CreatePuzzleLight()
    {
        _puzzleLightObj = new GameObject("PuzzleLight");
        _puzzleLightObj.transform.SetParent(_rootTransform);
        _puzzleLightObj.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);

        var light = _puzzleLightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.95f, 0.9f);
        light.intensity = 0.8f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.6f;
    }

    /// <summary> 指定セル数のグリッドテクスチャを取得/生成 (同サイズはキャッシュ). </summary>
    private Texture2D GetOrCreateGridTexture(int cellsX, int cellsZ)
    {
        var key = new Vector2Int(cellsX, cellsZ);
        if (_gridTextureCache.TryGetValue(key, out var cached)) return cached;

        const int cellPx = 16;
        int texW = cellsX * cellPx;
        int texH = cellsZ * cellPx;

        var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        // 白系ベース色 (マテリアルカラーとの乗算で状態色を反映).
        var cellA = new Color(0.70f, 0.70f, 0.75f);
        var cellB = new Color(0.90f, 0.90f, 0.95f);
        var lineCol = new Color(0.40f, 0.40f, 0.45f);

        var pixels = new Color[texW * texH];
        for (int py = 0; py < texH; py++)
        {
            for (int px = 0; px < texW; px++)
            {
                int cx = px / cellPx;
                int cy = py / cellPx;
                int lx = px % cellPx;
                int ly = py % cellPx;

                // セル境界線 (1px).
                bool isLine = (lx == 0 || ly == 0);
                pixels[py * texW + px] = isLine ? lineCol : ((cx + cy) % 2 == 0 ? cellA : cellB);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        _gridTextureCache[key] = tex;
        return tex;
    }

    public void ShowBlock(Vector3Int gridPos, BlockData data)
    {
        if (_blockObjects.ContainsKey(gridPos))
        {
            HideBlock(gridPos);
        }

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(_worldContentRoot);
        cube.transform.localPosition = GridToWorld(gridPos);
        cube.transform.localScale = Vector3.one * 0.8f;
        cube.name = $"Block_{gridPos.x}_{gridPos.y}_{gridPos.z}";

        var color = EffectColors.ContainsKey(data.EffectType) ? EffectColors[data.EffectType] : Color.gray;
        var renderer = cube.GetComponent<Renderer>();
        renderer.material.color = color;

        // レイヤー透明度を適用.
        if (_layerAlphas.TryGetValue(gridPos.y, out float alpha))
        {
            SetRendererAlpha(renderer, alpha);
        }

        _blockObjects[gridPos] = cube;
        _blockDataMap[gridPos] = data;

        // 隣接ブロックとのコネクタを更新.
        UpdateConnectors(gridPos);
    }

    public void HideBlock(Vector3Int gridPos)
    {
        // コネクタを先に除去.
        RemoveConnectorsForBlock(gridPos);
        _blockDataMap.Remove(gridPos);

        if (_blockObjects.TryGetValue(gridPos, out var obj))
        {
            Object.Destroy(obj);
            _blockObjects.Remove(gridPos);
        }
    }

    public void ShowMinoPreview(List<Vector3Int> positions, bool canPlace, List<BlockData> blockEffects)
    {
        HideMinoPreview();

        for (int i = 0; i < positions.Count; i++)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(_previewRoot);
            cube.transform.localPosition = GridToWorld(positions[i]);
            cube.transform.localScale = Vector3.one * 0.8f;
            cube.name = $"Preview_{i}";

            // コライダー無効化 (プレビューはraycast対象外).
            var collider = cube.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);

            // プレビューは影を落とさない.
            var meshRenderer = cube.GetComponent<MeshRenderer>();
            if (meshRenderer != null) meshRenderer.shadowCastingMode = ShadowCastingMode.Off;

            // 効果色を反映しつつ配置可否を明度で表現.
            var blockData = (blockEffects != null && i < blockEffects.Count) ? blockEffects[i] : new BlockData();
            var baseColor = EffectColors.ContainsKey(blockData.EffectType) ? EffectColors[blockData.EffectType] : Color.gray;

            if (!canPlace)
            {
                // 配置不可: 赤みを加える.
                baseColor = Color.Lerp(baseColor, new Color(1f, 0f, 0f), 0.4f);
            }
            else
            {
                // 配置可: やや明るく.
                baseColor = Color.Lerp(baseColor, Color.white, 0.2f);
            }
            baseColor.a = 0.5f;

            var renderer = cube.GetComponent<Renderer>();
            renderer.material.color = baseColor;
            SetRendererAlpha(renderer, baseColor.a);

            _previewObjects.Add(cube);
        }
    }

    public void HideMinoPreview()
    {
        for (int i = 0; i < _previewObjects.Count; i++)
        {
            if (_previewObjects[i] != null)
            {
                Object.Destroy(_previewObjects[i]);
            }
        }
        _previewObjects.Clear();
    }

    // グリッド背景 tint 色定数 (明度のみで状態表現. テクスチャ色との乗算).
    private static readonly Color GridTintSelected  = new Color(0.85f, 0.85f, 0.85f, 0.7f);  // 明 (選択中).
    private static readonly Color GridTintAvailable = new Color(0.45f, 0.45f, 0.45f, 0.5f);  // 中 (未選択).
    private static readonly Color GridTintPlaced    = new Color(0.22f, 0.22f, 0.22f, 0.35f); // 暗 (配置済み).

    public void ShowMinoList(List<MinoShapeData> shapes, int selectedIndex, List<bool> placedFlags = null)
    {
        ClearMinoList();

        if (shapes == null || shapes.Count == 0) return;

        // マップ右側にミノ一覧を配置.
        float listOriginX = _mapSize.x + 2f;
        float listOriginZ = _mapSize.z - 1f;
        float blockScale = 0.65f;
        int numColumns = 3;
        float columnWidth = 2.5f;

        // 各ミノの Bounding Box を事前計算.
        var boundsArr = new (int minX, int maxX, int minZ, int maxZ, int gridW, int gridH)[shapes.Count];
        for (int i = 0; i < shapes.Count; i++)
        {
            int bMinX = int.MaxValue, bMaxX = int.MinValue;
            int bMinZ = int.MaxValue, bMaxZ = int.MinValue;
            for (int b = 0; b < shapes[i].BlockCount; b++)
            {
                var off = shapes[i].BlockOffsets[b];
                if (off.x < bMinX) bMinX = off.x;
                if (off.x > bMaxX) bMaxX = off.x;
                if (off.z < bMinZ) bMinZ = off.z;
                if (off.z > bMaxZ) bMaxZ = off.z;
            }
            boundsArr[i] = (bMinX, bMaxX, bMinZ, bMaxZ, bMaxX - bMinX + 1, bMaxZ - bMinZ + 1);
        }

        // 複数列レイアウト: 各列ごとにカーソルZを管理.
        var slotCentersX = new float[shapes.Count];
        var slotCentersZ = new float[shapes.Count];
        var cursorZArr = new float[numColumns];
        for (int c = 0; c < numColumns; c++) cursorZArr[c] = listOriginZ;

        for (int i = 0; i < shapes.Count; i++)
        {
            int col = i % numColumns;
            var bd = boundsArr[i];
            slotCentersX[i] = listOriginX + col * columnWidth;
            slotCentersZ[i] = cursorZArr[col] - bd.maxZ * blockScale;
            float bottomZ = slotCentersZ[i] + bd.minZ * blockScale;
            cursorZArr[col] = bottomZ - 2 * blockScale; // グリッド端同士の間に 1ブロック分の隙間.
        }

        for (int i = 0; i < shapes.Count; i++)
        {
            var shape = shapes[i];
            bool isSelected = (i == selectedIndex);
            bool isPlaced = (placedFlags != null && i < placedFlags.Count && placedFlags[i]);

            var (minX, maxX, minZ, maxZ, gridW, gridH) = boundsArr[i];

            // 各ミノのスロット中心位置 (複数列対応).
            var slotCenter = new Vector3(slotCentersX[i], 0f, slotCentersZ[i]);

            // グリッドテクスチャ付き背景 Quad (盤面と同様の市松模様).
            var gridTex = GetOrCreateGridTexture(gridW, gridH);

            Color tintColor;
            if (isSelected)
                tintColor = GridTintSelected;
            else if (isPlaced)
                tintColor = GridTintPlaced;
            else
                tintColor = GridTintAvailable;

            var frame = GameObject.CreatePrimitive(PrimitiveType.Quad);
            frame.transform.SetParent(_minoListRoot);

            float bboxCenterX = (minX + maxX) * 0.5f * blockScale;
            float bboxCenterZ = (minZ + maxZ) * 0.5f * blockScale;
            frame.transform.localPosition = slotCenter + new Vector3(bboxCenterX, -0.04f, bboxCenterZ);
            frame.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            frame.transform.localScale = new Vector3(gridW * blockScale, gridH * blockScale, 1f);
            frame.name = $"MinoList_Grid_{i}";

            var frameCollider = frame.GetComponent<Collider>();
            if (frameCollider != null) Object.Destroy(frameCollider);

            var frameRenderer = frame.GetComponent<Renderer>();
            frameRenderer.material.mainTexture = gridTex;
            frameRenderer.material.color = tintColor;
            SetRendererAlpha(frameRenderer, tintColor.a);

            _minoListObjects.Add(frame);

            // 各ブロックを小型キューブで配置.
            // 明度のみで状態表現: 選択中=明るい, 未選択=そのまま, 配置済み=暗い.
            float brightnessMul;
            float blockAlpha;
            if (isSelected)
            {
                brightnessMul = 1.3f; // 明るく.
                blockAlpha = 1f;
            }
            else if (isPlaced)
            {
                brightnessMul = 0.45f; // 暗く.
                blockAlpha = 0.55f;
            }
            else
            {
                brightnessMul = 0.85f; // やや暗め (選択中との差を出す).
                blockAlpha = 1f;
            }

            for (int b = 0; b < shape.BlockCount; b++)
            {
                var offset = shape.BlockOffsets[b];
                var blockData = (b < shape.BlockEffects.Count) ? shape.BlockEffects[b] : new BlockData();

                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(_minoListRoot);
                cube.transform.localPosition = slotCenter + new Vector3(
                    offset.x * blockScale,
                    0f,
                    offset.z * blockScale
                );
                cube.transform.localScale = Vector3.one * (blockScale * 0.9f);
                cube.name = $"MinoList_{i}_Block_{b}";

                // コライダー不要.
                var collider = cube.GetComponent<Collider>();
                if (collider != null) Object.Destroy(collider);

                // 色: 効果種別色を保持し、明度のみ変更.
                var baseColor = EffectColors.ContainsKey(blockData.EffectType)
                    ? EffectColors[blockData.EffectType]
                    : Color.gray;

                baseColor.r = Mathf.Clamp01(baseColor.r * brightnessMul);
                baseColor.g = Mathf.Clamp01(baseColor.g * brightnessMul);
                baseColor.b = Mathf.Clamp01(baseColor.b * brightnessMul);
                baseColor.a = blockAlpha;

                var renderer = cube.GetComponent<Renderer>();
                renderer.material.color = baseColor;
                if (blockAlpha < 1f) SetRendererAlpha(renderer, blockAlpha);

                _minoListObjects.Add(cube);
            }
        }
    }

    /// <summary> ミノ一覧表示をクリア. </summary>
    private void ClearMinoList()
    {
        for (int i = 0; i < _minoListObjects.Count; i++)
        {
            if (_minoListObjects[i] != null)
            {
                Object.Destroy(_minoListObjects[i]);
            }
        }
        _minoListObjects.Clear();
    }

    #region コネクタ管理

    /// <summary> 指定位置の隣接コネクタを更新. </summary>
    private void UpdateConnectors(Vector3Int pos)
    {
        if (!_blockDataMap.TryGetValue(pos, out var blockA)) return;

        for (int d = 0; d < ConnectDirs.Length; d++)
        {
            var neighbor = pos + ConnectDirs[d];
            var key = NormalizeConnectorKey(pos, neighbor);

            if (_blockDataMap.TryGetValue(neighbor, out var blockB) && ShouldConnect(pos, neighbor, blockA, blockB))
            {
                if (!_connectorObjects.ContainsKey(key))
                {
                    CreateConnector(pos, neighbor, blockA, blockB);
                }
            }
            else
            {
                RemoveConnector(key);
            }
        }
    }

    /// <summary> 2ブロック間にコネクタ (細長いCube) を生成. </summary>
    private void CreateConnector(Vector3Int posA, Vector3Int posB, BlockData dataA, BlockData dataB)
    {
        var key = NormalizeConnectorKey(posA, posB);
        if (_connectorObjects.ContainsKey(key)) return;

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(_worldContentRoot);
        cube.transform.localPosition = (GridToWorld(posA) + GridToWorld(posB)) * 0.5f;

        // 接続方向に細長く、垂直方向に細く.
        var dir = posB - posA;
        const float connLen = 0.2f;
        const float connThick = 0.35f;

        if (dir.x != 0) cube.transform.localScale = new Vector3(connLen, connThick, connThick);
        else if (dir.y != 0) cube.transform.localScale = new Vector3(connThick, connLen, connThick);
        else cube.transform.localScale = new Vector3(connThick, connThick, connLen);

        cube.name = $"Connector_{posA.x}{posA.y}{posA.z}_{posB.x}{posB.y}{posB.z}";

        var collider = cube.GetComponent<Collider>();
        if (collider != null) Object.Destroy(collider);

        // 色: 両ブロック色の平均を明るく.
        var colorA = EffectColors.ContainsKey(dataA.EffectType) ? EffectColors[dataA.EffectType] : Color.gray;
        var colorB = EffectColors.ContainsKey(dataB.EffectType) ? EffectColors[dataB.EffectType] : Color.gray;
        var connColor = Color.Lerp((colorA + colorB) * 0.5f, Color.white, 0.3f);

        var renderer = cube.GetComponent<Renderer>();
        renderer.material.color = connColor;

        if (_layerAlphas.TryGetValue(posA.y, out float alpha))
        {
            SetRendererAlpha(renderer, alpha);
        }

        _connectorObjects[key] = cube;
    }

    /// <summary> 指定位置に関連する全コネクタを除去. </summary>
    private void RemoveConnectorsForBlock(Vector3Int pos)
    {
        for (int d = 0; d < ConnectDirs.Length; d++)
        {
            RemoveConnector(NormalizeConnectorKey(pos, pos + ConnectDirs[d]));
        }
    }

    /// <summary> コネクタを除去. </summary>
    private void RemoveConnector((Vector3Int, Vector3Int) key)
    {
        if (_connectorObjects.TryGetValue(key, out var obj))
        {
            Object.Destroy(obj);
            _connectorObjects.Remove(key);
        }
    }

    /// <summary> コネクタキーを正規化 (順序統一). </summary>
    private static (Vector3Int, Vector3Int) NormalizeConnectorKey(Vector3Int a, Vector3Int b)
    {
        if (a.x < b.x || (a.x == b.x && a.y < b.y) || (a.x == b.x && a.y == b.y && a.z < b.z))
            return (a, b);
        return (b, a);
    }

    /// <summary> 2つのブロックが連結表示すべきか判定 (位置込み). </summary>
    private bool ShouldConnect(Vector3Int posA, Vector3Int posB, BlockData a, BlockData b)
    {
        // Grant ↔ Grant (付与塊内).
        if (a.EffectType == BlockEffectType.Grant && b.EffectType == BlockEffectType.Grant) return true;
        // Grant ↔ Attack (接続マップに登録されている場合のみ).
        if (a.EffectType == BlockEffectType.Grant && b.EffectType == BlockEffectType.Attack)
            return IsGrantConnectedTo(posA, posB);
        if (a.EffectType == BlockEffectType.Attack && b.EffectType == BlockEffectType.Grant)
            return IsGrantConnectedTo(posB, posA);
        return false;
    }

    /// <summary> 付与ブロックが指定攻撃に接続されているか判定. </summary>
    private bool IsGrantConnectedTo(Vector3Int grantPos, Vector3Int attackPos)
    {
        return _grantConnectionMap.TryGetValue(grantPos, out var connected)
            && connected == attackPos;
    }

    #endregion

    #region パルスアニメーション / ホバーハイライト

    public void SetBlockPulsing(List<Vector3Int> positions, bool pulsing)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            if (pulsing)
                _pulsingBlocks.Add(positions[i]);
            else
                _pulsingBlocks.Remove(positions[i]);
        }

        // パルス解除時は通常スケールに戻す.
        if (!pulsing)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                if (_blockObjects.TryGetValue(positions[i], out var obj))
                {
                    // ハイライト中でなければ通常スケールに.
                    if (!_highlightedPositions.Contains(positions[i]))
                    {
                        obj.transform.localScale = Vector3.one * NormalBlockScale;
                    }
                }
            }
        }
    }

    public void SetBlockHighlight(Vector3Int pos, bool highlight)
    {
        if (highlight)
        {
            _highlightedPositions.Add(pos);
            if (_blockObjects.TryGetValue(pos, out var obj))
            {
                obj.transform.localScale = Vector3.one * PulseScaleMax;
            }
        }
        else
        {
            _highlightedPositions.Remove(pos);
            if (_blockObjects.TryGetValue(pos, out var obj))
            {
                // パルス対象ならパルスに任せる. それ以外は通常スケール.
                if (!_pulsingBlocks.Contains(pos))
                {
                    obj.transform.localScale = Vector3.one * NormalBlockScale;
                }
            }
        }
    }

    public void SetMinoListScroll(float scrollOffset)
    {
        _minoListScrollOffset = scrollOffset;
        if (_minoListRoot != null)
        {
            var pos = _minoListRoot.localPosition;
            pos.z = scrollOffset;
            _minoListRoot.localPosition = pos;
        }
    }

    public void SetFocusLayerBoard(int y)
    {
        _focusBoardTargetY = y;
    }

    public void UpdateAnimations(float deltaTime)
    {
        // ワールドコンテンツを下降 (不透明盤面+ブロック+コネクタが一緒に沈む).
        if (Mathf.Abs(_focusBoardCurrentY - _focusBoardTargetY) > 0.001f)
        {
            _focusBoardCurrentY = Mathf.Lerp(_focusBoardCurrentY, _focusBoardTargetY, FocusBoardLerpSpeed * deltaTime);
            if (Mathf.Abs(_focusBoardCurrentY - _focusBoardTargetY) < 0.01f)
                _focusBoardCurrentY = _focusBoardTargetY;
            if (_worldContentRoot != null)
                _worldContentRoot.localPosition = new Vector3(0f, -_focusBoardCurrentY, 0f);
        }

        // パルスアニメーション.
        if (_pulsingBlocks.Count == 0) return;

        float t = Mathf.PingPong(Time.time * PulseSpeed, 1f);
        float scale = Mathf.Lerp(PulseScaleMin, PulseScaleMax, t);

        foreach (var pos in _pulsingBlocks)
        {
            // ハイライト中のブロックはパルスをスキップ (最大サイズ固定).
            if (_highlightedPositions.Contains(pos)) continue;

            if (_blockObjects.TryGetValue(pos, out var obj))
            {
                obj.transform.localScale = Vector3.one * scale;
            }
        }
    }

    #endregion

    public void SetGrantConnections(Dictionary<Vector3Int, Vector3Int> connections)
    {
        _grantConnectionMap = connections != null
            ? new Dictionary<Vector3Int, Vector3Int>(connections)
            : new Dictionary<Vector3Int, Vector3Int>();

        // Grant/Attack関連のコネクタを再評価.
        RefreshGrantAttackConnectors();
    }

    /// <summary> Grant/Attackブロックのコネクタを全て再評価. </summary>
    private void RefreshGrantAttackConnectors()
    {
        var positionsToRefresh = new List<Vector3Int>();
        foreach (var kvp in _blockDataMap)
        {
            if (kvp.Value.EffectType == BlockEffectType.Grant || kvp.Value.EffectType == BlockEffectType.Attack)
                positionsToRefresh.Add(kvp.Key);
        }
        for (int i = 0; i < positionsToRefresh.Count; i++)
        {
            UpdateConnectors(positionsToRefresh[i]);
        }
    }

    #region 攻撃選択メニュー

    public void ShowGrantLinkMenu(Vector3Int menuAnchor, List<Vector3Int> attackPositions, List<BlockData> attackData)
    {
        HideGrantLinkMenu();

        _menuAttackPositions = attackPositions;

        var menuObj = new GameObject("GrantLinkMenuRoot");
        menuObj.transform.SetParent(_worldContentRoot);
        _menuRoot = menuObj.transform;

        // メニュー表示位置: 付与塊の上方.
        var menuBase = GridToWorld(menuAnchor) + new Vector3(1.5f, 0.5f, 0f);

        for (int i = 0; i < attackPositions.Count; i++)
        {
            var data = (i < attackData.Count) ? attackData[i] : new BlockData();
            var attackPos = attackPositions[i];

            // エントリ: 小さいCube + 座標テキスト表示用.
            var entry = GameObject.CreatePrimitive(PrimitiveType.Cube);
            entry.transform.SetParent(_menuRoot);
            entry.transform.localPosition = menuBase + new Vector3(0f, 0f, -i * 0.6f);
            entry.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            entry.name = $"LinkMenu_{i}";

            var collider = entry.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);

            var color = EffectColors.ContainsKey(data.EffectType) ? EffectColors[data.EffectType] : Color.gray;
            var renderer = entry.GetComponent<Renderer>();
            renderer.material.color = color;

            _menuObjects.Add(entry);

            // 座標ラベル (小さなCubeの隣に位置情報を表すマーカー).
            var label = GameObject.CreatePrimitive(PrimitiveType.Cube);
            label.transform.SetParent(_menuRoot);
            label.transform.localPosition = menuBase + new Vector3(0.5f, 0f, -i * 0.6f);
            label.transform.localScale = new Vector3(0.6f, 0.15f, 0.15f);
            label.name = $"LinkMenuLabel_{i}";

            var labelCollider = label.GetComponent<Collider>();
            if (labelCollider != null) Object.Destroy(labelCollider);

            var labelRenderer = label.GetComponent<Renderer>();
            // 座標のY値で色を変え階層を示す.
            float yHue = (attackPos.y % 4) * 0.25f;
            labelRenderer.material.color = Color.HSVToRGB(yHue, 0.3f, 0.7f);

            _menuObjects.Add(label);
        }

        _menuSelectedIndex = -1;
    }

    public void HideGrantLinkMenu()
    {
        for (int i = 0; i < _menuObjects.Count; i++)
        {
            if (_menuObjects[i] != null) Object.Destroy(_menuObjects[i]);
        }
        _menuObjects.Clear();
        _menuAttackPositions = null;
        _menuSelectedIndex = -1;

        if (_menuRoot != null)
        {
            Object.Destroy(_menuRoot.gameObject);
            _menuRoot = null;
        }
    }

    public void SetGrantLinkMenuSelection(int index)
    {
        // 旧選択のハイライト解除.
        if (_menuSelectedIndex >= 0 && _menuAttackPositions != null && _menuSelectedIndex < _menuAttackPositions.Count)
        {
            SetBlockHighlight(_menuAttackPositions[_menuSelectedIndex], false);
            // メニューCubeを通常サイズに.
            int objIdx = _menuSelectedIndex * 2; // Cube + Label で2個ずつ.
            if (objIdx < _menuObjects.Count && _menuObjects[objIdx] != null)
            {
                _menuObjects[objIdx].transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            }
        }

        _menuSelectedIndex = index;

        // 新選択のハイライト.
        if (index >= 0 && _menuAttackPositions != null && index < _menuAttackPositions.Count)
        {
            SetBlockHighlight(_menuAttackPositions[index], true);
            // メニューCubeを拡大.
            int objIdx = index * 2;
            if (objIdx < _menuObjects.Count && _menuObjects[objIdx] != null)
            {
                _menuObjects[objIdx].transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            }
        }
    }

    #endregion

    public void SetLayerAlpha(int y, float alpha)
    {
        _layerAlphas[y] = alpha;

        foreach (var kvp in _blockObjects)
        {
            if (kvp.Key.y == y)
            {
                var renderer = kvp.Value.GetComponent<Renderer>();
                if (renderer != null) SetRendererAlpha(renderer, alpha);
            }
        }

        // コネクタも同レイヤーのものを更新.
        foreach (var kvp in _connectorObjects)
        {
            if (kvp.Key.Item1.y == y || kvp.Key.Item2.y == y)
            {
                var renderer = kvp.Value.GetComponent<Renderer>();
                if (renderer != null) SetRendererAlpha(renderer, alpha);
            }
        }
    }

    public void ResetAllLayerAlpha()
    {
        _layerAlphas.Clear();

        foreach (var kvp in _blockObjects)
        {
            var renderer = kvp.Value.GetComponent<Renderer>();
            if (renderer != null) SetRendererAlpha(renderer, 1f);
        }

        foreach (var kvp in _connectorObjects)
        {
            var renderer = kvp.Value.GetComponent<Renderer>();
            if (renderer != null) SetRendererAlpha(renderer, 1f);
        }
    }

    public void Dispose()
    {
        foreach (var kvp in _blockObjects)
        {
            if (kvp.Value != null) Object.Destroy(kvp.Value);
        }
        _blockObjects.Clear();
        _blockDataMap.Clear();

        // コネクタ破棄.
        foreach (var kvp in _connectorObjects)
        {
            if (kvp.Value != null) Object.Destroy(kvp.Value);
        }
        _connectorObjects.Clear();

        // パルス/メニュー破棄.
        _pulsingBlocks.Clear();
        _highlightedPositions.Clear();
        HideGrantLinkMenu();

        HideMinoPreview();
        ClearMinoList();

        if (_previewRoot != null) Object.Destroy(_previewRoot.gameObject);
        if (_minoListRoot != null) Object.Destroy(_minoListRoot.gameObject);

        for (int i = 0; i < _boardObjects.Count; i++)
        {
            if (_boardObjects[i] != null) Object.Destroy(_boardObjects[i]);
        }
        _boardObjects.Clear();
        if (_boardRoot != null) Object.Destroy(_boardRoot.gameObject);

        // フォーカス層盤面破棄.
        for (int i = 0; i < _focusLayerBoardObjects.Count; i++)
        {
            if (_focusLayerBoardObjects[i] != null) Object.Destroy(_focusLayerBoardObjects[i]);
        }
        _focusLayerBoardObjects.Clear();
        if (_focusLayerBoardRoot != null) Object.Destroy(_focusLayerBoardRoot.gameObject);

        // 操作ガイド破棄.
        if (_controlGuideObj != null) Object.Destroy(_controlGuideObj);

        // パズルライト破棄.
        if (_puzzleLightObj != null) Object.Destroy(_puzzleLightObj);

        // ワールドコンテンツルート破棄.
        if (_worldContentRoot != null) Object.Destroy(_worldContentRoot.gameObject);

        // グリッドテクスチャキャッシュ破棄.
        foreach (var kvp in _gridTextureCache)
        {
            if (kvp.Value != null) Object.Destroy(kvp.Value);
        }
        _gridTextureCache.Clear();
    }

    /// <summary> グリッド座標 => ワールド座標. </summary>
    private Vector3 GridToWorld(Vector3Int gridPos)
    {
        return new Vector3(gridPos.x, gridPos.y, gridPos.z);
    }

    /// <summary> マテリアルの透明度を設定. URP Lit シェーダー対応. </summary>
    private void SetRendererAlpha(Renderer renderer, float alpha)
    {
        var mat = renderer.material;
        var color = mat.color;
        color.a = alpha;
        mat.color = color;

        if (alpha < 1f)
        {
            // URP Lit: Surface Type を Transparent に切り替え.
            mat.SetFloat("_Surface", 1f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            // ブレンドモード設定.
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.renderQueue = 3000;
        }
        else
        {
            // URP Lit: Surface Type を Opaque に戻す.
            mat.SetFloat("_Surface", 0f);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");

            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetFloat("_ZWrite", 1f);
            mat.renderQueue = -1;
        }
    }
}
