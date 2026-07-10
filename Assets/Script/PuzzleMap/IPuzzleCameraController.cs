using UnityEngine;

/// <summary> パズルマップ用カメラ制御のインターフェース. 2D/3D共通. </summary>
public interface IPuzzleCameraController
{
    /// <summary> カメラ初期化. </summary>
    void Initialize(Camera camera, Vector3Int mapSize);

    /// <summary> 毎フレーム更新. 透明化処理等. </summary>
    void Update();

    /// <summary> 現在注目しているy層. </summary>
    int FocusLayer { get; set; }

    /// <summary> 破棄. </summary>
    void Dispose();
}
