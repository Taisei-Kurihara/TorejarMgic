using UnityEngine;

/// <summary> 2D描画モードのStateクラス. View + Camera + 2D固有操作を統合. </summary>
public class PuzzleViewState2D : IPuzzleViewState
{
    private PuzzleMapView2D _view;
    private PuzzleCameraController2D _cameraController;

    public IPuzzleMapView View => _view;
    public IPuzzleCameraController Camera => _cameraController;

    public void Enter(Vector3Int mapSize, Camera camera, Transform viewRoot)
    {
        _view = new PuzzleMapView2D();
        _cameraController = new PuzzleCameraController2D(_view);

        _view.Initialize(mapSize);
        _cameraController.Initialize(camera, mapSize);

        Debug.Log("PuzzleViewState2D: Enter");
    }

    public void Exit()
    {
        _view?.Dispose();
        _cameraController?.Dispose();
        _view = null;
        _cameraController = null;

        Debug.Log("PuzzleViewState2D: Exit");
    }

    public void Update()
    {
        _cameraController?.Update();
    }
}
