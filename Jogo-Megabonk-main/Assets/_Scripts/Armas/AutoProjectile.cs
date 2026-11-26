using UnityEngine;

public class AutoProjectile : MonoBehaviour, IProjectile
{
    public float lifeTime = 3f;

    private Vector3 _direction;
    private float _speed;
    private float _damage;
    private float _knockback;
    private bool _isCrit;
    private LayerMask _targetLayer;
    private float _timer;

    public void Initialize(ProjectileInitData data)
    {
        _direction = data.direction.normalized;
        _speed = data.speed;
        _damage = data.damage;
        _knockback = data.knockback;
        _isCrit = data.isCrit;
        _targetLayer = data.targetLayer;
        _timer = lifeTime;

        transform.localScale *= data.areaMultiplier;
    }

    private void Update()
    {
        float moveStep = _speed * Time.deltaTime;

        if (Physics.Raycast(transform.position, _direction, out RaycastHit hit, moveStep + 0.2f, _targetLayer, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.TryGetComponent(out InDamage damageable))
            {
                damageable.TakeDamage(_damage, hit.point, _direction * _knockback, _isCrit);
            }
            Destroy(gameObject);
            return;
        }

        transform.position += _direction * moveStep;

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
