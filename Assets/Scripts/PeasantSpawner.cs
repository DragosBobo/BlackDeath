using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PeasantSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject peasantPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private int count = 50;
    [SerializeField] private float spawnRadius = 20f;

    [Tooltip("Try to keep peasants at least this far apart at spawn time.")]
    [SerializeField] private float minSpawnSeparation = 1.2f;

    [Tooltip("How far (max) we search to snap a random point onto the NavMesh.")]
    [SerializeField] private float navMeshSampleMaxDistance = 3f;

    [Tooltip("Delay between spawns, in seconds.")]
    [SerializeField] private float spawnDelay = 0.2f;

    [Tooltip("Max attempts to find a valid spot per peasant.")]
    [SerializeField] private int attemptsPerPeasant = 25;

    [Header("Runtime")]
    [SerializeField] private bool spawnOnStart = true;

    private readonly List<Vector3> usedPositions = new List<Vector3>(128);

    private void Start()
    {
        if (spawnOnStart)
            StartCoroutine(SpawnRoutine());
    }

    [ContextMenu("Spawn All (timed)")]
    public void SpawnNow()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        if (!peasantPrefab)
        {
            Debug.LogError("[PeasantSpawner] Missing peasantPrefab.");
            yield break;
        }

        usedPositions.Clear();
        int spawned = 0;

        for (int i = 0; i < count; i++)
        {
            if (TryFindSpawnPoint(out Vector3 pos))
            {
                Instantiate(peasantPrefab, pos, RandomYRotation());
                usedPositions.Add(pos);
                spawned++;
            }

            yield return new WaitForSeconds(spawnDelay);
        }

        Debug.Log($"[PeasantSpawner] Spawned {spawned}/{count} peasants.");
    }

    private bool TryFindSpawnPoint(out Vector3 result)
    {
        Vector3 origin = transform.position;

        for (int attempt = 0; attempt < attemptsPerPeasant; attempt++)
        {
            // 1) random point in circle
            Vector2 r = Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = new Vector3(origin.x + r.x, origin.y, origin.z + r.y);

            // 2) snap to NavMesh
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleMaxDistance, NavMesh.AllAreas))
                continue;

            Vector3 snapped = hit.position;

            // 3) optional separation
            if (minSpawnSeparation > 0f && !IsFarEnough(snapped))
                continue;

            result = snapped;
            return true;
        }

        result = origin;
        return false;
    }

    private bool IsFarEnough(Vector3 candidate)
    {
        float minSqr = minSpawnSeparation * minSpawnSeparation;

        for (int i = 0; i < usedPositions.Count; i++)
        {
            if ((usedPositions[i] - candidate).sqrMagnitude < minSqr)
                return false;
        }
        return true;
    }

    private static Quaternion RandomYRotation()
    {
        return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
#endif
}
