using UnityEngine;
using UnityEngine.Pool;

public class ExperienciaGem : MonoBehaviour
{
    [Header("Movimiento y Flotación")]
    public float bobSpeed = 3f;
    public float bobHeight = 0.15f;
    public float baseMagnetSpeed = 14f;
    public float acceleration = 28f;

    private int _xpValue;
    private Transform _playerTransform;
    private ExperienceManager _xpManager;
    private IObjectPool<ExperienciaGem> _pool;

    private Vector3 _spawnPosition;
    private float _currentSpeed;
    private bool _isBeingPulled;
    private bool _isCollected;

    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    public void Initialize(Vector3 position, int xpAmount, Transform player, ExperienceManager xpManager, IObjectPool<ExperienciaGem> pool)
    {
        _spawnPosition = position + Vector3.up * 0.45f;
        transform.position = _spawnPosition;
        _xpValue = xpAmount;
        _playerTransform = player;
        _xpManager = xpManager;
        _pool = pool;

        _currentSpeed = baseMagnetSpeed;
        _isBeingPulled = false;
        _isCollected = false;

        UpdateVisualByValue(xpAmount);
    }

    private void UpdateVisualByValue(int xp)
    {
        if (_renderer == null) return;

        // Azul = pequeña, Verde = mediana, Dorada = grande
        Color gemColor = xp >= 50 ? new Color(1f, 0.8f, 0.1f) :
                         xp >= 20 ? new Color(0.2f, 0.95f, 0.35f) :
                                    new Color(0.2f, 0.65f, 1f);

        transform.localScale = Vector3.one * (xp >= 50 ? 0.5f : xp >= 20 ? 0.38f : 0.3f);

        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(BaseColorId, gemColor);
        _renderer.SetPropertyBlock(_propBlock);
    }

    private void Update()
    {
        if (_isCollected || _playerTransform == null || _xpManager == null) return;

        Vector3 targetPoint = _playerTransform.position + Vector3.up * 0.9f;
        float sqrDist = (targetPoint - transform.position).sqrMagnitude;
        float magnetRadius = _xpManager.magnetRadius;

        if (!_isBeingPulled && sqrDist <= magnetRadius * magnetRadius)
        {
            _isBeingPulled = true;
        }

        if (_isBeingPulled)
        {
            _currentSpeed += acceleration * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetPoint, _currentSpeed * Time.deltaTime);

            if (sqrDist <= 0.35f)
            {
                Collect();
            }
        }
        else
        {
            // Rotación y flotación suave en el sitio
            transform.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);
            float offsetY = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = _spawnPosition + Vector3.up * offsetY;
        }
    }

    private void Collect()
    {
        if (_isCollected) return;
        _isCollected = true;

        _xpManager.AddExperience(_xpValue);

        if (_pool != null)
            _pool.Release(this);
        else
            Destroy(gameObject);
    }
}
