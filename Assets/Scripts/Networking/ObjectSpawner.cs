using UnityEngine;
using System.Collections.Generic;

public class ObjectSpawner : MonoBehaviour
{
    public static ObjectSpawner instance{get; private set;}

    [Header("Network Settings")]
    [SerializeField] private bool isOwner;
    [SerializeField] private ObjectSpawner sourceSpawner;

    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float minSpawnZ = 20f;
    [SerializeField] private float maxSpawnZ = 40f;
    [SerializeField] private float[] lanePositions = { -4f, 0f, 4f }; // X positions for lanes

    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private int obstaclePoolSize = 20;

    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int coinPoolSize = 30;
    [SerializeField] private float coinSpawnChance = 0.7f;

    [SerializeField] private float minSpawnIntervalAtMaxDifficulty = 0.5f;
    [SerializeField] private float difficultyRampUpTime = 60f; // Time in seconds to reach max difficulty
    [SerializeField] GameObject referenceCamera;

    private float nextSpawnTime;
    private float gameTime;
    private Queue<GameObject> obstaclePool;
    private Queue<GameObject> coinPool;
    private List<GameObject> activeObstacles;
    private List<GameObject> activeCoins;
    private GameObject collidedObstacle;
    private GameObject collecteCoin;

    int TotalDamage = 0, TotalScore = 0;

    void Awake()
    {
        instance = this;
    }

    private Dictionary<int, GameObject> sourceToTargetObjects = new Dictionary<int, GameObject>();

    private void Start()
    {
        InitializeObjectPools();
        if (!isOwner)
        {
            NetworkManager.Instance.RegisterSpawnerTarget(sourceSpawner.GetInstanceID(), this);
            return;
        }

        nextSpawnTime = Time.time + spawnInterval;
        gameTime = 0f;
    }

    private void InitializeObjectPools()
    {
        obstaclePool = new Queue<GameObject>();
        coinPool = new Queue<GameObject>();
        activeObstacles = new List<GameObject>();
        activeCoins = new List<GameObject>();

        for (int i = 0; i < obstaclePoolSize; i++)
        {
            GameObject obstacle = Instantiate(obstaclePrefab);
            obstacle.SetActive(false);
            obstacle.transform.SetParent(obstaclePrefab.transform.parent);
            obstaclePool.Enqueue(obstacle);
        }

        for (int i = 0; i < coinPoolSize; i++)
        {
            GameObject coin = Instantiate(coinPrefab);
            coin.SetActive(false);
            coin.transform.SetParent(coinPrefab.transform.parent);
            coinPool.Enqueue(coin);
        }
    }

#region Owner
    private void Update()
    {
        if (!isOwner) return; // Only owner handles spawning logic

        gameTime += Time.deltaTime;

        // Calculate current spawn interval based on difficulty
        float currentSpawnInterval = Mathf.Lerp(spawnInterval, minSpawnIntervalAtMaxDifficulty, 
            Mathf.Clamp01(gameTime / difficultyRampUpTime));

        if (Time.time >= nextSpawnTime)
        {
            SpawnObjects();
            nextSpawnTime = Time.time + currentSpawnInterval;
        }

        // Clean up objects that are behind the player
        CleanupObjects();
    }
#endregion
    private void SpawnObjects()
    {
        var randomLane = Random.Range(0, lanePositions.Length);
        float xPos = lanePositions[randomLane];
        float zPos = referenceCamera.transform.position.z + Random.Range(minSpawnZ, maxSpawnZ);
        Vector3 worldSpawnPos = new Vector3(xPos, obstaclePrefab.transform.position.y, zPos);

        if (Random.value < coinSpawnChance)
        {
            SpawnCoin(new Vector3(worldSpawnPos.x, coinPrefab.transform.position.y, worldSpawnPos.z));
            
            // Spawn obstacle in a different lane
            int obstacleLane = (randomLane + Random.Range(1, lanePositions.Length)) % lanePositions.Length;
            worldSpawnPos = new  Vector3(lanePositions[obstacleLane], obstaclePrefab.transform.position.y, zPos + 2f);
        }
        SpawnObstacle(worldSpawnPos);
    }

    private GameObject SpawnObstacle(Vector3 position)
    {
        if (obstaclePool.Count == 0) return null;

        GameObject obstacle = obstaclePool.Dequeue();
        
        if (isOwner)
        {
            obstacle.transform.position = position;
            byte[] spawnData = Utils.TransformDataSerializer.SerializeSpawnData("obstacle", position, obstacle.GetInstanceID());
            NetworkManager.Instance.RelaySpawnData(spawnData, GetInstanceID());
        }
        else
        {
            position += NetworkTransform.PositionOffset;
            obstacle.transform.position = position;
        }
        
        obstacle.SetActive(true);
        activeObstacles.Add(obstacle);
        return obstacle;
    }

