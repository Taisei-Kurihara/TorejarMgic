using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary> パズルマップの入力ハンドリング + Model/View統合. </summary>
/// <remarks> MonoBehaviourなし. PuzzleMap ActionMapの入力を処理. </remarks>
public class PuzzleMapController
{
    private PuzzleMapModel _model;
    private IPuzzleViewState _viewState;
    private InputSystem_Actions _inputActions;
    private Camera _camera;

    private Vector3Int _mapSize;
    private List<MinoShapeData> _minoShapes;
    private List<MinoInstance> _minoInstances = new List<MinoInstance>();

    private int _selectedMinoIndex = -1;
    private MinoInstance _activeMino;

    // マウスカーソル追従用.
    private Vector2 _lastMousePos;

    // ホバーハイライト (同一ミノ全ブロック対応).
    private List<Vector3Int> _lastHoveredMinoPositions;

    // ミノ一覧スクロール.
    private float _minoListScrollOffset;
    private const float ScrollSpeed = 0.005f;

    // 付与→攻撃 編集モード.
    private GrantCluster _editingCluster;
    private List<Vector3Int> _editAttackOptions;
    private int _editMenuIndex = -1;
    private int _savedFocusLayer;
    private Vector3 _editMenuBasePos;           // メニュー基準ワールド座標.
    private const float EditMenuItemSpacing = 0.6f; // メニュー項目間隔 (View側と一致).
    private const float EditMenuHitRadius = 0.35f;  // メニュー項目のヒット判定半径.
    private bool _editModeJustEntered;          // 編集モード開始フレームフラグ (入力無視用).

    // ミノ一覧レイアウト定数 (PuzzleMapView3D.ShowMinoList と一致させる).
    private const float ListOriginXOffset = 2f;
    private const float ListBlockScale = 0.65f;
    private const int ListNumColumns = 3;
    private const float ListColumnWidth = 2.5f;

    // 動的算出されたスロット中心座標 + ヒット判定半径.
    private readonly List<float> _slotCenterXList = new List<float>();
    private readonly List<float> _slotCenterZList = new List<float>();
    private readonly List<float> _slotHitRadiusZList = new List<float>();

    /// <summary> 現在のViewState. </summary>
    public IPuzzleViewState CurrentViewState => _viewState;

    /// <summary> 各ミノの配置済みフラグリストを構築. </summary>
    private List<bool> BuildPlacedFlags()
    {
        var flags = new List<bool>(_minoInstances.Count);
        for (int i = 0; i < _minoInstances.Count; i++)
        {
            flags.Add(_minoInstances[i].IsPlaced);
        }
        return flags;
    }

    /// <summary> 初期化. IPuzzleViewStateで2D/3Dを切り替え. </summary>
    public void Initialize(IPuzzleViewState viewState, Vector3Int mapSize, Camera camera, Transform viewRoot)
    {
        _mapSize = mapSize;
        _camera = camera;

        // Model.
        _model = new PuzzleMapModel(mapSize);

        // ViewState Enter.
        _viewState = viewState;
        _viewState.Enter(mapSize, camera, viewRoot);

        // CSVからミノ形状読み込み. CSV未設定時はテスト用フォールバック.
        _minoShapes = MinoShapeMasterCsv.LoadFromCsv("MinoShapeMaster.csv");
        if (_minoShapes.Count == 0)
        {
            _minoShapes = CreateFallbackMinoShapes();
            Debug.Log("PuzzleMapController: CSV未設定のためテスト用ミノを使用");
        }

        // ミノインスタンス生成 (map右側に配置).
        CreateMinoInstances(mapSize);

        // InputSystem PuzzleMap ActionMap.
        _inputActions = new InputSystem_Actions();
        BindInput();
        _inputActions.PuzzleMap.Enable();

        Debug.Log($"PuzzleMapController: 初期化完了 (ViewState={_viewState.GetType().Name}, ミノ数={_minoShapes.Count})");
    }

