using UnityEngine;
[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Game/Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identidad")]
    public string weaponName = "Hueso Mágico";
    public Sprite icon;

    [Header("Estadísticas Base")]
    public float baseDamage = 15f;
    public float baseCooldown = 1.2f;
    public float range = 18f;
    public int projectileCount = 1;
    public float projectileSpeed = 22f;
    public float knockback = 4f;

    [Header("Escalamiento por Nivel")]
    public float damagePerLevel = 5f;
    public float cooldownReductionPerLevel = 0.06f;
    public int extraProjectileEveryNLevels = 3;

    [Header("Prefab")]
    public GameObject projectilePrefab;

    public float GetDamage(int level) => baseDamage + damagePerLevel * (level - 1);

    public float GetCooldown(int level) =>
        Mathf.Max(0.15f, baseCooldown * Mathf.Pow(1f - cooldownReductionPerLevel, level - 1));

    public int GetProjectileCount(int level) =>
        projectileCount + ((level - 1) / Mathf.Max(1, extraProjectileEveryNLevels));
}
