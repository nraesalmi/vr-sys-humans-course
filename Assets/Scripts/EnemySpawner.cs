using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class Wave
{
    [Header("Wave Settings")]
    public string waveName = "Wave 1";
    public float spawnInterval = 2f;
    public float timeBeforeNextWave = 5f;

    [Header("Enemy Types")]
    public List<EnemySpawnData> enemiesToSpawn = new List<EnemySpawnData>();
}

[System.Serializable]
public class EnemySpawnData
{
    public GameObject enemyPrefab;
    public int count = 1;
    public float healthMultiplier = 1f; // Optional: for difficulty scaling
    public float speedMultiplier = 1f;  // Optional: for difficulty scaling
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Wave Settings")]
    public List<Wave> waves = new List<Wave>();
    public Transform spawnPoint;
    public Transform startWaypoint;

    [Header("Current Wave Info")]
    public int currentWaveIndex = 0;
    public int totalEnemiesDefeated = 0;
    public bool isSpawning = false;
    public bool allWavesComplete = false;

    [Header("Game Events")]
    public BaseHealth baseHealth;

    [Header("Debug")]
    public bool debugMode = false;

    [Header("UI")]
    public Text statusText; // Changed back to legacy Text
    public Vector3 textWorldOffset = new Vector3(0f, 3f, 0f);

    private float nextWaveCountdown = 0f;
    private bool waitingForNextWave = false;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private Coroutine spawnCoroutine;
    private bool introCheckCompleted = false;

    void Start()
    {
        if (baseHealth == null)
        {
            baseHealth = FindAnyObjectByType<BaseHealth>(FindObjectsInactive.Include);
            if (baseHealth == null)
                Debug.LogWarning("No BaseHealth found in scene!");
        }

        GameIntroManager introManager = FindAnyObjectByType<GameIntroManager>(FindObjectsInactive.Include);
        if (introManager == null)
        {
            introCheckCompleted = true;
            if (waves.Count > 0)
                StartNextWave();
            else
                Debug.LogWarning("No waves configured in spawner!");
        }
        else
        {
            Debug.Log("EnemySpawner: Waiting for intro to complete...");
        }
    }

    void Update()
    {
        // Check if intro is complete (only check once at start of Update)
        if (!introCheckCompleted)
        {
            GameIntroManager introManager = FindAnyObjectByType<GameIntroManager>(FindObjectsInactive.Include);
            if (introManager != null && !introManager.IsIntroComplete())
            {
                // Don't spawn enemies during intro
                if (statusText != null)
                    statusText.text = "DESTROY THE TARGETS TO BEGIN!";
                return;
            }
            else
            {
                introCheckCompleted = true;
                Debug.Log("EnemySpawner: Intro complete or no intro manager found.");
            }
        }

        // If wave has finished spawning and all enemies are gone, start countdown
        if (!isSpawning && activeEnemies.Count == 0 && !waitingForNextWave && !allWavesComplete)
        {
            waitingForNextWave = true;
            nextWaveCountdown = waves[currentWaveIndex].timeBeforeNextWave;
        }

        // Countdown for next wave
        if (waitingForNextWave)
        {
            nextWaveCountdown -= Time.deltaTime;
            if (nextWaveCountdown <= 0f)
            {
                waitingForNextWave = false;
                currentWaveIndex++;
                StartNextWave();
            }
        }

        // Update the UI
        UpdateStatusTextPosition();
        UpdateStatusTextContent();
    }

    void UpdateStatusTextPosition()
    {
        if (statusText == null) return;
        statusText.transform.position = transform.position + textWorldOffset;
    }

    void UpdateStatusTextContent()
    {
        if (statusText == null) return;

        if (allWavesComplete)
        {
            statusText.text = "All waves complete!";
            return;
        }

        // Check if we're still in intro
        GameIntroManager introManager = FindAnyObjectByType<GameIntroManager>(FindObjectsInactive.Include);
        if (introManager != null && !introManager.IsIntroComplete())
        {
            statusText.text = "DESTROY THE TARGETS TO BEGIN!";
            return;
        }

        if (isSpawning || activeEnemies.Count > 0)
        {
            statusText.text = $"Wave {currentWaveIndex + 1}/{waves.Count}\n" +
                              $"Enemies left: {activeEnemies.Count}";
        }
        else if (waitingForNextWave)
        {
            statusText.text = $"Wave complete\nNext wave in: {nextWaveCountdown:F1}s";
        }
        else if (!isSpawning && currentWaveIndex < waves.Count)
        {
            statusText.text = $"Ready for wave {currentWaveIndex + 1}";
        }
    }