    /// <summary> ViewStateを切り替え. 実行中に2D/3Dを変更可能. </summary>
    public void SwitchViewState(IPuzzleViewState newViewState, Camera camera, Transform viewRoot)
    {
        // 旧State Exit.
        _viewState?.Exit();

        // 新State Enter.
        _viewState = newViewState;
        _viewState.Enter(_mapSize, camera, viewRoot);
        _camera = camera;

        // 現在のModel状態をViewに反映.
        var occupied = _model.GetAllOccupiedPositions();
        for (int i = 0; i < occupied.Count; i++)
        {
            var pos = occupied[i];
            _viewState.View.ShowBlock(pos, _model.GetBlock(pos));
        }

        _viewState.View.ShowMinoList(_minoShapes, _selectedMinoIndex, BuildPlacedFlags());
        UpdateMinoPreview();

        Debug.Log($"PuzzleMapController: ViewState切り替え => {_viewState.GetType().Name}");
    }

    /// <summary> 入力バインド. </summary>
    private void BindInput()
    {
        var pm = _inputActions.PuzzleMap;

        pm.RotateLeft.performed += _ => OnRotateLeft();
        pm.RotateRight.performed += _ => OnRotateRight();
        pm.LayerUp.performed += _ => OnLayerUp();
        pm.LayerDown.performed += _ => OnLayerDown();
    }

    /// <summary> ミノインスタンスを生成してmap右側に配置. </summary>
    private void CreateMinoInstances(Vector3Int mapSize)
    {
        _minoInstances.Clear();

        for (int i = 0; i < _minoShapes.Count; i++)
        {
            var instance = new MinoInstance(_minoShapes[i]);
            // map右側にオフセット配置.
            instance.Position = new Vector3Int(mapSize.x + 2, 0, i * 3);
            _minoInstances.Add(instance);
        }

        // スロットレイアウトを算出 (View側と同じアルゴリズム).
        CalculateSlotLayout();

        // Viewにミノ一覧表示.
        _viewState.View.ShowMinoList(_minoShapes, _selectedMinoIndex, BuildPlacedFlags());
    }

    /// <summary> ミノ一覧のスロット中心座標を算出 (View側レイアウトと一致, 複数列対応). </summary>
    private void CalculateSlotLayout()
    {
        _slotCenterXList.Clear();
        _slotCenterZList.Clear();
        _slotHitRadiusZList.Clear();

        float listOriginX = _mapSize.x + ListOriginXOffset;
        float listOriginZ = _mapSize.z - 1f;

        // 各列ごとにカーソルZを管理.
        var cursorZArr = new float[ListNumColumns];
        for (int c = 0; c < ListNumColumns; c++) cursorZArr[c] = listOriginZ;

        for (int i = 0; i < _minoShapes.Count; i++)
        {
            int col = i % ListNumColumns;
            var shape = _minoShapes[i];
            int minZ = int.MaxValue, maxZ = int.MinValue;
            for (int b = 0; b < shape.BlockCount; b++)
            {
                int z = shape.BlockOffsets[b].z;
                if (z < minZ) minZ = z;
                if (z > maxZ) maxZ = z;
            }
            int gridH = maxZ - minZ + 1;

            float slotCenterX = listOriginX + col * ListColumnWidth;
            float slotCenterZ = cursorZArr[col] - maxZ * ListBlockScale;

            _slotCenterXList.Add(slotCenterX);
            _slotCenterZList.Add(slotCenterZ);
            _slotHitRadiusZList.Add(gridH * ListBlockScale * 0.5f + ListBlockScale * 0.3f);

            float bottomZ = slotCenterZ + minZ * ListBlockScale;
            cursorZArr[col] = bottomZ - 2 * ListBlockScale; // グリッド端同士の間に 1ブロック分の隙間.
        }
    }

