using UnityEngine;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;
    [SerializeField] float tickRate;

    public float TickRate => tickRate;
    private Dictionary<int, NetworkTransformTarget> transformTargets = new Dictionary<int, NetworkTransformTarget>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void RegisterTransformTarget(int sourceId, NetworkTransformTarget target)
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

    public void RelayTransformData(byte[] transformData, int sourceId)
    {
        if (transformTargets.TryGetValue(sourceId, out NetworkTransformTarget target))
        {
            target.ReceiveTransformData(transformData);
        }
    }
}
