using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[System.Serializable]
public class EnemyWaveEntry
{
    public EnemyData enemyData;
    [Tooltip("Segundo de la partida a partir del cual empieza a salir este enemigo")]
    public float unlockTime = 0f;
    [Range(1, 100)]
    [Tooltip("Peso relativo de aparición frente a otros enemigos desbloqueados")]
    public int spawnWeight = 50;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public LayerMask groundLayer;
    [Tooltip("BoxCollider (con Is Trigger) que delimita el interior jugable del cofre")]
    public BoxCollider interiorBounds;

    [Header("Progresión de Tipos de Enemigos")]
    public List<EnemyWaveEntry> enemyRoster = new List<EnemyWaveEntry>();

    [Header("Curva de Ritmo (Inicio Lento -> Horda)")]
    public float warmupDuration = 30f;
    public float warmupSpawnInterval = 2.2f;
    public float baseSpawnInterval = 1.3f;
    public float minSpawnInterval = 0.18f;
    public int maxActiveEnemies = 250;

    [Header("Radio y Altura Interior del Cofre")]
    public float minSpawnRadius = 6f;
    public float maxSpawnRadius = 13f;
    [Tooltip("Altura máxima sobre el jugador desde donde busca suelo (evita chocar con la tapa del cofre)")]
    public float ceilingCheckHeight = 4.5f;

    [Header("Emboscada Anti-Campeo en Plataformas")]
    public bool enablePlatformAmbush = true;
    public EnemyData ambushBugData; // Arrastra aquí tu enemigo volador (Bat/Bicho)
    [Tooltip("Altura Y del suelo base del cofre")]
    public float chestFloorY = 0f;
    [Tooltip("Altura mínima sobre el piso para considerar que estás en una plataforma")]
    public float platformHeightThreshold = 2.0f;
    [Tooltip("Segundos arriba antes de que empiecen a salir bichos")]
    public float timeBeforeAmbush = 5.5f;
    [Tooltip("Cada cuántos segundos salen más bichos mientras sigas arriba")]
    public float ambushRepeatInterval = 3.5f;
    public int bugsPerAmbush = 3;

    private readonly Dictionary<EnemyData, IObjectPool<EnemyController>> _pools = new Dictionary<EnemyData, IObjectPool<EnemyController>>();
    private float _spawnTimer;
    private float _elapsedTime;
    private int _activeCount;

    private float _highGroundTimer;
    private float _ambushCooldownTimer;

    public float ElapsedTime => _elapsedTime;

    private void Start()
    {
        if (player != null)
        {
            chestFloorY = player.position.y;
        }
    }

    private void Update()
    {
        if (player == null || enemyRoster.Count == 0) return;

        _elapsedTime += Time.deltaTime;
        _spawnTimer -= Time.deltaTime;

        if (_spawnTimer <= 0f && _activeCount < maxActiveEnemies)
        {
            int burstCount = GetBurstCountForCurrentTime();
            for (int i = 0; i < burstCount; i++)
            {
                if (_activeCount >= maxActiveEnemies) break;
                SpawnNextEnemy();
            }

            _spawnTimer = GetCurrentSpawnInterval();
        }

        HandlePlatformAmbush();
    }

    private void HandlePlatformAmbush()
    {
        if (!enablePlatformAmbush || player == null) return;

        bool isOnHighPlatform = player.position.y >= (chestFloorY + platformHeightThreshold);

        if (isOnHighPlatform)
        {
            _highGroundTimer += Time.deltaTime;

            if (_highGroundTimer >= timeBeforeAmbush)
            {
                _ambushCooldownTimer -= Time.deltaTime;
                if (_ambushCooldownTimer <= 0f)
                {
                    TriggerPlatformAmbush();
                    _ambushCooldownTimer = ambushRepeatInterval;
                }
            }
        }
        else
        {
            // Se reinicia poco a poco al bajar al piso del cofre
            _highGroundTimer = Mathf.Max(0f, _highGroundTimer - Time.deltaTime * 2f);
            _ambushCooldownTimer = 0f;
        }
    }

    private void TriggerPlatformAmbush()
    {
        EnemyData bugData = ambushBugData != null ? ambushBugData : PickEnemyDataForCurrentTime();
        if (bugData == null || bugData.prefab == null) return;

        float minutes = _elapsedTime / 60f;
        float hpScale = 1f + (minutes * 0.35f) + (minutes * minutes * 0.12f);
        float damageScale = 1f + (minutes * 0.20f);
        float speedScale = Mathf.Min(1.6f, 1.1f + (minutes * 0.08f));

        for (int i = 0; i < bugsPerAmbush; i++)
        {
            if (_activeCount >= maxActiveEnemies) break;

            float angle = (360f / bugsPerAmbush) * i + Random.Range(-25f, 25f);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Vector3 spawnPos = player.position + dir * Random.Range(3.5f, 6.5f) + Vector3.up * Random.Range(1.2f, 2.5f);
            spawnPos = ClampInsideChest(spawnPos);

            IObjectPool<EnemyController> pool = GetOrCreatePool(bugData);
            EnemyController bug = pool.Get();
            bug.transform.position = spawnPos;
            bug.Initialize(player, pool, hpScale, damageScale, speedScale);
        }
    }