    /// <summary> 毎フレーム更新. </summary>
    public void Update()
    {
        var mouse = Mouse.current;
        var kb = Keyboard.current;

        // 編集モード中の入力処理.
        if (_editingCluster != null)
        {
            UpdateEditMode(mouse, kb);
            _viewState?.View.UpdateAnimations(Time.deltaTime);
            _viewState?.Update();
            return;
        }

        // マウスカーソル追従 (選択中のミノがカーソル位置に追従).
        if (_activeMino != null && mouse != null && _camera != null)
        {
            var currentMousePos = mouse.position.ReadValue();
            if (currentMousePos != _lastMousePos)
            {
                _lastMousePos = currentMousePos;
                var worldPos = ScreenToGridWorld(currentMousePos);
                int gridX = Mathf.RoundToInt(worldPos.x);
                int gridZ = Mathf.RoundToInt(worldPos.z);

                var newPos = new Vector3Int(gridX, _activeMino.Position.y, gridZ);
                if (newPos != _activeMino.Position)
                {
                    _activeMino.Position = newPos;
                    UpdateMinoPreview();
                }
            }
        }

        // ホバーハイライト (ミノ未選択時のみ).
        if (_activeMino == null && mouse != null && _camera != null)
        {
            UpdateHoverHighlight(mouse.position.ReadValue());
        }

        // 左クリック: ミノ選択 / ミノ配置 / 配置済みミノ再選択 / 付与塊編集.
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && _camera != null)
        {
            OnLeftClick(mouse.position.ReadValue());
        }

        // 右クリック: 選択中ミノを解除.
        if (mouse != null && mouse.rightButton.wasPressedThisFrame)
        {
            DeselectMino();
        }

        // キーボード: ミノ選択 / 配置.
        if (kb != null)
        {
            // Tab: 次のミノ選択 / Shift+Tab: 前のミノ選択.
            if (kb.tabKey.wasPressedThisFrame)
            {
                if (kb.leftShiftKey.isPressed)
                    SelectPreviousMino();
                else
                    SelectNextMino();
            }

            // Space: ミノ配置確定.
            if (kb.spaceKey.wasPressedThisFrame)
            {
                if (_activeMino != null)
                {
                    if (TryPlaceActiveMino())
                        Debug.Log("PuzzleMapController: ミノ配置成功.");
                    else
                        Debug.Log("PuzzleMapController: 配置不可 (範囲外 or 重複).");
                }
            }
        }

        // マウスホイール: ミノ一覧スクロール.
        if (mouse != null)
        {
            float scrollDelta = mouse.scroll.ReadValue().y;
            if (scrollDelta != 0f)
            {
                _minoListScrollOffset -= scrollDelta * ScrollSpeed;
                _minoListScrollOffset = Mathf.Max(0f, _minoListScrollOffset);
                _viewState?.View.SetMinoListScroll(_minoListScrollOffset);
            }
        }

        // アニメーション更新.
        _viewState?.View.UpdateAnimations(Time.deltaTime);