    public void StartNextWave()
    {
        if (currentWaveIndex >= waves.Count)
        {
            allWavesComplete = true;
            Debug.Log("All waves complete! Victory!");
            return;
        }

        if (isSpawning)
        {
            Debug.LogWarning("Already spawning a wave!");
            return;
        }

        Wave currentWave = waves[currentWaveIndex];
        waitingForNextWave = false;

        if (debugMode) Debug.Log($"Starting {currentWave.waveName}");

        spawnCoroutine = StartCoroutine(SpawnWave(currentWave));
    }

    IEnumerator SpawnWave(Wave wave)
    {
        isSpawning = true;

        if (debugMode) Debug.Log($"Spawning wave: {wave.waveName}");

        foreach (EnemySpawnData enemyData in wave.enemiesToSpawn)
        {
            for (int i = 0; i < enemyData.count; i++)
            {
                if (enemyData.enemyPrefab != null)
                {
                    SpawnEnemy(enemyData.enemyPrefab, enemyData.healthMultiplier, enemyData.speedMultiplier);
                    yield return new WaitForSeconds(wave.spawnInterval);
                }
            }
        }

        if (debugMode) Debug.Log($"Finished spawning all enemies for {wave.waveName}");

        isSpawning = false;
    }

    public void EnableSpawning()
    {
        Debug.Log("EnemySpawner.EnableSpawning() called");
        
        // Make sure the spawner is enabled
        enabled = true;
        
        // Reset the intro check
        introCheckCompleted = true;
        
        // Start the first wave if not already started
        if (!isSpawning && !allWavesComplete && currentWaveIndex < waves.Count)
        {
            // If currentWaveIndex is 0, start the first wave
            // If currentWaveIndex is > 0, we might be in the middle of waves
            if (currentWaveIndex == 0)
            {
                StartNextWave();
            }
            else
            {
                // We're already past wave 0, just resume
                Debug.Log($"Resuming at wave {currentWaveIndex + 1}");
            }
        }
        else if (allWavesComplete)
        {
            Debug.Log("All waves already completed");
        }
    }

    void SpawnEnemy(GameObject enemyPrefab, float healthMultiplier = 1f, float speedMultiplier = 1f)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("No spawn point assigned!");
            return;
        }

        GameObject newEnemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);

        // Setup waypoints
        var patrolScript = newEnemy.GetComponent<PatrolFollowingPredeterminedOrder>();
        if (patrolScript != null)
        {
            patrolScript.MovementSpeed *= speedMultiplier;

            if (startWaypoint != null)
                patrolScript.Waypoints = GetWaypointsList();
        }

        // Setup EnemyController
        var enemyController = newEnemy.GetComponent<EnemyController>();
        if (enemyController == null)
            enemyController = newEnemy.AddComponent<EnemyController>();

        enemyController.Initialize(this, baseHealth, healthMultiplier, speedMultiplier);

        activeEnemies.Add(newEnemy);

        if (debugMode) Debug.Log($"Spawned enemy at {spawnPoint.position}");
    }

    List<Transform> GetWaypointsList()
    {
        List<Transform> waypoints = new List<Transform>();
        if (startWaypoint != null)
        {
            waypoints.Add(startWaypoint);
            GameObject[] allWaypoints = GameObject.FindGameObjectsWithTag("Waypoint");
            System.Array.Sort(allWaypoints, (a, b) => a.name.CompareTo(b.name));
            foreach (var wp in allWaypoints)
            {
                if (wp.transform != startWaypoint)
                    waypoints.Add(wp.transform);
            }
        }
        return waypoints;
    }

    public void OnEnemyDestroyed(GameObject enemy)
    {
        totalEnemiesDefeated++;
        activeEnemies.Remove(enemy);

        if (debugMode) Debug.Log($"Enemy destroyed. {activeEnemies.Count} remaining in wave {currentWaveIndex + 1}");
    }

    public void OnEnemyReachedBase(GameObject enemy)
    {
        activeEnemies.Remove(enemy);

        if (debugMode) Debug.Log($"Enemy reached base. {activeEnemies.Count} remaining in wave {currentWaveIndex + 1}");
    }

    // Skip to next wave (for debugging)
    public void SkipToNextWave()
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);

        foreach (var enemy in activeEnemies.ToArray())
        {
            if (enemy != null) Destroy(enemy);
        }
        activeEnemies.Clear();

        isSpawning = false;
        waitingForNextWave = false;

        currentWaveIndex++;
        StartNextWave();
    }

    // Restart all waves
    public void RestartWaves()
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);

        foreach (var enemy in activeEnemies.ToArray())
        {
            if (enemy != null) Destroy(enemy);
        }
        activeEnemies.Clear();

        currentWaveIndex = 0;
        totalEnemiesDefeated = 0;
        isSpawning = false;
        waitingForNextWave = false;
        allWavesComplete = false;
        introCheckCompleted = true;

        StartNextWave();
    }

    void OnDrawGizmos()
    {
        if (spawnPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(spawnPoint.position, 0.5f);
            Gizmos.DrawWireCube(spawnPoint.position, Vector3.one);
        }
    }
}