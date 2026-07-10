using UnityEngine;

/// <summary> 2Dパズルマップ用カメラ制御 (スタブ). </summary>
/// <remarks> Orthographicカメラ. 透明化処理不要. 層切り替えで表示制御. </remarks>
public class PuzzleCameraController2D : IPuzzleCameraController
{
    private Camera _camera;
    private Vector3Int _mapSize;
    private IPuzzleMapView _view;

    private int _focusLayer;
    public int FocusLayer
    {
        get => _focusLayer;
        set
        {
            _focusLayer = Mathf.Clamp(value, 0, _mapSize.y - 1);
            UpdateLayerVisibility();
        }
    }

    public PuzzleCameraController2D(IPuzzleMapView view)
    {
        _view = view;
    }

    public void Initialize(Camera camera, Vector3Int mapSize)
    {
        _camera = camera;
        _mapSize = mapSize;
        _focusLayer = 0;

        // Orthographicカメラに設定.
        if (_camera != null)
        {
            _camera.orthographic = true;
            _camera.orthographicSize = Mathf.Max(mapSize.x, mapSize.z) * 0.6f;
            _camera.transform.position = new Vector3(mapSize.x * 0.5f, 10f, mapSize.z * 0.5f);
            _camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    public void Update()
    {
        // 2Dカメラは毎フレームの透明化処理不要.
    }

    /// <summary> 層ごとの表示/非表示を更新. フォーカス層のみ表示. </summary>
    private void UpdateLayerVisibility()
    {
        if (_view == null) return;

        _view.ResetAllLayerAlpha();

        for (int y = 0; y < _mapSize.y; y++)
        {
            if (y != _focusLayer)
            {
                _view.SetLayerAlpha(y, 0f);
            }
        }
    }

    public void Dispose()
    {
    }
}
