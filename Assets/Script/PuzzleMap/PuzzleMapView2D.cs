using System.Collections.Generic;
using UnityEngine;

/// <summary> パズルマップの2D UI描画実装 (スタブ). </summary>
/// <remarks> 3Dオブジェから2DUIに切り替わる可能性があるため用意. </remarks>
public class PuzzleMapView2D : IPuzzleMapView
{
    private Vector3Int _mapSize;
    private int _focusLayer;

    public void Initialize(Vector3Int mapSize)
    {
        _mapSize = mapSize;
        _focusLayer = 0;
        Debug.Log($"PuzzleMapView2D: Initialize ({mapSize})");
    }

    public void ShowBlock(Vector3Int gridPos, BlockData data)
    {
        // 2D UI上にブロック表示. RectTransform + Image で描画.
        // フォーカス層のみ表示 or 全層をタブ切り替え.
    }

    public void HideBlock(Vector3Int gridPos)
    {
    }

    public void ShowMinoPreview(List<Vector3Int> positions, bool canPlace, List<BlockData> blockEffects)
    {
    }

    public void HideMinoPreview()
    {
    }

    public void ShowMinoList(List<MinoShapeData> shapes, int selectedIndex, List<bool> placedFlags = null)
    {
    }

    public void SetLayerAlpha(int y, float alpha)
    {
        // 2Dの場合は層ごとの表示/非表示で切り替え.
    }

    public void ResetAllLayerAlpha()
    {
    }

    public void SetBlockPulsing(List<Vector3Int> positions, bool pulsing)
    {
    }

    public void SetBlockHighlight(Vector3Int pos, bool highlight)
    {
    }

    public void ShowGrantLinkMenu(Vector3Int menuAnchor, List<Vector3Int> attackPositions, List<BlockData> attackData)
    {
    }

    public void HideGrantLinkMenu()
    {
    }

    public void SetGrantLinkMenuSelection(int index)
    {
    }

    public void SetMinoListScroll(float scrollOffset)
    {
    }

    public void SetFocusLayerBoard(int y)
    {
    }

    public void SetGrantConnections(Dictionary<Vector3Int, Vector3Int> connections)
    {
    }

    public void UpdateAnimations(float deltaTime)
    {
    }

    public void Dispose()
    {
    }
}
