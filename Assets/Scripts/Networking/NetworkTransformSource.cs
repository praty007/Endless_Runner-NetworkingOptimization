using UnityEngine;
using System;

public class NetworkTransformSource : MonoBehaviour
{
    private Transform cachedTransform;
    private Vector3 lastPosition;
    float sendRate => NetworkManager.Instance.TickRate ;
    private float nextSendTime;

    void Start()
    {
        cachedTransform = transform;
        lastPosition = cachedTransform.position;
    }

    void Update()
    {
        if (Time.time >= nextSendTime)
        {
            if (HasTransformChanged())
            {
                SendTransformData();
            }
            nextSendTime = Time.time + sendRate;
        }
    }

    private bool HasTransformChanged()
    {
        return lastPosition != cachedTransform.position;
    }

    private void SendTransformData()
    {
        byte[] transformData = new byte[36]; // 9 floats * 4 bytes

        // Convert position
        Buffer.BlockCopy(BitConverter.GetBytes(cachedTransform.position.x), 0, transformData, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(cachedTransform.position.y), 0, transformData, 4, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(cachedTransform.position.z), 0, transformData, 8, 4);

        // Update last known transform
        lastPosition = cachedTransform.position;

        // Send to NetworkManager
        NetworkManager.Instance.RelayTransformData(transformData, gameObject.GetInstanceID());
    }
}