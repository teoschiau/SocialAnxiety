using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Street_CrowdManager : MonoBehaviour
{
    [Header("Configuration")]
    public Transform activePlayer;      // The player transform (assigned by LevelManager)
    public GameObject[] robotPrefabs; // Array of different robot prefabs
    public Transform[] spawnPoints;     // List of empty objects at street ends

    [Header("Randomness")]
    public bool randomizeSpeed = true;
    public float minSpeed = 0.5f;
    public float maxSpeed = 1.2f;
    public float spawnRadius = 4.0f;    // Radius around spawn point to prevent stacking

    // We store the active agents to manage them if needed
    private int targetPopulation;

    /// <summary>
    /// Call this function from LevelSetupManager to fill the city.
    /// </summary>
    public void GenerateCrowd(int populationSize)
    {
        targetPopulation = populationSize;
        
        // Safety Check
        if (spawnPoints.Length < 2)
        {
            Debug.LogError("Street_CrowdManager: You need at least 2 Spawn Points assigned!");
            return;
        }

        // Start the mass spawning routine
        StartCoroutine(SpawnBatchRoutine(populationSize));
    }

    IEnumerator SpawnBatchRoutine(int count)
    {
        // We spawn them in a tight loop. 
        // We yield every 5 spawns to prevent the game from freezing while loading.
        for (int i = 0; i < count; i++)
        {
            SpawnSinglePedestrian();
            
            if (i % 5 == 0) yield return null; 
        }
    }

    void SpawnSinglePedestrian()
    {
        // 1. Pick Random Start and End
        int startIndex = Random.Range(0, spawnPoints.Length);
        int destIndex = GetDifferentRandomIndex(startIndex, spawnPoints.Length);

        Transform startNode = spawnPoints[startIndex];
        Transform endNode = spawnPoints[destIndex];

        // 2. Calculate Spawn Position with Random Offset (so they don't stack)
        Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
        randomOffset.y = 0; // Keep them on the floor
        Vector3 spawnPos = startNode.position + randomOffset;

        // 3. Instantiate
        GameObject robotPrefab = robotPrefabs[Random.Range(0, robotPrefabs.Length)];
        GameObject npc = Instantiate(robotPrefab, spawnPos, Quaternion.identity, transform);

        // 4. Setup Scripts (Player Reference)
        RobotMovement robotScript = npc.GetComponentInChildren<RobotMovement>();
        RobotHeadAI headScript = npc.GetComponentInChildren<RobotHeadAI>();
        ParentHeadAI parentHeadScript = npc.GetComponentInChildren<ParentHeadAI>();

        if (robotScript != null) robotScript.playerTransform = activePlayer;
        if (headScript != null) headScript.player = activePlayer;
        if (parentHeadScript != null) parentHeadScript.player = activePlayer;

        // 5. Setup NavMesh Agent
        NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            // agent.Warp(spawnPos); // Ensure they snap to the mesh
            agent.SetDestination(endNode.position);

            if (randomizeSpeed)
            {
                agent.speed = Random.Range(minSpeed, maxSpeed);
                // Randomize avoidance priority so they don't form lines
                agent.avoidancePriority = Random.Range(30, 70); 
            }
        }
    }

    // Helper to get a random index that isn't the start index
    int GetDifferentRandomIndex(int avoidIndex, int length)
    {
        int newIndex = Random.Range(0, length);
        while (newIndex == avoidIndex)
        {
            newIndex = Random.Range(0, length);
        }
        return newIndex;
    }
}