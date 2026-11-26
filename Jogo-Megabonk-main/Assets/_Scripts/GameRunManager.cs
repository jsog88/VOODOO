using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[System.Serializable]
public class ScheduledBoss
{
    public string bossTitle = "Guardián del Abismo";
    public EnemyData bossData;
    [Tooltip("Segundo de la partida en el que aparece este jefe (ej: 120 = 2:00)")]
    public float spawnTimeSeconds = 120f;
    [HideInInspector] public bool hasSpawned;
}

public class GameRunManager : MonoBehaviour
{
    public static GameRunManager Instance { get; private set; }

    [Header("Referencias")]
    public Transform player;
    public LayerMask groundLayer;

    [Header("Jefes Programados")]
    public List<ScheduledBoss> bosses = new List<ScheduledBoss>();
    public int gemsRequiredToWin = 2;
    public float bossSpawnDistance = 14f;

    public float ElapsedTime { get; private set; }
    public int CollectedBossGems { get; private set; }
    public bool IsVictory { get; private set; }

    public event Action<float> OnTimerUpdated;
    public event Action<int, int> OnBossGemCollected;
    public event Action<string> OnBossSpawned;
    public event Action OnFinalBossSpawned;
    public event Action OnGameWon;

    

    private CamaraTerceraPersona _cam;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _cam = FindFirstObjectByType<CamaraTerceraPersona>();
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        OnBossGemCollected?.Invoke(CollectedBossGems, gemsRequiredToWin);
    }

    private void Update()
    {
        if (IsVictory)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                RestartRun();
            }
            return;
        }

        ElapsedTime += Time.deltaTime;
        OnTimerUpdated?.Invoke(ElapsedTime);

        // Revisar si toca invocar algún Boss
        for (int i = 0; i < bosses.Count; i++)
        {
            ScheduledBoss entry = bosses[i];
            if (!entry.hasSpawned && entry.bossData != null && ElapsedTime >= entry.spawnTimeSeconds)
            {
                entry.hasSpawned = true;
                SpawnBoss(entry);

                if (i == bosses.Count - 1)
                {
                    OnFinalBossSpawned?.Invoke();
                }
            }
        }
    }

    private void SpawnBoss(ScheduledBoss entry)
    {
        if (player == null || entry.bossData.prefab == null) return;

        Vector3 spawnPos = FindBossSpawnPosition(entry.bossData);
        GameObject bossObj = Instantiate(entry.bossData.prefab, spawnPos, Quaternion.identity);

        if (bossObj.TryGetComponent(out EnemyController controller))
        {
            controller.data = entry.bossData;
            float minutes = ElapsedTime / 60f;
            float hpMult = 1f + (minutes * 0.35f);
            float dmgMult = 1f + (minutes * 0.2f);
            controller.Initialize(player, null, hpMult, dmgMult, 1f);
        }

        OnBossSpawned?.Invoke(entry.bossTitle);
    }

    private Vector3 FindBossSpawnPosition(EnemyData data)
    {
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();

        for (int i = 0; i < 10; i++)
        {
            Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
            Vector3 candidate = player.position + new Vector3(dir.x * bossSpawnDistance, 0f, dir.y * bossSpawnDistance);

            if (spawner != null)
                candidate = spawner.ClampInsideChest(candidate);

            Vector3 rayOrigin = new Vector3(candidate.x, player.position.y + 4.5f, candidate.z);

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 16f, groundLayer, QueryTriggerInteraction.Ignore))
            {
                float offset = data.isFlying ? data.spawnHeightOffset : 1.2f;
                Vector3 pos = hit.point + Vector3.up * offset;
                return spawner != null ? spawner.ClampInsideChest(pos) : pos;
            }
        }

        Vector3 fallback = player.position + Vector3.up * 1.5f;
        return spawner != null ? spawner.ClampInsideChest(fallback) : fallback;
    }

    public void CollectBossGem()
    {
        if (IsVictory) return;

        CollectedBossGems++;
        OnBossGemCollected?.Invoke(CollectedBossGems, gemsRequiredToWin);

        if (CollectedBossGems >= gemsRequiredToWin)
        {
            TriggerVictory();
        }
    }

    private void TriggerVictory()
    {
        IsVictory = true;
        Time.timeScale = 0f;

        if (_cam != null) _cam.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        OnGameWon?.Invoke();
    }

    public void RestartRun()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
