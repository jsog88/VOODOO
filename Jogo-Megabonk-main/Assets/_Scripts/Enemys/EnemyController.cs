using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class EnemyController : MonoBehaviour, InDamage
{
    [Header("Configuración")]
    public EnemyData data;
    public Renderer meshRenderer;

    private Rigidbody _rb;
    private Transform _target;
    private IObjectPool<EnemyController> _pool;

    private float _currentDamage;
    private float _currentSpeed;

    private float _currentHp;
    private Vector3 _knockbackVelocity;
    private bool _isDead;

    private MaterialPropertyBlock _propBlock;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private Color _originalColor = Color.white;
    private Coroutine _flashRoutine;

    // Evento para conectar luego el drop de gemas de XP o partículas
    public static event Action<Vector3, int> OnEnemyKilled;
    public static event Action OnBossKilled;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = false;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<Renderer>();

        _propBlock = new MaterialPropertyBlock();
        if (meshRenderer != null && meshRenderer.sharedMaterial != null && meshRenderer.sharedMaterial.HasProperty(BaseColorId))
        {
            _originalColor = meshRenderer.sharedMaterial.GetColor(BaseColorId);
        }
    }

    private void Start()
    {
        // Fallback por si colocas un enemigo manualmente en la escena sin usar el Spawner
        if (_target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _target = playerObj.transform;
        }

        if (_currentHp <= 0f && data != null)
        {
            _currentHp = data.maxHp;
        }
    }

    public void Initialize(Transform target, IObjectPool<EnemyController> pool, float hpMultiplier = 1f, float damageMultiplier = 1f, float speedMultiplier = 1f)
    {
        _target = target;
        _pool = pool;
        _isDead = false;
        _knockbackVelocity = Vector3.zero;

        _currentHp = (data != null ? data.maxHp : 40f) * hpMultiplier;
        _currentDamage = (data != null ? data.contactDamage : 10f) * damageMultiplier;
        _currentSpeed = (data != null ? data.moveSpeed : 4.5f) * speedMultiplier;

        bool flying = data != null && data.isFlying;
        _rb.useGravity = !flying;
        _rb.linearVelocity = Vector3.zero;

        SetFlashColor(_originalColor);
    }

    private void FixedUpdate()
    {
        if (_isDead || _target == null) return;

        float speed = _currentSpeed > 0f ? _currentSpeed : (data != null ? data.moveSpeed : 4.5f);
        bool flying = data != null && data.isFlying;

        if (flying)
        {
            Vector3 targetCenter = _target.position + Vector3.up * 0.9f;
            Vector3 toTarget3D = targetCenter - transform.position;
            Vector3 moveDir3D = toTarget3D.sqrMagnitude > 0.01f ? toTarget3D.normalized : Vector3.zero;

            // Si tiene una plataforma enfrente o encima bloqueando el paso, se eleva para bordearla
            if (Physics.SphereCast(transform.position, 0.45f, moveDir3D, out RaycastHit obstacleHit, 1.4f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!obstacleHit.collider.CompareTag("Player"))
                {
                    moveDir3D = (moveDir3D + Vector3.up * 1.4f).normalized;
                }
            }

            Vector3 flatLook = new Vector3(moveDir3D.x, 0f, moveDir3D.z);
            if (flatLook.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(flatLook);
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRot, 10f * Time.fixedDeltaTime));
            }

            _rb.linearVelocity = (moveDir3D * speed) + _knockbackVelocity;
        }
        else
        {
            Vector3 toTarget = _target.position - transform.position;
            toTarget.y = 0f;
            Vector3 moveDir = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : Vector3.zero;

            if (moveDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRot, 10f * Time.fixedDeltaTime));
            }

            Vector3 finalHorizontal = (moveDir * speed) + _knockbackVelocity;

            // Adapta el movimiento a los desniveles del MeshCollider del cofre sin atascarse
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit groundHit, 0.9f, ~0, QueryTriggerInteraction.Ignore))
            {
                Vector3 slopeMove = Vector3.ProjectOnPlane(finalHorizontal, groundHit.normal);
                _rb.linearVelocity = new Vector3(slopeMove.x, Mathf.Min(_rb.linearVelocity.y, -1f), slopeMove.z);
            }
            else
            {
                _rb.linearVelocity = new Vector3(finalHorizontal.x, _rb.linearVelocity.y, finalHorizontal.z);
            }
        }

        _knockbackVelocity = Vector3.Lerp(_knockbackVelocity, Vector3.zero, 10f * Time.fixedDeltaTime);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (_isDead) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.gameObject.TryGetComponent(out PlayerHealth playerHealth))
            {
                float dmg = _currentDamage > 0f ? _currentDamage : (data != null ? data.contactDamage : 10f);
                Vector3 hitPoint = collision.GetContact(0).point;
                playerHealth.TakeDamage(dmg, hitPoint, Vector3.zero, false);
            }
        }
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 knockbackForce, bool isCrit)
    {
        if (_isDead) return;

        _currentHp -= amount;

        float resistance = data != null ? data.knockbackResistance : 1f;
        if (resistance > 0f)
        {
            _knockbackVelocity += knockbackForce / resistance;
            _knockbackVelocity.y = 0f;
        }

        TriggerHitFlash(isCrit ? Color.yellow : Color.white);

        if (_currentHp <= 0f)
        {
            Die();
        }
    }

    private void TriggerHitFlash(Color flashColor)
    {
        if (meshRenderer == null) return;
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashCoroutine(flashColor));
    }

    private IEnumerator FlashCoroutine(Color flashColor)
    {
        SetFlashColor(flashColor);
        yield return new WaitForSeconds(0.08f);
        SetFlashColor(_originalColor);
    }

    private void SetFlashColor(Color color)
    {
        if (meshRenderer == null) return;
        meshRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(BaseColorId, color);
        meshRenderer.SetPropertyBlock(_propBlock);
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        int xp = data != null ? data.xpDropAmount : 10;
        OnEnemyKilled?.Invoke(transform.position, xp);

        if (data != null && data.isBoss)
        {
            OnBossKilled?.Invoke();
            if (data.bossGemPrefab != null)
            {
                Instantiate(data.bossGemPrefab, transform.position + Vector3.up * 0.9f, Quaternion.identity);
            }
        }

        if (_pool != null)
        {
            _pool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
