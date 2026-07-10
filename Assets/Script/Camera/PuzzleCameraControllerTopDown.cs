using UnityEngine;

/// <summary> パズルフェーズ用カメラ. 固定トップダウン + レイヤー透過. </summary>
public class PuzzleCameraControllerTopDown : IPuzzleCameraController
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
            UpdateLayerTransparency();
            _view?.SetFocusLayerBoard(_focusLayer);
        }
    }

    public PuzzleCameraControllerTopDown(IPuzzleMapView view)
    {
        _view = view;
    }

    public void Initialize(Camera camera, Vector3Int mapSize)
    {
        _camera = camera;
        _mapSize = mapSize;
        _focusLayer = 0;

        _camera.orthographic = true;
        _camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // 盤面 + 右側ミノ一覧 (3列 × 2.5f + 余白) が画面に収まるよう調整.
        float aspect = _camera.aspect;
        if (aspect <= 1f) aspect = 16f / 9f; // フォールバック.

        float gridW = mapSize.x;   // 5.
        float gridH = mapSize.z;   // 5.
        float leftMargin = 2f;     // 左端の余白 (操作ガイド分).
        float minoListWidth = 3 * 2.5f; // 3列 × 2.5f.
        float totalW = leftMargin + gridW + 2f + minoListWidth; // 余白 + 盤面 + 間隔 + ミノ一覧.
        float orthoSize = Mathf.Max(totalW / (2f * aspect), gridH * 0.6f);

        _camera.orthographicSize = orthoSize;
        _camera.transform.position = new Vector3(
            totalW * 0.5f - leftMargin, // 盤面が左マージン分右に寄る.
            mapSize.y + 10f,
            gridH * 0.5f                // 盤面を上下中央に.
        );

        UpdateLayerTransparency();
    }

    public void Update()
    {
        // 固定カメラのためフレーム更新不要.
    }

    private void UpdateLayerTransparency()
    {
        if (_view == null) return;

        _view.ResetAllLayerAlpha();

        // フォーカス層より上を半透明に.
        for (int y = 0; y < _mapSize.y; y++)
        {
            if (y > _focusLayer)
            {
                _view.SetLayerAlpha(y, 0.1f);
            }
        }
    }

    public void Dispose()
    {
        _view = null;
    }
}
