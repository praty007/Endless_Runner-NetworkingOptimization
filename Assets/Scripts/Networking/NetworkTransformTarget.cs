using UnityEngine;
using System;

public class NetworkTransformTarget : MonoBehaviour
{
    [SerializeField] GameObject SourceGameObject;
    
    private Transform cachedTransform;
    private Vector3 positionOffset;
    private bool hasInitialTransform = false;
    private float lastUpdateTime;
    private float extrapolationTimeLimit => NetworkManager.Instance.TickRate * 1.6f; // Maximum time to extrapolate
    private float lerpSpeed = 10f; // Speed of interpolation
    private Vector3 targetPosition;
    private Vector3 currentVelocity;
    private Vector3 targetVelocity;
    private float velocityLerpSpeed = 5f; // Speed of velocity interpolation

     // Dynamic threshold: decreases as speed increases
        float baseThreshold = 45f; // Base angle threshold in degrees
        float minThreshold = 10f; // Minimum threshold at high speeds
        float speedFactor = 5f; // How quickly threshold decreases with speed
        

    void Start()
    {
        cachedTransform = transform;
        lastUpdateTime = Time.time;
        NetworkManager.Instance.RegisterTransformTarget(SourceGameObject.GetInstanceID(), this);
    }

    void FixedUpdate()
    {        
        if (hasInitialTransform)
        {            
           SetTransformData();
        }
    }

    void SetTransformData()
    {
        float timeSinceUpdate = Time.time - lastUpdateTime;
            if (timeSinceUpdate > 0 && timeSinceUpdate <= extrapolationTimeLimit)
            {                
                if(checkForMajorDirectionChange(currentVelocity, targetVelocity))
                    currentVelocity = targetVelocity;
                else
                    currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.fixedDeltaTime * velocityLerpSpeed);
                cachedTransform.position = Vector3.MoveTowards(cachedTransform.position, targetPosition, currentVelocity.magnitude*Time.fixedDeltaTime )        ;        // Update position using interpolated velocity
                //cachedTransform.position += currentVelocity * Time.fixedDeltaTime;

            }   
    } 

    bool checkForMajorDirectionChange(Vector3 a, Vector3 b){
        if (a.magnitude < 0.001f || b.magnitude < 0.001f) return false; // Ignore very small velocities
        
        float angle = Vector3.Angle(a, b);
        float speed = a.magnitude;

        float dynamicThreshold = Mathf.Max(minThreshold, baseThreshold - (speed * speedFactor));
        
        return angle > dynamicThreshold;
    }
    public void ReceiveTransformData(byte[] transformData)
    {        
        float currentTime = Time.time;
        float deltaTime = currentTime - lastUpdateTime;
        lastUpdateTime = currentTime;

        // Extract position
        float posX = BitConverter.ToSingle(transformData, 0);
        float posY = BitConverter.ToSingle(transformData, 4);
        float posZ = BitConverter.ToSingle(transformData, 8);
        Vector3 sourcePosition = new Vector3(posX, posY, posZ);

        if (!hasInitialTransform)
        {            
            // Store initial offsets
            positionOffset = cachedTransform.position - sourcePosition;
            hasInitialTransform = true;
            currentVelocity = Vector3.zero;
            targetVelocity = Vector3.zero;
        }

        // Set target transforms with offsets
        Vector3 newTargetPosition = sourcePosition + positionOffset;
        
        // Calculate target velocity based on position change
        if (deltaTime > 0)
        {
            targetVelocity = (newTargetPosition - targetPosition) / deltaTime;
        }
        
        targetPosition = newTargetPosition;
    }
}