using System.Collections.Generic;
using UnityEngine;

/// <summary> 3Dパズルマップ用カメラ制御. 角度ベース透明化 + Raycast遮蔽. </summary>
/// <remarks> 3DTetoris の GridManager を参考. </remarks>
public class PuzzleCameraController3D : IPuzzleCameraController
{
    private Camera _camera;
    private Vector3Int _mapSize;
    private Vector3 _mapCenter;
    private IPuzzleMapView _view;

    // 球面座標.
    private float _yaw = 45f;
    private float _pitch = 30f;
    private float _distance = 15f;

    private const float PitchMin = 5f;
    private const float PitchMax = 85f;
    private const float Sensitivity = 0.15f;

    // 層透過.
    private int _focusLayer;
    public int FocusLayer
    {
        get => _focusLayer;
        set
        {
            _focusLayer = Mathf.Clamp(value, 0, _mapSize.y - 1);
            UpdateLayerTransparency();
        }
    }

    // Raycast透明化管理.
    private readonly HashSet<Renderer> _transparentRenderers = new HashSet<Renderer>();
    private readonly RaycastHit[] _raycastBuffer = new RaycastHit[32];

    public PuzzleCameraController3D(IPuzzleMapView view)
    {
        _view = view;
    }

    public void Initialize(Camera camera, Vector3Int mapSize)
    {
        _camera = camera;
        _mapSize = mapSize;
        _mapCenter = new Vector3(mapSize.x * 0.5f, mapSize.y * 0.5f, mapSize.z * 0.5f);
        _focusLayer = 0;

        UpdateCameraPosition();
    }

    public void Update()
    {
        UpdateCameraPosition();
        UpdateRaycastTransparency();
    }

    /// <summary> マウスドラッグでカメラ回転 (右クリック). </summary>
    public void RotateByInput(float deltaX, float deltaY)
    {
        _yaw += deltaX * Sensitivity;
        _pitch = Mathf.Clamp(_pitch - deltaY * Sensitivity, PitchMin, PitchMax);
    }

    /// <summary> 球面座標からカメラ位置を更新. </summary>
    private void UpdateCameraPosition()
    {
        if (_camera == null) return;

        float yawRad = _yaw * Mathf.Deg2Rad;
        float pitchRad = _pitch * Mathf.Deg2Rad;

        var offset = new Vector3(
            _distance * Mathf.Cos(pitchRad) * Mathf.Sin(yawRad),
            _distance * Mathf.Sin(pitchRad),
            _distance * Mathf.Cos(pitchRad) * Mathf.Cos(yawRad)
        );

        _camera.transform.position = _mapCenter + offset;
        _camera.transform.LookAt(_mapCenter);
    }

    /// <summary> 層ごとの透明度を更新. フォーカス層より上を透明化. </summary>
    private void UpdateLayerTransparency()
    {
        if (_view == null) return;

        _view.ResetAllLayerAlpha();

        for (int y = 0; y < _mapSize.y; y++)
        {
            if (y > _focusLayer)
            {
                // フォーカス層より上は透明化.
                float alpha = 0.1f;
                _view.SetLayerAlpha(y, alpha);
            }
        }
    }

    /// <summary> カメラからブロックへのRaycast遮蔽による透明化. 3DTetoris参考. </summary>
    private void UpdateRaycastTransparency()
    {
        if (_camera == null) return;

        // 前フレームの透明化をリセット.
        foreach (var renderer in _transparentRenderers)
        {
            if (renderer != null)
            {
                RestoreRendererAlpha(renderer);
            }
        }
        _transparentRenderers.Clear();

        // カメラからマップ中心方向にRaycast. 遮蔽物を透明化.
        var direction = (_mapCenter - _camera.transform.position).normalized;
        var distance = Vector3.Distance(_camera.transform.position, _mapCenter) + _mapSize.magnitude;

        int hitCount = Physics.RaycastNonAlloc(
            _camera.transform.position,
            direction,
            _raycastBuffer,
            distance
        );

        for (int i = 0; i < hitCount; i++)
        {
            var renderer = _raycastBuffer[i].collider.GetComponent<Renderer>();
            if (renderer != null && !_transparentRenderers.Contains(renderer))
            {
                SetRendererAlpha(renderer, 0.15f);
                _transparentRenderers.Add(renderer);
            }
        }
    }

    private void SetRendererAlpha(Renderer renderer, float alpha)
    {
        var mat = renderer.material;
        var color = mat.color;
        color.a = alpha;
        mat.color = color;
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.renderQueue = 3000;
    }

    private void RestoreRendererAlpha(Renderer renderer)
    {
        var mat = renderer.material;
        var color = mat.color;
        color.a = 1f;
        mat.color = color;
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetFloat("_ZWrite", 1f);
        mat.renderQueue = -1;
    }

    public void Dispose()
    {
        _transparentRenderers.Clear();
    }
}
