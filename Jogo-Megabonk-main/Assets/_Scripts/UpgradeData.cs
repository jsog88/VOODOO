using UnityEngine;

public enum UpgradeCategory
{
    Weapon,
    Tome
}

public enum TomeStatType
{
    DamagePercent,
    AttackSpeedPercent,
    AreaPercent,
    CritChancePercent,
    CritDamagePercent,
    BonusProjectilesFlat,
    MoveSpeedPercent,
    MaxHpFlat,
    HpRegenFlat,
    ArmorFlat,
    XpGainPercent,
    MagnetRadiusFlat
}

public enum UpgradeRarity
{
    Comun,
    Raro,
    Epico,
    Legendario
}

[CreateAssetMenu(fileName = "NewUpgradeData", menuName = "Game/Progression/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [Header("Presentación de la Carta")]
    public string title = "Tomo de Fuerza";
    [TextArea(2, 4)] public string description = "Aumenta el daño de todas tus armas.";
    public Sprite icon;
    public UpgradeCategory category = UpgradeCategory.Tome;

    [Header("Si es Arma")]
    public WeaponData weaponData;

    [Header("Si es Tomo (Estadística Global)")]
    public TomeStatType statType = TomeStatType.DamagePercent;
    public float baseStatValue = 0.15f;

    public float GetScaledValue(UpgradeRarity rarity)
    {
        float mult = rarity switch
        {
            UpgradeRarity.Raro => 1.35f,
            UpgradeRarity.Epico => 1.75f,
            UpgradeRarity.Legendario => 2.3f,
            _ => 1f
        };
        return baseStatValue * mult;
    }
}
