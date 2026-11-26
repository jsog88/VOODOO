using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class EquippedWeapon
{
    public WeaponData data;
    [Min(1)] public int level = 1;
    [HideInInspector] public float cooldownTimer;
}
public class WeaponManager : MonoBehaviour
{
    [Header("Ranuras de Armas")]
    public int maxWeaponSlots = 4;
    public List<EquippedWeapon> equippedWeapons = new List<EquippedWeapon>();
    public LayerMask enemyLayer;

    [Header("Multiplicadores Globales (Tomos e Ítems)")]
    public float damageMultiplier = 1f;
    public float attackSpeedMultiplier = 1f;
    public float areaMultiplier = 1f;
    public int bonusProjectiles = 0;
    [Range(0f, 2f)] public float critChance = 0.05f; // >1.0 permite Overcrit
    public float critDamageMultiplier = 2f;

    // Buffer reutilizable para evitar Garbage Collection en cada disparo
    private readonly Collider[] _hitBuffer = new Collider[64];

    private void Update()
    {
        for (int i = 0; i < equippedWeapons.Count; i++)
        {
            EquippedWeapon weapon = equippedWeapons[i];
            if (weapon.data == null) continue;

            weapon.cooldownTimer -= Time.deltaTime * attackSpeedMultiplier;
            if (weapon.cooldownTimer <= 0f)
            {
                Transform target = FindClosestEnemy(weapon.data.range * areaMultiplier);
                if (target != null)
                {
                    FireWeapon(weapon, target);
                    weapon.cooldownTimer = weapon.data.GetCooldown(weapon.level);
                }
            }
        }
    }

    private Transform FindClosestEnemy(float searchRadius)
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, searchRadius, _hitBuffer, enemyLayer);
        Transform closest = null;
        float closestSqrDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            float sqrDist = (_hitBuffer[i].transform.position - transform.position).sqrMagnitude;
            if (sqrDist < closestSqrDist)
            {
                closestSqrDist = sqrDist;
                closest = _hitBuffer[i].transform;
            }
        }

        return closest;
    }

    private void FireWeapon(EquippedWeapon weapon, Transform primaryTarget)
    {
        if (weapon.data.projectilePrefab == null) return;

        int totalProjectiles = weapon.data.GetProjectileCount(weapon.level) + bonusProjectiles;
        Vector3 spawnOrigin = transform.position + Vector3.up * 1f;
        Vector3 targetCenter = primaryTarget.position + Vector3.up * 0.5f;
        Vector3 baseDir = (targetCenter - spawnOrigin).normalized;

        float spreadAngle = 12f;
        float startAngle = -spreadAngle * (totalProjectiles - 1) * 0.5f;

        for (int i = 0; i < totalProjectiles; i++)
        {
            Quaternion spreadRot = Quaternion.Euler(0f, startAngle + (spreadAngle * i), 0f);
            Vector3 shotDir = spreadRot * baseDir;

            // Si dispara múltiples bombas, desplaza ligeramente el punto de caída
            Vector3 finalTargetPos = spawnOrigin + shotDir * Vector3.Distance(spawnOrigin, targetCenter);
            finalTargetPos.y = primaryTarget.position.y;

            GameObject projObj = Instantiate(weapon.data.projectilePrefab, spawnOrigin, Quaternion.LookRotation(shotDir));

            if (projObj.TryGetComponent(out IProjectile projectile))
            {
                float finalDamage = CalculateDamage(weapon.data.GetDamage(weapon.level), out bool isCrit);
                projectile.Initialize(new ProjectileInitData
                {
                    direction = shotDir,
                    targetPosition = finalTargetPos,
                    speed = weapon.data.projectileSpeed,
                    damage = finalDamage,
                    knockback = weapon.data.knockback,
                    areaMultiplier = areaMultiplier,
                    weaponLevel = weapon.level,
                    isCrit = isCrit,
                    targetLayer = enemyLayer
                });
            }
        }
    }

    private float CalculateDamage(float baseDmg, out bool isCrit)
    {
        float dmg = baseDmg * damageMultiplier;
        isCrit = false;

        if (critChance > 0f)
        {
            int guaranteedCrits = Mathf.FloorToInt(critChance);
            float remainderChance = critChance - guaranteedCrits;
            int totalCrits = guaranteedCrits + (Random.value < remainderChance ? 1 : 0);

            if (totalCrits > 0)
            {
                isCrit = true;
                dmg *= Mathf.Pow(critDamageMultiplier, totalCrits);
            }
        }

        return dmg;
    }
}
