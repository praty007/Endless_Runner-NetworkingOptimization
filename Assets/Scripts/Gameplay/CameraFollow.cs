using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 2, -5);
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private bool lookAtTarget = true;

    private void FixedUpdate()
    {
        if (target == null) return;

        // Calculate desired position
        Vector3 desiredPosition = target.position + offset;
        
        // Smoothly move the camera towards desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.fixedDeltaTime);
        transform.position = smoothedPosition;

        // Optionally make the camera look at the target
        if (lookAtTarget)
        {
            transform.LookAt(target);
        }
    }
}