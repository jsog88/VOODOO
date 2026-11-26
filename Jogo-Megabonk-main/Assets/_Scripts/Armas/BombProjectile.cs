using System.Collections;
using UnityEngine;

public class BombProjectile : MonoBehaviour, IProjectile
{
    [Header("Trayectoria Parabólica")]
    public float arcHeight = 3.5f;

    [Header("Explosión en Área")]
    public float baseExplosionRadius = 3.5f;
    public float radiusBonusPerLevel = 0.4f;
    public Color explosionFlashColor = new Color(1f, 0.45f, 0.05f, 0.85f);

    private Vector3 _startPos;
    private Vector3 _targetPos;
    private float _travelDuration;
    private float _elapsed;

    private float _damage;
    private float _knockback;
    private float _finalRadius;
    private bool _isCrit;
    private LayerMask _targetLayer;
    private bool _hasExploded;

    private readonly Collider[] _hitBuffer = new Collider[64];

    public void Initialize(ProjectileInitData data)
    {
        _startPos = transform.position;
        _targetPos = data.targetPosition;
        _damage = data.damage;
        _knockback = data.knockback;
        _isCrit = data.isCrit;
        _targetLayer = data.targetLayer;

        // El radio escala con el nivel propio de la bomba y con el Tomo de Área
        float levelRadius = baseExplosionRadius + radiusBonusPerLevel * (data.weaponLevel - 1);
        _finalRadius = levelRadius * data.areaMultiplier;

        float distance = Vector3.Distance(_startPos, _targetPos);
        _travelDuration = Mathf.Clamp(distance / Mathf.Max(1f, data.speed), 0.35f, 0.9f);
        _elapsed = 0f;
        _hasExploded = false;
    }

    private void Update()
    {
        if (_hasExploded) return;

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _travelDuration);

        // Movimiento horizontal + arco parabólico
        Vector3 currentPos = Vector3.Lerp(_startPos, _targetPos, t);
        currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
        transform.position = currentPos;

        transform.Rotate(Vector3.right, 360f * Time.deltaTime);

        if (t >= 1f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        int count = Physics.OverlapSphereNonAlloc(transform.position, _finalRadius, _hitBuffer, _targetLayer, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            if (_hitBuffer[i].TryGetComponent(out InDamage damageable))
            {
                Vector3 pushDir = _hitBuffer[i].transform.position - transform.position;
                pushDir.y = 0f;
                if (pushDir.sqrMagnitude < 0.001f) pushDir = Random.insideUnitSphere;
                pushDir.y = 0f;
                pushDir.Normalize();

                damageable.TakeDamage(_damage, transform.position, pushDir * _knockback, _isCrit);
            }
        }

        StartCoroutine(ExplosionVisualRoutine());
    }

    private IEnumerator ExplosionVisualRoutine()
    {
        // Expande brevemente la esfera al tamaño real del área dañada
        if (TryGetComponent(out Renderer rend))
        {
            rend.material.color = explosionFlashColor;
        }

        float timer = 0f;
        float duration = 0.16f;
        Vector3 initialScale = transform.localScale;
        Vector3 targetScale = Vector3.one * (_finalRadius * 2f);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(initialScale, targetScale, timer / duration);
            yield return null;
        }

        Destroy(gameObject);
    }
}