    private GameObject SpawnCoin(Vector3 position)
    {
        if (coinPool.Count == 0) return null;

        GameObject coin = coinPool.Dequeue();
        
        if (isOwner)
        {
            coin.transform.position = position;
            byte[] spawnData = Utils.TransformDataSerializer.SerializeSpawnData("coin", position, coin.GetInstanceID());
            NetworkManager.Instance.RelaySpawnData(spawnData, GetInstanceID());
        }
        else
        {
            position += NetworkTransform.PositionOffset;
            coin.transform.position = position;
        }
        
        coin.SetActive(true);
        activeCoins.Add(coin);
        return coin;
    }


    private void CleanupObjects()
    {
        // Clean up obstacles
        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            if (activeObstacles[i].transform.position.z < referenceCamera.transform.position.z || 
                collidedObstacle == activeObstacles[i])
            {
                GameObject obstacle = activeObstacles[i];
                activeObstacles.RemoveAt(i);
                obstacle.SetActive(false);
                obstaclePool.Enqueue(obstacle);

                if (isOwner)
                {
                    // Send cleanup data through network
                    byte[] cleanupData = Utils.TransformDataSerializer.SerializeCleanupData("obstacle", obstacle.GetInstanceID());
                    NetworkManager.Instance.RelayCleanupData(cleanupData, GetInstanceID());
                }
            }
        }

        // Clean up coins
        for (int i = activeCoins.Count - 1; i >= 0; i--)
        {
            if (activeCoins[i].transform.position.z < referenceCamera.transform.position.z || 
                collecteCoin == activeCoins[i])
            {
                GameObject coin = activeCoins[i];
                activeCoins.RemoveAt(i);
                coin.SetActive(false);
                coinPool.Enqueue(coin);

                if (isOwner)
                {
                    // Send cleanup data through network
                    byte[] cleanupData = Utils.TransformDataSerializer.SerializeCleanupData("coin", coin.GetInstanceID());
                    NetworkManager.Instance.RelayCleanupData(cleanupData, GetInstanceID());
                }
            }
        }
    }

    public void ObstacleCollided(GameObject obstacle)
    {
        if (!isOwner) return;

        collidedObstacle = obstacle;
        TotalDamage++;
        UIManager.Instance.UpdatePlayerStats(-1,TotalDamage, -1,-1);
        byte[] collisionData = Utils.TransformDataSerializer.SerializeCollisionData("obstacle", obstacle.GetInstanceID(), 1);
        NetworkManager.Instance.RelayCollisionData(collisionData, GetInstanceID());
    }

    public void CoinCollected(GameObject coin)
    {
        if (!isOwner) return;
        collecteCoin = coin;
        TotalScore++;
        UIManager.Instance.UpdatePlayerStats(TotalScore, -1, -1,-1);
        byte[] collectionData = Utils.TransformDataSerializer.SerializeCollisionData("coin", coin.GetInstanceID(), 1);
        NetworkManager.Instance.RelayCollisionData(collectionData, GetInstanceID());
    }

#region target

    public void ReceiveCollisionData(byte[] collisionData)
    {
        if (isOwner) return;

        var (objectType, instanceId, val) = Utils.TransformDataSerializer.DeserializeCollisionData(collisionData);
        if (sourceToTargetObjects.TryGetValue(instanceId, out GameObject targetObject))
        {
            if (objectType == "obstacle")
            {
                TotalDamage += val;
                UIManager.Instance.UpdatePlayerStats(-1, -1, -1, TotalDamage);
            }
            else
            {
                TotalScore += val;
                UIManager.Instance.UpdatePlayerStats(-1, -1, TotalScore, -1);
            }
        }
    }

    public void ReceiveSpawnData(byte[] spawnData)
    {
        if (isOwner) return;

        var spawnDataList = Utils.TransformDataSerializer.DeserializeBatchedSpawnData(spawnData);
        foreach (var data in spawnDataList)
        {
            var (objectType, worldPosition, instanceId) = Utils.TransformDataSerializer.DeserializeSpawnData(data);
            if (objectType == "obstacle")
            {
                GameObject obstacle = SpawnObstacle(worldPosition);
                if (obstacle != null)
                {
                    sourceToTargetObjects[instanceId] = obstacle;
                }
            }
            else if (objectType == "coin")
            {
                GameObject coin = SpawnCoin(worldPosition);
                if (coin != null)
                {
                    sourceToTargetObjects[instanceId] = coin;
                }
            }
        }
    }
  
    public void ReceiveCleanupData(byte[] cleanupData)
    {
        if (isOwner) return;

        var (objectType, instanceId) = Utils.TransformDataSerializer.DeserializeCleanupData(cleanupData);
        if (sourceToTargetObjects.TryGetValue(instanceId, out GameObject targetObject))
        {
            if (objectType == "obstacle" && activeObstacles.Contains(targetObject))
            {
                activeObstacles.Remove(targetObject);
                targetObject.SetActive(false);
                obstaclePool.Enqueue(targetObject);
            }
            else if (objectType == "coin" && activeCoins.Contains(targetObject))
            {
                activeCoins.Remove(targetObject);
                targetObject.SetActive(false);
                coinPool.Enqueue(targetObject);
            }
            sourceToTargetObjects.Remove(instanceId);
        }
    }
    #endregion
}