        // ViewState固有の毎フレーム処理 (カメラ回転等).
        _viewState?.Update();
    }

    /// <summary> 左クリック処理. ミノ一覧クリック => 選択, 盤面クリック => 配置/再選択. </summary>
    private void OnLeftClick(Vector2 screenPos)
    {
        var worldPos = ScreenToGridWorld(screenPos);
        float listBorderX = _mapSize.x + 0.5f;

        if (worldPos.x >= listBorderX)
        {
            // ミノ一覧エリア: クリック位置からミノインデックスを特定して選択.
            int index = FindMinoIndexAtWorldPos(worldPos);
            if (index >= 0)
            {
                SelectMino(index);
            }
        }
        else if (_activeMino != null)
        {
            // 盤面エリア (ミノ選択中): 配置.
            if (TryPlaceActiveMino())
                Debug.Log("PuzzleMapController: ミノ配置成功.");
            else
                Debug.Log("PuzzleMapController: 配置不可 (範囲外 or 重複).");
        }
        else
        {
            // 盤面エリア (ミノ未選択): 付与ブロック左クリック → 編集モード / それ以外 → 再選択.
            int focusY = _viewState?.Camera != null ? _viewState.Camera.FocusLayer : 0;
            var gridPos = new Vector3Int(Mathf.RoundToInt(worldPos.x), focusY, Mathf.RoundToInt(worldPos.z));

            // 付与ブロックかチェック → 編集モード開始を試みる.
            if (TryEnterEditMode(gridPos)) return;

            TryPickUpMinoAt(gridPos);
        }
    }

    /// <summary> スクリーン座標をワールド座標 (XZ平面) に変換. </summary>
    private Vector3 ScreenToGridWorld(Vector2 screenPos)
    {
        var worldPos = _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        // トップダウンカメラ: worldPos.x => grid X, worldPos.z => grid Z.
        return worldPos;
    }

    /// <summary> ワールド座標からミノ一覧のインデックスを特定. 該当なしなら -1. </summary>
    private int FindMinoIndexAtWorldPos(Vector3 worldPos)
    {
        // スクロールオフセットを考慮したZ座標.
        float adjustedZ = worldPos.z - _minoListScrollOffset;

        for (int i = 0; i < _slotCenterZList.Count; i++)
        {
            // X方向: 列に対する距離チェック.
            float distX = Mathf.Abs(worldPos.x - _slotCenterXList[i]);
            if (distX > ListColumnWidth * 0.5f) continue;

            // Z方向: スロットに対する距離チェック.
            float distZ = Mathf.Abs(adjustedZ - _slotCenterZList[i]);
            if (distZ <= _slotHitRadiusZList[i])
            {
                if (_minoInstances[i].IsPlaced) return -1;
                return i;
            }
        }

        return -1;
    }

    /// <summary> 次の未配置ミノを選択. </summary>
    public void SelectNextMino()
    {
        int start = _selectedMinoIndex + 1;
        for (int i = 0; i < _minoInstances.Count; i++)
        {
            int idx = (start + i) % _minoInstances.Count;
            if (!_minoInstances[idx].IsPlaced)
            {
                SelectMino(idx);
                return;
            }
        }
    }

    /// <summary> 前の未配置ミノを選択. </summary>
    public void SelectPreviousMino()
    {
        int start = _selectedMinoIndex - 1;
        if (start < 0) start = _minoInstances.Count - 1;
        for (int i = 0; i < _minoInstances.Count; i++)
        {
            int idx = ((start - i) % _minoInstances.Count + _minoInstances.Count) % _minoInstances.Count;
            if (!_minoInstances[idx].IsPlaced)
            {
                SelectMino(idx);
                return;
            }
        }
    }

    /// <summary> 選択中ミノを解除. </summary>
    public void DeselectMino()
    {
        if (_activeMino == null) return;

        _activeMino = null;
        _selectedMinoIndex = -1;

        _viewState.View.HideMinoPreview();
        _viewState.View.ShowMinoList(_minoShapes, _selectedMinoIndex, BuildPlacedFlags());
        Debug.Log("PuzzleMapController: ミノ選択解除.");
    }

    /// <summary> 盤面上の配置済みミノを拾い上げる (Model/View から除去して再選択). </summary>
    private void TryPickUpMinoAt(Vector3Int gridPos)
    {
        // 全配置済みミノを走査し、クリック位置を含むミノを探す.
        for (int i = 0; i < _minoInstances.Count; i++)
        {
            var mino = _minoInstances[i];
            if (!mino.IsPlaced) continue;

            var positions = mino.GetWorldBlockPositions();
            bool found = false;
            for (int p = 0; p < positions.Count; p++)
            {
                if (positions[p] == gridPos)
                {
                    found = true;
                    break;
                }
            }

            if (!found) continue;

            // ホバーハイライトをクリア (これからブロックを除去するため).
            ClearHoverHighlight();

            // Model からブロック除去.
            _model.RemoveMino(mino);

            // View からブロック除去.
            for (int p = 0; p < positions.Count; p++)
            {
                _viewState.View.HideBlock(positions[p]);
            }

            // 配置状態を解除してアクティブに.
            mino.IsPlaced = false;
            _selectedMinoIndex = i;
            _activeMino = mino;

            _viewState.View.ShowMinoList(_minoShapes, _selectedMinoIndex, BuildPlacedFlags());
            UpdateMinoPreview();

            // 付与→攻撃 接続を再構築.
            RebuildGrantConnections();

            Debug.Log($"PuzzleMapController: ミノ再選択 ({mino.ShapeData.ShapeId}).");
            return;
        }
    }

    /// <summary> ミノを選択. 選択時に盤面中央に配置位置をリセット. </summary>
    public void SelectMino(int index)
    {
        if (index < 0 || index >= _minoInstances.Count) return;
        if (_minoInstances[index].IsPlaced) return;

        _selectedMinoIndex = index;
        _activeMino = _minoInstances[index];

        // 盤面中央に配置位置をリセット (y は現在のFocusLayerに合わせる).
        int focusY = _viewState?.Camera != null ? _viewState.Camera.FocusLayer : 0;
        _activeMino.Position = new Vector3Int(_mapSize.x / 2, focusY, _mapSize.z / 2);
        _activeMino.Rotation = 0;

        _viewState.View.ShowMinoList(_minoShapes, _selectedMinoIndex, BuildPlacedFlags());
        UpdateMinoPreview();
    }

    /// <summary> 選択中のミノを配置. </summary>
    public bool TryPlaceActiveMino()
    {
        if (_activeMino == null) return false;
        if (!_model.CanPlaceMino(_activeMino)) return false;

        // Modelに配置.
        _model.PlaceMino(_activeMino);
        _activeMino.IsPlaced = true;

        // Viewに反映.
        var positions = _activeMino.GetWorldBlockPositions();
        var effects = _activeMino.ShapeData.BlockEffects;
        for (int i = 0; i < positions.Count; i++)
        {
            var effect = (i < effects.Count) ? effects[i] : new BlockData();
            _viewState.View.ShowBlock(positions[i], effect);
        }

        _viewState.View.HideMinoPreview();
        _activeMino = null;
        _selectedMinoIndex = -1;
        _viewState.View.ShowMinoList(_minoShapes, _selectedMinoIndex, BuildPlacedFlags());

        // 付与→攻撃 接続を再構築.
        RebuildGrantConnections();

        return true;
    }

    /// <summary> ミノプレビュー更新. </summary>
    private void UpdateMinoPreview()
    {
        if (_activeMino == null)
        {
            _viewState?.View.HideMinoPreview();
            return;
        }

        var positions = _activeMino.GetWorldBlockPositions();
        bool canPlace = _model.CanPlaceMino(_activeMino);
        _viewState.View.ShowMinoPreview(positions, canPlace, _activeMino.ShapeData.BlockEffects);
    }

    #region 付与→攻撃 接続管理

    /// <summary> 全付与塊の接続状態を再構築. ミノ配置/除去後に呼ぶ. </summary>
    private void RebuildGrantConnections()
    {
        var clusters = BlockLinkResolver.FindAllGrantClusters(_model);

        for (int c = 0; c < clusters.Count; c++)
        {
            var cluster = clusters[c];

            // 既存接続先が現在も隣接攻撃に含まれていれば維持.
            var existingConnection = _model.GetGrantConnection(cluster.Positions[0]);
            if (existingConnection.HasValue && cluster.AdjacentAttacks.Contains(existingConnection.Value))
            {
                // 接続先がまだ有効 → 維持. パルス解除.
                _viewState.View.SetBlockPulsing(cluster.Positions, false);
                continue;
            }

            // 既存接続が無効化された場合はクリア.
            _model.ClearGrantConnection(cluster.Positions);

            if (cluster.AdjacentAttacks.Count == 1)
            {
                // 隣接攻撃1つ → 自動接続.
                _model.SetGrantConnection(cluster.Positions, cluster.AdjacentAttacks[0]);
                _viewState.View.SetBlockPulsing(cluster.Positions, false);
                Debug.Log($"PuzzleMapController: 付与塊自動接続 ({cluster.Positions.Count}ブロック → 攻撃{cluster.AdjacentAttacks[0]})");
            }
            else
            {
                // 隣接攻撃0 or 2+ → 未接続. パルス開始.
                _viewState.View.SetBlockPulsing(cluster.Positions, true);
                if (cluster.AdjacentAttacks.Count >= 2)
                {
                    Debug.Log($"PuzzleMapController: 付与塊未接続 ({cluster.Positions.Count}ブロック, 隣接攻撃{cluster.AdjacentAttacks.Count}個 → 編集待ち)");
                }
            }
        }

        // View側の接続マップを更新 (コネクタ表示に反映).
        _viewState.View.SetGrantConnections(_model.GetAllGrantConnections());
    }

    /// <summary> ホバーハイライト更新. カーソル下のブロックが属するミノ全体を拡大表示. </summary>
    private void UpdateHoverHighlight(Vector2 screenPos)
    {
        var worldPos = ScreenToGridWorld(screenPos);
        int gridX = Mathf.RoundToInt(worldPos.x);
        int gridZ = Mathf.RoundToInt(worldPos.z);
        int focusY = _viewState?.Camera != null ? _viewState.Camera.FocusLayer : 0;
        var gridPos = new Vector3Int(gridX, focusY, gridZ);

        // 盤面外 or 空ブロック → 解除.
        if (!_model.IsInBounds(gridPos) || _model.IsEmpty(gridPos))
        {
            ClearHoverHighlight();
            return;
        }

        // 同じミノ内なら何もしない.
        if (_lastHoveredMinoPositions != null && _lastHoveredMinoPositions.Contains(gridPos)) return;

        // 旧ハイライト解除.
        ClearHoverHighlight();

        // この位置のミノを探してミノ全ブロックをハイライト.
        int minoIdx = FindMinoAtGridPos(gridPos);
        if (minoIdx < 0) return;

        var positions = _minoInstances[minoIdx].GetWorldBlockPositions();
        _lastHoveredMinoPositions = positions;
        for (int p = 0; p < positions.Count; p++)
        {
            _viewState.View.SetBlockHighlight(positions[p], true);
        }
    }

    /// <summary> ホバーハイライトを解除. </summary>
    private void ClearHoverHighlight()
    {
        if (_lastHoveredMinoPositions == null) return;
        for (int p = 0; p < _lastHoveredMinoPositions.Count; p++)
        {
            _viewState.View.SetBlockHighlight(_lastHoveredMinoPositions[p], false);
        }
        _lastHoveredMinoPositions = null;
    }

    /// <summary> 指定座標が属する配置済みミノのインデックスを取得. 該当なし=-1. </summary>
    private int FindMinoAtGridPos(Vector3Int gridPos)
    {
        for (int i = 0; i < _minoInstances.Count; i++)
        {
            var mino = _minoInstances[i];
            if (!mino.IsPlaced) continue;
            var positions = mino.GetWorldBlockPositions();
            for (int p = 0; p < positions.Count; p++)
            {
                if (positions[p] == gridPos) return i;
            }
        }
        return -1;
    }

    /// <summary> 付与ブロック左クリック → 編集モード開始を試みる. </summary>
    /// <returns> 編集モードに入った場合 true. </returns>
    private bool TryEnterEditMode(Vector3Int gridPos)
    {
        var block = _model.GetBlock(gridPos);
        if (block == null || block.EffectType != BlockEffectType.Grant) return false;

        // この付与ブロックが属するクラスタを探す.
        var clusters = BlockLinkResolver.FindAllGrantClusters(_model);
        GrantCluster targetCluster = null;
        for (int c = 0; c < clusters.Count; c++)
        {
            if (clusters[c].Positions.Contains(gridPos))
            {
                targetCluster = clusters[c];
                break;
            }
        }
        if (targetCluster == null) return false;

        // 隣接攻撃が2つ以上の場合のみ編集モード開始.
        if (targetCluster.AdjacentAttacks.Count < 2) return false;

        // 編集モード開始.
        _editingCluster = targetCluster;
        _editAttackOptions = targetCluster.AdjacentAttacks;
        _savedFocusLayer = _viewState.Camera != null ? _viewState.Camera.FocusLayer : 0;
        _editModeJustEntered = true;

        // メニュー表示用の攻撃データを収集.
        var attackDataList = new List<BlockData>();
        for (int i = 0; i < _editAttackOptions.Count; i++)
        {
            var data = _model.GetBlock(_editAttackOptions[i]);
            attackDataList.Add(data ?? new BlockData());
        }

        // メニューのアンカー座標 (クラスタの先頭).
        var anchor = targetCluster.Positions[0];
        // メニュー基準ワールド座標を記録 (View側と同じオフセット).
        _editMenuBasePos = new Vector3(anchor.x + 1.5f, anchor.y + 0.5f, anchor.z);

        _viewState.View.ShowGrantLinkMenu(anchor, _editAttackOptions, attackDataList);

        // 最初の項目を自動選択.
        _editMenuIndex = 0;
        SetEditMenuSelection(0);

        Debug.Log($"PuzzleMapController: 編集モード開始 (付与塊{targetCluster.Positions.Count}ブロック, 攻撃候補{_editAttackOptions.Count}個)");
        return true;
    }

    /// <summary> 編集モード中の入力処理. </summary>
    private void UpdateEditMode(Mouse mouse, Keyboard kb)
    {
        if (_editAttackOptions == null || _editAttackOptions.Count == 0)
        {
            ExitEditMode();
            return;
        }

        // 編集モード開始フレームは入力無視 (クリック二重検知防止).
        if (_editModeJustEntered)
        {
            _editModeJustEntered = false;
            return;
        }

        // キーボード: 上下で選択移動.
        if (kb != null)
        {
            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
            {
                int newIndex = _editMenuIndex <= 0 ? _editAttackOptions.Count - 1 : _editMenuIndex - 1;
                SetEditMenuSelection(newIndex);
            }
            if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)
            {
                int newIndex = _editMenuIndex >= _editAttackOptions.Count - 1 ? 0 : _editMenuIndex + 1;
                SetEditMenuSelection(newIndex);
            }

            // Enter / Space: 接続確定.
            if ((kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) && _editMenuIndex >= 0)
            {
                ConfirmEditModeSelection();
                return;
            }

            // Escape: キャンセル.
            if (kb.escapeKey.wasPressedThisFrame)
            {
                ExitEditMode();
                return;
            }
        }

        // マウス: ホバーでメニュー選択 + 左クリック確定/キャンセル.
        if (mouse != null && _camera != null)
        {
            var mouseScreenPos = mouse.position.ReadValue();
            var mouseWorld = ScreenToGridWorld(mouseScreenPos);

            // メニュー項目のホバー判定.
            int hoveredIndex = FindEditMenuItemAtWorldPos(mouseWorld);
            if (hoveredIndex >= 0 && hoveredIndex != _editMenuIndex)
            {
                SetEditMenuSelection(hoveredIndex);
            }

            // 左クリック.
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (hoveredIndex >= 0)
                {
                    // メニュー項目上 → 確定.
                    SetEditMenuSelection(hoveredIndex);
                    ConfirmEditModeSelection();
                }
                else
                {
                    // メニュー外 → キャンセル.
                    ExitEditMode();
                }
                return;
            }

            // 右クリック: キャンセル.
            if (mouse.rightButton.wasPressedThisFrame)
            {
                ExitEditMode();
                return;
            }
        }
    }

    /// <summary> ワールド座標からメニュー項目インデックスを取得. 該当なし=-1. </summary>
    private int FindEditMenuItemAtWorldPos(Vector3 worldPos)
    {
        for (int i = 0; i < _editAttackOptions.Count; i++)
        {
            // メニュー項目のワールド座標 (View側と同じ配置).
            var itemPos = _editMenuBasePos + new Vector3(0f, 0f, -i * EditMenuItemSpacing);
            float distX = Mathf.Abs(worldPos.x - itemPos.x);
            float distZ = Mathf.Abs(worldPos.z - itemPos.z);
            if (distX <= EditMenuHitRadius * 2f && distZ <= EditMenuHitRadius)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary> 編集メニューの選択を変更. </summary>
    private void SetEditMenuSelection(int index)
    {
        _editMenuIndex = index;
        _viewState.View.SetGrantLinkMenuSelection(index);

        // 選択中攻撃の階層にカメラを一時変更.
        if (index >= 0 && index < _editAttackOptions.Count && _viewState.Camera != null)
        {
            int attackLayer = _editAttackOptions[index].y;
            if (_viewState.Camera.FocusLayer != attackLayer)
            {
                _viewState.Camera.FocusLayer = attackLayer;
            }
        }
    }

    /// <summary> 編集モードの選択を確定. </summary>
    private void ConfirmEditModeSelection()
    {
        if (_editingCluster == null || _editMenuIndex < 0 || _editMenuIndex >= _editAttackOptions.Count) return;

        var selectedAttack = _editAttackOptions[_editMenuIndex];
        _model.SetGrantConnection(_editingCluster.Positions, selectedAttack);
        _viewState.View.SetBlockPulsing(_editingCluster.Positions, false);

        // View側の接続マップを更新 (コネクタ表示に反映).
        _viewState.View.SetGrantConnections(_model.GetAllGrantConnections());

        Debug.Log($"PuzzleMapController: 接続確定 (付与塊 → 攻撃{selectedAttack})");

        ExitEditMode();
    }

    /// <summary> 編集モード終了. </summary>
    private void ExitEditMode()
    {
        _viewState.View.HideGrantLinkMenu();

        // カメラFocusLayer復帰.
        if (_viewState.Camera != null)
        {
            _viewState.Camera.FocusLayer = _savedFocusLayer;
        }

        _editingCluster = null;
        _editAttackOptions = null;
        _editMenuIndex = -1;

        Debug.Log("PuzzleMapController: 編集モード終了.");
    }

    #endregion

    #region 入力コールバック

    private void OnRotateLeft()
    {
        if (_activeMino == null) return;
        _activeMino.RotateLeft();
        UpdateMinoPreview();
    }

    private void OnRotateRight()
    {
        if (_activeMino == null) return;
        _activeMino.RotateRight();
        UpdateMinoPreview();
    }

    private void OnLayerUp()
    {
        if (_viewState?.Camera == null) return;
        _viewState.Camera.FocusLayer++;
        if (_activeMino != null)
        {
            var pos = _activeMino.Position;
            pos.y = _viewState.Camera.FocusLayer;
            _activeMino.Position = pos;
            UpdateMinoPreview();
        }
    }

    private void OnLayerDown()
    {
        if (_viewState?.Camera == null) return;
        _viewState.Camera.FocusLayer--;
        if (_activeMino != null)
        {
            var pos = _activeMino.Position;
            pos.y = _viewState.Camera.FocusLayer;
            _activeMino.Position = pos;
            UpdateMinoPreview();
        }
    }

    #endregion

    /// <summary> テスト用フォールバックミノ形状 (L/T/I). </summary>
    private static List<MinoShapeData> CreateFallbackMinoShapes()
    {
        return new List<MinoShapeData>
        {
            CreateShape("test_L", new Vector3Int[]
            {
                new(0, 0, 0), new(1, 0, 0), new(2, 0, 0), new(2, 0, 1)
            }),
            CreateShape("test_T", new Vector3Int[]
            {
                new(0, 0, 0), new(1, 0, 0), new(2, 0, 0), new(1, 0, 1)
            }),
            CreateShape("test_I", new Vector3Int[]
            {
                new(0, 0, 0), new(1, 0, 0), new(2, 0, 0), new(3, 0, 0)
            }),
            CreateShape("test_S", new Vector3Int[]
            {
                new(0, 0, 0), new(1, 0, 0), new(1, 0, 1), new(2, 0, 1)
            }),
            CreateShape("test_O", new Vector3Int[]
            {
                new(0, 0, 0), new(1, 0, 0), new(0, 0, 1), new(1, 0, 1)
            })
        };
    }

    private static MinoShapeData CreateShape(string id, Vector3Int[] offsets)
    {
        var shape = new MinoShapeData { ShapeId = id };
        for (int i = 0; i < offsets.Length; i++)
        {
            shape.BlockOffsets.Add(offsets[i]);
            shape.BlockEffects.Add(new BlockData(BlockEffectType.Attack, "test", 1f));
        }
        return shape;
    }

    /// <summary> 破棄. </summary>
    public void Dispose()
    {
        _inputActions.PuzzleMap.Disable();
        _inputActions.Dispose();
        _viewState?.Exit();
    }
}
