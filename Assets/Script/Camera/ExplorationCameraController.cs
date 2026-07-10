using UnityEngine;

/// <summary> 探索フェーズ用カメラ. プレイヤーの少し後ろ上方から見下ろす追尾カメラ. </summary>
public class ExplorationCameraController
{
    private Camera _camera;
    private Transform _target;
    private bool _enabled = true;

    private float _height = 5f;
    private float _backOffset = -5f;
    private float _smoothSpeed = 5f;

    private static readonly Quaternion CameraRotation = Quaternion.Euler(45f, 0f, 0f);

    public void Initialize(Camera camera, Transform playerTransform)
    {
        _camera = camera;
        _target = playerTransform;

        _camera.orthographic = false;
        _camera.transform.rotation = CameraRotation;

        UpdatePosition(snap: true);
    }

    /// <summary> 追尾更新の有効/無効を切り替え. カメラ描画自体は維持 (背景として使用). </summary>
    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
    }

    public void Update()
    {
        if (!_enabled) return;
        UpdatePosition(snap: false);
    }

    private void UpdatePosition(bool snap)
    {
        if (_camera == null || _target == null) return;

        var targetPos = _target.position + new Vector3(0f, _height, _backOffset);

        if (snap)
        {
            _camera.transform.position = targetPos;
        }
        else
        {
            _camera.transform.position = Vector3.Lerp(
                _camera.transform.position,
                targetPos,
                _smoothSpeed * Time.deltaTime
            );
        }
    }

    public void Dispose()
    {
        _camera = null;
        _target = null;
    }
}
