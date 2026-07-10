using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary> 3D描画モードのStateクラス. View + Camera + 3D固有操作を統合. </summary>
public class PuzzleViewState3D : IPuzzleViewState
{
    private PuzzleMapView3D _view;
    private IPuzzleCameraController _cameraController;
    private readonly Func<IPuzzleMapView, IPuzzleCameraController> _cameraFactory;

    public IPuzzleMapView View => _view;
    public IPuzzleCameraController Camera => _cameraController;

    /// <summary> デフォルト: PuzzleCameraController3D を使用. </summary>
    public PuzzleViewState3D() : this(null) { }

    /// <summary> カメラ制御を外部から注入. nullならデフォルト(PuzzleCameraController3D). </summary>
    public PuzzleViewState3D(Func<IPuzzleMapView, IPuzzleCameraController> cameraFactory)
    {
        _cameraFactory = cameraFactory;
    }

    public void Enter(Vector3Int mapSize, Camera camera, Transform viewRoot)
    {
        _view = new PuzzleMapView3D(viewRoot);

        // ファクトリがあればそれを使用、なければ従来の3Dカメラ.
        _cameraController = _cameraFactory != null
            ? _cameraFactory(_view)
            : new PuzzleCameraController3D(_view);

        _view.Initialize(mapSize);
        _cameraController.Initialize(camera, mapSize);

        Debug.Log("PuzzleViewState3D: Enter");
    }

    public void Exit()
    {
        _view?.Dispose();
        _cameraController?.Dispose();
        _view = null;
        _cameraController = null;

        Debug.Log("PuzzleViewState3D: Exit");
    }

    public void Update()
    {
        _cameraController?.Update();

        // 右クリックドラッグでカメラ回転 (球面座標カメラの場合のみ).
        if (_cameraController is PuzzleCameraController3D orbiting)
        {
            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                var mouseDelta = Mouse.current.delta.ReadValue();
                orbiting.RotateByInput(mouseDelta.x, mouseDelta.y);
            }
        }
    }
}
