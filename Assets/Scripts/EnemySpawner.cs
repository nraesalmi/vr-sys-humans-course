using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Wave
{
    [Header("Wave Settings")]
    public string waveName = "Wave 1";
    public int totalEnemies = 5;
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
    public Transform startWaypoint; // First waypoint for enemies
    
    [Header("Current Wave Info")]
    public int currentWaveIndex = 0;
    public int enemiesRemaining = 0;
    public int totalEnemiesDefeated = 0;
    public bool isSpawning = false;
    public bool allWavesComplete = false;
    
    [Header("Game Events")]
    public BaseHealth baseHealth; // Reference to base health script
    
    [Header("Debug")]
    public bool debugMode = false;
    
    private List<GameObject> activeEnemies = new List<GameObject>();
    private Coroutine spawnCoroutine;
    
    void Start()
    {
        // Auto-find base health if not assigned
        if (baseHealth == null)
        {
            baseHealth = FindFirstObjectByType<BaseHealth>();
            if (baseHealth == null)
            {
                Debug.LogWarning("No BaseHealth found in scene!");
            }
        }
        
        if (waves.Count > 0)
        {
            StartNextWave();
        }
        else
        {
            Debug.LogWarning("No waves configured in spawner!");
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
        enemiesRemaining = currentWave.totalEnemies;
        
        Debug.Log($"Starting {currentWave.waveName}");
        
        spawnCoroutine = StartCoroutine(SpawnWave(currentWave));
    }
    
    IEnumerator SpawnWave(Wave wave)
    {
        isSpawning = true;
        
        if (debugMode) Debug.Log($"Spawning wave: {wave.waveName}");
        
        // Spawn all enemies in the wave
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
        
        // Wait for all enemies to be defeated or reach base
        while (enemiesRemaining > 0)
        {
            yield return new WaitForSeconds(1f);
        }
        
        isSpawning = false;
        Debug.Log($"{wave.waveName} complete!");
        
        // Wait before next wave
        yield return new WaitForSeconds(wave.timeBeforeNextWave);
        
        currentWaveIndex++;
        StartNextWave();
    }
    
    void SpawnEnemy(GameObject enemyPrefab, float healthMultiplier = 1f, float speedMultiplier = 1f)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("No spawn point assigned!");
            return;
        }
        
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        
        // Setup enemy waypoints if PatrolFollowingPredeterminedOrder exists
        var patrolScript = newEnemy.GetComponent<PatrolFollowingPredeterminedOrder>();
        if (patrolScript != null)
        {
            // Apply speed multiplier if needed
            patrolScript.MovementSpeed *= speedMultiplier;
            
            // Set waypoints if startWaypoint is assigned
            if (startWaypoint != null)
            {
                // You can modify this to set the full waypoint list
                patrolScript.Waypoints = GetWaypointsList();
            }
        }
        
        // Set up enemy to report back when destroyed or reaching base
        var enemyController = newEnemy.GetComponent<EnemyController>();
        if (enemyController == null)
        {
            enemyController = newEnemy.AddComponent<EnemyController>();
        }
        enemyController.Initialize(this, baseHealth, healthMultiplier, speedMultiplier);
        
        activeEnemies.Add(newEnemy);
        
        if (debugMode) Debug.Log($"Spawned enemy at {spawnPoint.position}");
    }
    
    // Helper method to get waypoints (you'll need to set this up based on your level)
    List<Transform> GetWaypointsList()
    {
        List<Transform> waypoints = new List<Transform>();
        
        if (startWaypoint != null)
        {
            waypoints.Add(startWaypoint);
            
            // Find additional waypoints - you can modify this based on your setup
            // Example: Look for GameObjects with "Waypoint" in name
            GameObject[] allWaypoints = GameObject.FindGameObjectsWithTag("Waypoint");
            System.Array.Sort(allWaypoints, (a, b) => a.name.CompareTo(b.name));
            
            foreach (var wp in allWaypoints)
            {
                if (wp.transform != startWaypoint)
                {
                    waypoints.Add(wp.transform);
                }
            }
        }
        
        return waypoints;
    }
    
    public void OnEnemyDestroyed(GameObject enemy)
    {
        enemiesRemaining--;
        totalEnemiesDefeated++;
        activeEnemies.Remove(enemy);
        
        if (debugMode) Debug.Log($"Enemy destroyed. {enemiesRemaining} remaining in wave {currentWaveIndex + 1}");
    }
    
    public void OnEnemyReachedBase(GameObject enemy)
    {
        enemiesRemaining--;
        activeEnemies.Remove(enemy);
        
        if (debugMode) Debug.Log($"Enemy reached base. {enemiesRemaining} remaining in wave {currentWaveIndex + 1}");
    }
    
    // For UI display
    public string GetCurrentWaveInfo()
    {
        if (currentWaveIndex >= waves.Count)
            return "All waves complete!";
        
        return $"Wave {currentWaveIndex + 1}/{waves.Count}: {waves[currentWaveIndex].waveName}";
    }
    
    public float GetWaveProgress()
    {
        if (waves.Count == 0) return 0;
        
        if (currentWaveIndex >= waves.Count)
            return 1f;
        
        Wave currentWave = waves[currentWaveIndex];
        int totalSpawned = 0;
        foreach (EnemySpawnData data in currentWave.enemiesToSpawn)
        {
            totalSpawned += data.count;
        }
        
        int spawnedSoFar = totalSpawned - enemiesRemaining;
        return (float)spawnedSoFar / totalSpawned;
    }
    
    // Get remaining enemies for UI
    public int GetEnemiesRemaining()
    {
        return enemiesRemaining;
    }
    
    // Skip to next wave (for debugging)
    public void SkipToNextWave()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        
        // Destroy all active enemies
        foreach (var enemy in activeEnemies.ToArray())
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        activeEnemies.Clear();
        
        enemiesRemaining = 0;
        isSpawning = false;
        
        currentWaveIndex++;
        StartNextWave();
    }
    
    // Restart from first wave
    public void RestartWaves()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        
        // Destroy all active enemies
        foreach (var enemy in activeEnemies.ToArray())
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        activeEnemies.Clear();
        
        currentWaveIndex = 0;
        enemiesRemaining = 0;
        totalEnemiesDefeated = 0;
        isSpawning = false;
        allWavesComplete = false;
        
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