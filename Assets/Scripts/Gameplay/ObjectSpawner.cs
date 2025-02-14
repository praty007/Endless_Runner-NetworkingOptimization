using UnityEngine;
using System.Collections.Generic;

public class ObjectSpawner : MonoBehaviour
{
    public static ObjectSpawner instance{get; private set;}

    [Header("General Spawn Settings")]
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float minSpawnZ = 20f;
    [SerializeField] private float maxSpawnZ = 40f;
    [SerializeField] private float[] lanePositions = { -4f, 0f, 4f }; // X positions for lanes

    [Header("Obstacle Settings")]
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private int obstaclePoolSize = 20;

    [Header("Coin Settings")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int coinPoolSize = 30;
    [SerializeField] private float coinSpawnChance = 0.7f;

    [Header("Difficulty Settings")]
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

    void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        InitializeObjectPools();
        nextSpawnTime = Time.time + spawnInterval;
        gameTime = 0f;
    }

    private void Update()
    {
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

    private void SpawnObjects()
    {
        // Random lane position for initial object
        int randomLane = Random.Range(0, lanePositions.Length);
        float xPos = lanePositions[randomLane];
        float zPos = referenceCamera.transform.position.z + Random.Range(minSpawnZ, maxSpawnZ);

        if (Random.value < coinSpawnChance)
        {
            SpawnCoin(new Vector3(xPos, 1f, zPos));
            
            // Spawn obstacle in a different lane
            int obstacleLane = (randomLane + Random.Range(1, lanePositions.Length)) % lanePositions.Length;
            SpawnObstacle(new Vector3(lanePositions[obstacleLane], 0.5f, zPos + 2f));
        }
        else
        {
            SpawnObstacle(new Vector3(xPos, 0.5f, zPos));
        }
    }

    private void SpawnObstacle(Vector3 position)
    {
        if (obstaclePool.Count == 0) return;

        GameObject obstacle = obstaclePool.Dequeue();
        obstacle.transform.position = position;
        obstacle.SetActive(true);
        activeObstacles.Add(obstacle);
    }

    private void SpawnCoin(Vector3 position)
    {
        if (coinPool.Count == 0) return;

        GameObject coin = coinPool.Dequeue();
        coin.transform.position = position;
        coin.SetActive(true);
        activeCoins.Add(coin);
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
            }
        }

        // Clean up coins
        for (int i = activeCoins.Count - 1; i >= 0; i--)
        {
            if (activeCoins[i].transform.position.z < referenceCamera.transform.position.z)
            {
                GameObject coin = activeCoins[i];
                activeCoins.RemoveAt(i);
                coin.SetActive(false);
                coinPool.Enqueue(coin);
            }
        }
    }

    public void ObstacleCollided(GameObject obstacle)
    {
        collidedObstacle = obstacle;
    }

    public void CoinCollected(GameObject coin)
    {
        if (activeCoins.Contains(coin))
        {
            activeCoins.Remove(coin);
            coin.SetActive(false);
            coinPool.Enqueue(coin);
        }
    }
}