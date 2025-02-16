using UnityEngine;
using System.Collections.Generic;
using Utils;

public class NetworkManager : MonoBehaviour
{
    public static Vector3 PositionOffset => NetworkTransform.PositionOffset;
    public static NetworkManager Instance;
    float tickRate;

    public float TickRate => tickRate;
    private Dictionary<int, NetworkTransform> transformTargets = new Dictionary<int, NetworkTransform>();
    private Dictionary<int, ObjectSpawner> spawnerTargets = new Dictionary<int, ObjectSpawner>();
    private Queue<(byte[] data, int sourceId)> pendingSpawnData = new Queue<(byte[] data, int sourceId)>();
    private Queue<(byte[] data, int sourceId)> pendingCollisionData = new Queue<(byte[] data, int sourceId)>();
    private Queue<(byte[] data, int sourceId)> pendingCleanupData = new Queue<(byte[] data, int sourceId)>();

    int uncompressedByteCount = 0, compressedByteCount=0, totalUncompressedByteCount = 0, totalCompressedByteCount = 0;

#region Init

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    public void RegisterTransformTarget(int sourceId, NetworkTransform target)
    {
        transformTargets[sourceId] = target;
    }

    public void UnregisterTransformTarget(int sourceId)
    {
        if (transformTargets.ContainsKey(sourceId))
        {
            transformTargets.Remove(sourceId);
        }
    }
   public void RegisterSpawnerTarget(int sourceId, ObjectSpawner target)
    {
        spawnerTargets.Add(sourceId, target);
    }

    public void UnregisterSpawnerTarget(int sourceId)
    {
        if (spawnerTargets.ContainsKey(sourceId))
        {
            spawnerTargets.Remove(sourceId);
        }
    }

    public void SetTickInterval(float interval) => tickRate = interval;
#endregion
    
#region dataRelay

  public void RelaySpawnData(byte[] spawnData, int sourceId)
    {
        pendingSpawnData.Enqueue((spawnData, sourceId));
    }

    public void RelayCleanupData(byte[] cleanupData, int sourceId)
    {
        pendingCleanupData.Enqueue((cleanupData, sourceId));
    }
    public void RelayCollisionData(byte[] collisionData, int sourceId)
    {
        pendingCollisionData.Enqueue((collisionData, sourceId));
    }
    public void RelayTransformData(byte[] transformData, int sourceId)
    {   
        ProcessPendingSpawnData();
        ProcessPendingCollisionData();
        ProcessPendingCleanupData();
        if (transformTargets.TryGetValue(sourceId, out NetworkTransform target))
        {
            uncompressedByteCount += 12;
            compressedByteCount += transformData.Length;
            target.ReceiveTransformData(transformData);
        }
        totalCompressedByteCount += compressedByteCount;
        totalUncompressedByteCount += uncompressedByteCount;

        UIManager.Instance.UpdateNetworkStats(
            totalCompressedByteCount, 
            totalUncompressedByteCount, 
            compressedByteCount, 
            uncompressedByteCount);

        compressedByteCount = 0;
        uncompressedByteCount = 0;
    }

    private void ProcessPendingCleanupData()
    {
        while (pendingCleanupData.Count > 0)
        {
            var (data, sourceId) = pendingCleanupData.Dequeue();
            if (spawnerTargets.TryGetValue(sourceId, out ObjectSpawner target))
            {
                target.ReceiveCleanupData(data);
                // uncompressedByteCount += 4; // Instance ID (4 bytes)
                // compressedByteCount += data.Length;
            }
        }
    }

    

    private void ProcessPendingCollisionData()
    {
        while (pendingCollisionData.Count > 0)
        {
            var (data, sourceId) = pendingCollisionData.Dequeue();
            if (spawnerTargets.TryGetValue(sourceId, out ObjectSpawner target))
            {
                target.ReceiveCollisionData(data);
                // uncompressedByteCount += 8; // Instance ID (4 bytes) + collision value (4 bytes)
                // compressedByteCount += data.Length;
            }
        }
    }

    private void ProcessPendingSpawnData()
    {
        if (pendingSpawnData.Count == 0) return;

        // Group spawn data by target ID
        var spawnDataByTarget = new Dictionary<int, List<byte[]>>();
        while (pendingSpawnData.Count > 0)
        {
            var (data, id) = pendingSpawnData.Dequeue();
            if (!spawnDataByTarget.ContainsKey(id))
            {
                spawnDataByTarget[id] = new List<byte[]>();
            }
            spawnDataByTarget[id].Add(data);
        }

        // Send batched spawn data to each target
        foreach (var kvp in spawnDataByTarget)
        {
            if (spawnerTargets.TryGetValue(kvp.Key, out ObjectSpawner target))
            {
                (byte[] batchedData, int uncompressedSize) = Utils.TransformDataSerializer.SerializeBatchedSpawnData(kvp.Value);
                target.ReceiveSpawnData(batchedData);
                compressedByteCount += batchedData.Length;
                uncompressedByteCount += uncompressedSize;
            }
        }
    }

 #endregion
}
