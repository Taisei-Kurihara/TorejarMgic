using System.Collections.Generic;
using UnityEngine;

/// <summary> パズルマップ描画のインターフェース. 3Dオブジェ/2DUI共通. </summary>
public interface IPuzzleMapView
{
    /// <summary> マップの初期表示を生成. </summary>
    void Initialize(Vector3Int mapSize);

    /// <summary> 指定座標にブロックを表示. </summary>
    void ShowBlock(Vector3Int gridPos, BlockData data);

    /// <summary> 指定座標のブロックを非表示. </summary>
    void HideBlock(Vector3Int gridPos);

    /// <summary> ミノのプレビュー(ゴースト)を表示. blockEffectsで各ブロックの効果色を反映. </summary>
    void ShowMinoPreview(List<Vector3Int> positions, bool canPlace, List<BlockData> blockEffects);

    /// <summary> ミノプレビューを非表示. </summary>
    void HideMinoPreview();

    /// <summary> ミノ一覧をmap右側に表示. </summary>
    /// <param name="placedFlags"> 各ミノが配置済みかどうか. nullなら全て未配置扱い. </param>
    void ShowMinoList(List<MinoShapeData> shapes, int selectedIndex, List<bool> placedFlags = null);

    /// <summary> 指定y層のブロック透明度を設定 (0=透明, 1=不透明). </summary>
    void SetLayerAlpha(int y, float alpha);

    /// <summary> 全レイヤーの透明度をリセット. </summary>
    void ResetAllLayerAlpha();

    /// <summary> パルスアニメーション対象を設定/解除 (未接続付与塊). </summary>
    void SetBlockPulsing(List<Vector3Int> positions, bool pulsing);

    /// <summary> ホバーハイライト (拡大固定). </summary>
    void SetBlockHighlight(Vector3Int pos, bool highlight);

    /// <summary> 付与→攻撃 接続編集メニューを表示. </summary>
    void ShowGrantLinkMenu(Vector3Int menuAnchor, List<Vector3Int> attackPositions, List<BlockData> attackData);

    /// <summary> 接続編集メニューを非表示. </summary>
    void HideGrantLinkMenu();

    /// <summary> 接続編集メニューの選択インデックスを更新. </summary>
    void SetGrantLinkMenuSelection(int index);

    /// <summary> ミノ一覧のスクロールオフセットを設定. </summary>
    void SetMinoListScroll(float scrollOffset);

    /// <summary> フォーカス層の盤面表示を更新 (半透明盤面の移動先). </summary>
    void SetFocusLayerBoard(int y);

    /// <summary> 付与→攻撃 接続マップを設定 (視覚的コネクタ更新用). </summary>
    void SetGrantConnections(Dictionary<Vector3Int, Vector3Int> connections);

    /// <summary> 毎フレームのアニメーション更新. </summary>
    void UpdateAnimations(float deltaTime);

    /// <summary> 破棄. </summary>
    void Dispose();
}
