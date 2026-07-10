using UnityEngine;

/// <summary> パズルマップの描画モード(2D/3D)をStateクラスとして管理するインターフェース. </summary>
public interface IPuzzleViewState
{
    /// <summary> View. </summary>
    IPuzzleMapView View { get; }

    /// <summary> カメラ制御. </summary>
    IPuzzleCameraController Camera { get; }

    /// <summary> この描画モードに入った時の初期化. </summary>
    void Enter(Vector3Int mapSize, Camera camera, Transform viewRoot);

    /// <summary> この描画モードから出る時の後処理. </summary>
    void Exit();

    /// <summary> 毎フレーム更新 (カメラ操作等の描画モード固有処理). </summary>
    void Update();
}
