
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;

    [SerializeField] private SpriteRenderer arenaFrame;

    private Vector3 offset;
    private Camera sceneCamera;

    void Start()
    {
        sceneCamera = GetComponent<Camera>();
        if (target != null)
        {
            offset = transform.position - target.position;
        }
    }

    void LateUpdate()
    {
        if (arenaFrame != null && sceneCamera != null && sceneCamera.orthographic)
        {
            Bounds bounds = arenaFrame.bounds;
            sceneCamera.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.x / sceneCamera.aspect);
            transform.position = new Vector3(bounds.center.x, bounds.center.y, transform.position.z);
            return;
        }

        if (target == null) return;

        Vector3 targetPosition = target.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}
