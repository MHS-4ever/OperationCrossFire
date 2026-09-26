using UnityEngine;

public class CameraCoverBackground : MonoBehaviour
{
    [SerializeField] Camera _camera;
    [SerializeField] SpriteRenderer _background;

    int _lastPixelWidth = -1;
    int _lastPixelHeight = -1;
    float _lastOrthographicSize = -1f;
    float _lastAspect = -1f;
    float _preservedWorldZ;

    void Awake()
    {
        if (_background == null)
        {
            _background = GetComponent<SpriteRenderer>();
        }

        if (_camera == null)
        {
            Debug.LogError($"{nameof(CameraCoverBackground)} requires the gameplay {nameof(Camera)} reference.", this);
        }

        if (_background == null)
        {
            Debug.LogError($"{nameof(CameraCoverBackground)} requires the Background {nameof(SpriteRenderer)}.", this);
        }

        _preservedWorldZ = transform.position.z;
        ApplyCover(force: true);
    }

    void OnEnable()
    {
        ApplyCover(force: true);
    }

    void LateUpdate()
    {
        ApplyCover(force: false);
    }

    void ApplyCover(bool force)
    {
        if (_camera == null || _background == null || _background.sprite == null)
        {
            return;
        }

        int pixelWidth = _camera.pixelWidth;
        int pixelHeight = _camera.pixelHeight;
        float orthographicSize = _camera.orthographicSize;
        float aspect = _camera.aspect;

        if (!force
            && pixelWidth == _lastPixelWidth
            && pixelHeight == _lastPixelHeight
            && Mathf.Approximately(orthographicSize, _lastOrthographicSize)
            && Mathf.Approximately(aspect, _lastAspect))
        {
            return;
        }

        _lastPixelWidth = pixelWidth;
        _lastPixelHeight = pixelHeight;
        _lastOrthographicSize = orthographicSize;
        _lastAspect = aspect;

        Vector3 cameraPosition = _camera.transform.position;
        transform.position = new Vector3(cameraPosition.x, cameraPosition.y, _preservedWorldZ);

        Vector2 spriteSize = _background.sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return;
        }

        float viewHeight = _camera.orthographic ? orthographicSize * 2f : 0f;
        if (!_camera.orthographic)
        {
            float distance = Mathf.Abs(_preservedWorldZ - cameraPosition.z);
            viewHeight = 2f * distance * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }

        float viewWidth = viewHeight * aspect;
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        float parentX = Mathf.Abs(parentScale.x) > 0.0001f ? Mathf.Abs(parentScale.x) : 1f;
        float parentY = Mathf.Abs(parentScale.y) > 0.0001f ? Mathf.Abs(parentScale.y) : 1f;

        float scaleX = viewWidth / (spriteSize.x * parentX);
        float scaleY = viewHeight / (spriteSize.y * parentY);
        float scale = Mathf.Max(scaleX, scaleY);
        Vector3 localScale = transform.localScale;
        transform.localScale = new Vector3(scale, scale, localScale.z);
    }
}
