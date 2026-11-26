using UnityEngine;

public struct ProjectileInitData
{
    public Vector3 direction;
    public Vector3 targetPosition;
    public float speed;
    public float damage;
    public float knockback;
    public float areaMultiplier;
    public int weaponLevel;
    public bool isCrit;
    public LayerMask targetLayer;
}

public interface IProjectile
{
    void Initialize(ProjectileInitData data);
}