    private float GetCurrentSpawnInterval()
    {
        if (_elapsedTime < warmupDuration)
        {
            float t = _elapsedTime / warmupDuration;
            return Mathf.Lerp(warmupSpawnInterval, baseSpawnInterval, t);
        }

        float timeAfterWarmup = _elapsedTime - warmupDuration;
        return Mathf.Max(minSpawnInterval, baseSpawnInterval - (timeAfterWarmup * 0.009f));
    }

    private int GetBurstCountForCurrentTime()
    {
        if (_elapsedTime < 35f) return 1;
        if (_elapsedTime < 90f) return Random.value < 0.45f ? 2 : 1;
        if (_elapsedTime < 180f) return Random.Range(2, 5);
        return Random.Range(4, 7);
    }

    private void SpawnNextEnemy()
    {
        EnemyData selectedData = PickEnemyDataForCurrentTime();
        if (selectedData == null || selectedData.prefab == null) return;

        if (TryGetValidSpawnPosition(selectedData, out Vector3 spawnPos))
        {
            IObjectPool<EnemyController> pool = GetOrCreatePool(selectedData);
            EnemyController enemy = pool.Get();
            enemy.transform.position = spawnPos;

            float minutes = _elapsedTime / 60f;
            float hpScale = 1f + (minutes * 0.35f) + (minutes * minutes * 0.12f);
            float damageScale = 1f + (minutes * 0.20f);
            float speedScale = Mathf.Min(1.55f, 1f + (minutes * 0.08f));

            enemy.Initialize(player, pool, hpScale, damageScale, speedScale);
        }
    }

    private EnemyData PickEnemyDataForCurrentTime()
    {
        int totalWeight = 0;
        for (int i = 0; i < enemyRoster.Count; i++)
        {
            EnemyWaveEntry entry = enemyRoster[i];
            if (entry.enemyData != null && entry.enemyData.prefab != null && _elapsedTime >= entry.unlockTime)
            {
                totalWeight += entry.spawnWeight;
            }
        }

        if (totalWeight <= 0) return null;

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;

        for (int i = 0; i < enemyRoster.Count; i++)
        {
            EnemyWaveEntry entry = enemyRoster[i];
            if (entry.enemyData != null && entry.enemyData.prefab != null && _elapsedTime >= entry.unlockTime)
            {
                cumulative += entry.spawnWeight;
                if (roll < cumulative)
                {
                    return entry.enemyData;
                }
            }
        }

        return null;
    }

    private bool TryGetValidSpawnPosition(EnemyData data, out Vector3 validPosition)
    {
        for (int attempt = 0; attempt < 6; attempt++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float randomDist = Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector3 candidate = player.position + new Vector3(randomDir.x * randomDist, 0f, randomDir.y * randomDist);

            // Asegura que el punto candidato caiga dentro de las paredes del cofre
            candidate = ClampInsideChest(candidate);

            // Evita aparecer encima del jugador si quedó pegado a una pared
            if (Vector2.Distance(new Vector2(candidate.x, candidate.z), new Vector2(player.position.x, player.position.z)) < 3.5f)
            {
                continue;
            }

            Vector3 rayOrigin = new Vector3(candidate.x, player.position.y + ceilingCheckHeight, candidate.z);

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, ceilingCheckHeight + 12f, groundLayer, QueryTriggerInteraction.Ignore))
            {
                float heightOffset = data.isFlying ? data.spawnHeightOffset : 1f;
                validPosition = ClampInsideChest(hit.point + Vector3.up * heightOffset);
                return true;
            }
            else if (data.isFlying)
            {
                validPosition = ClampInsideChest(new Vector3(candidate.x, player.position.y + data.spawnHeightOffset, candidate.z));
                return true;
            }
        }

        validPosition = Vector3.zero;
        return false;
    }

    public Vector3 ClampInsideChest(Vector3 worldPos)
    {
        if (interiorBounds == null) return worldPos;

        Bounds b = interiorBounds.bounds;
        float wallMargin = 1.1f;

        worldPos.x = Mathf.Clamp(worldPos.x, b.min.x + wallMargin, b.max.x - wallMargin);
        worldPos.z = Mathf.Clamp(worldPos.z, b.min.z + wallMargin, b.max.z - wallMargin);
        worldPos.y = Mathf.Clamp(worldPos.y, b.min.y + 0.3f, b.max.y - 0.8f);
        return worldPos;
    }

    private IObjectPool<EnemyController> GetOrCreatePool(EnemyData data)
    {
        if (_pools.TryGetValue(data, out IObjectPool<EnemyController> existingPool))
        {
            return existingPool;
        }

        IObjectPool<EnemyController> newPool = new ObjectPool<EnemyController>(
            createFunc: () =>
            {
                GameObject obj = Instantiate(data.prefab);
                EnemyController controller = obj.GetComponent<EnemyController>();
                controller.data = data;
                return controller;
            },
            actionOnGet: enemy =>
            {
                enemy.gameObject.SetActive(true);
                _activeCount++;
            },
            actionOnRelease: enemy =>
            {
                enemy.gameObject.SetActive(false);
                _activeCount = Mathf.Max(0, _activeCount - 1);
            },
            actionOnDestroy: enemy =>
            {
                if (enemy != null) Destroy(enemy.gameObject);
            },
            collectionCheck: false,
            defaultCapacity: 50,
            maxSize: 300
        );

        _pools[data] = newPool;
        return newPool;
    }
}