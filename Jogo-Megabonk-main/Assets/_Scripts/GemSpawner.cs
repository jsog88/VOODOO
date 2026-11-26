using UnityEngine;
using UnityEngine.Pool;

public class GemSpawner : MonoBehaviour
{
    public ExperienciaGem gemPrefab;
    public Transform player;
    public ExperienceManager experienceManager;

    private IObjectPool<ExperienciaGem> _gemPool;

    private void Awake()
    {
        _gemPool = new ObjectPool<ExperienciaGem>(
            createFunc: () => Instantiate(gemPrefab),
            actionOnGet: gem => gem.gameObject.SetActive(true),
            actionOnRelease: gem => gem.gameObject.SetActive(false),
            actionOnDestroy: gem => { if (gem != null) Destroy(gem.gameObject); },
            collectionCheck: false,
            defaultCapacity: 100,
            maxSize: 500
        );
    }

    private void OnEnable()
    {
        EnemyController.OnEnemyKilled += HandleEnemyKilled;
    }

    private void OnDisable()
    {
        EnemyController.OnEnemyKilled -= HandleEnemyKilled;
    }

    private void HandleEnemyKilled(Vector3 deathPosition, int xpAmount)
    {
        if (gemPrefab == null || player == null || experienceManager == null) return;

        ExperienciaGem gem = _gemPool.Get();
        gem.Initialize(deathPosition, xpAmount, player, experienceManager, _gemPool);
    }
}
