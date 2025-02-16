using UnityEngine;
using System.Collections.Generic;
using System;

using Utils;

public class NetworkTransform : MonoBehaviour
{
    
    public static Vector3 PositionOffset {get; private set;}
        [SerializeField] private bool isOwner;
    [SerializeField, Tooltip("Only needed for non-owner objects")] private GameObject targetObject; // Only needed for non-owner objects

    // Cached references
    private Transform cachedTransform;

    // Source (Owner) variables
    private Vector3 lastPosition;
    private Vector3 lastVelocity;
    private float nextSendTime = 0f;

    // Target (Non-owner) variables
    private Vector3 positionOffset;
    private bool hasInitialTransform;
    private float lastUpdateTime;
    private Vector3 targetPosition;
    private Vector3 currentVelocity;
    private Vector3 targetVelocity;
    private Queue<(Vector3 position, float time)> positionHistory = new Queue<(Vector3 position, float time)>();
    private const int MAX_HISTORY_POINTS = 5;

    // Shared network settings
    private float baseSendRate => NetworkManager.Instance.TickRate;
    private float velocityLerpSpeed = 6f;

    // Dynamic threshold settings for target interpolation
    private float baseThreshold = 45f;
    private float minThreshold = 10f;
    private float speedFactor = 5f;
    private ExtrapolationType extrapolationType = ExtrapolationType.SimpleVelocity;
    void Start()
    {
        cachedTransform = transform;
        lastPosition = cachedTransform.position;
        lastVelocity = Vector3.zero;
        if (!isOwner)
        {
            NetworkManager.Instance.RegisterTransformTarget(targetObject.GetInstanceID(), this);
            lastUpdateTime = Time.time;
        }
    }

    void FixedUpdate()
    {
        if (isOwner)
            ProcessSourceTransformData();

        else if (hasInitialTransform)
            SetTransformData();
    }

    #region Owner
    private void ProcessSourceTransformData()
    {
        UIManager.Instance.UpdateNextTick(nextSendTime-Time.time);
        if (Time.time < nextSendTime) return;

        Vector3 currentPosition = cachedTransform.position;
        bool positionChanged = currentPosition != lastPosition;
        if (!positionChanged) return;

        SendTransformData();
        nextSendTime = Time.time + baseSendRate;

    }
    private void SendTransformData()
    {
        byte[] transformData = Utils.TransformDataSerializer.SerializeTransformPosition(cachedTransform.position);
        lastPosition = cachedTransform.position;
        NetworkManager.Instance.RelayTransformData(transformData, gameObject.GetInstanceID());
    }

    #endregion

    #region Non Owner

    private const int MIN_POINTS = 3; // Minimum points needed for quadratic Bezier

    public void ReceiveTransformData(byte[] transformData)
    {
        if (isOwner) return;

        float currentTime = Time.time;
        float deltaTime = currentTime - lastUpdateTime;
        lastUpdateTime = currentTime;

        Vector3 sourcePosition = Utils.TransformDataSerializer.DeserializeTransformPosition(transformData);

        if (!hasInitialTransform)
        {
            positionOffset = cachedTransform.position - sourcePosition;
            hasInitialTransform = true;
            currentVelocity = Vector3.zero;
            targetVelocity = Vector3.zero;
            PositionOffset = positionOffset;
        }
        Vector3 newTargetPosition = sourcePosition + positionOffset;

        if (deltaTime > 0)
        {
            targetVelocity = (newTargetPosition - transform.position) / deltaTime;
        }

        // Update position history
        positionHistory.Enqueue((newTargetPosition, currentTime));
        while (positionHistory.Count > MAX_HISTORY_POINTS)
        {
            positionHistory.Dequeue();
        }

        targetPosition = newTargetPosition;
    }

    public void SetExtrapolationType(ExtrapolationType type)
    {
        extrapolationType = type;
    }
    private void SetTransformData()
    {
        float timeSinceLastUpdate = Time.time - lastUpdateTime;
        Vector3 extrapolatedPosition = ExtrapolationAlgorithms.ExtrapolatePosition(
            positionHistory,
            currentVelocity,
            Time.time,
            lastUpdateTime,
            extrapolationType
        );

        if (CheckForMajorDirectionChange(currentVelocity, targetVelocity))
            currentVelocity = targetVelocity;
        else
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.fixedDeltaTime * velocityLerpSpeed);

        cachedTransform.position = Vector3.MoveTowards(cachedTransform.position, extrapolatedPosition, currentVelocity.magnitude * Time.fixedDeltaTime);
    }

    private bool CheckForMajorDirectionChange(Vector3 a, Vector3 b)
    {
        if (a.magnitude < 0.001f || b.magnitude < 0.001f) return false;

        float angle = Vector3.Angle(a, b);
        float speed = a.magnitude;
        float dynamicThreshold = Mathf.Max(minThreshold, baseThreshold - (speed * speedFactor));

        return angle > dynamicThreshold;
    }

    #endregion
}