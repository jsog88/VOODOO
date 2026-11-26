using System;
using System.Collections.Generic;
using UnityEngine;

public struct RolledUpgradeOption
{
    public UpgradeData data;
    public UpgradeRarity rarity;
    public string formattedTitle;
    public string formattedDescription;
}

public class ExperienceManager : MonoBehaviour
{
    [Header("Progresión")]
    public int currentLevel = 1;
    public float currentXp = 0f;
    public float xpToNextLevel = 50f;
    public float xpGrowthMultiplier = 1.28f;

    [Header("Estadísticas de Recolección")]
    public float xpGainMultiplier = 1f;
    public float magnetRadius = 4.5f;

    [Header("Pool de Mejoras Disponibles")]
    public List<UpgradeData> availableUpgrades = new List<UpgradeData>();

    public event Action<float, float, int> OnXpChanged;
    public event Action<List<RolledUpgradeOption>> OnLevelUpChoicesReady;
    public event Action OnUpgradeApplied;

    private WeaponManager _weaponManager;
    private PlayerController _playerController;
    private PlayerHealth _playerHealth;
    private CamaraTerceraPersona _thirdPersonCam;

    private void Awake()
    {
        _weaponManager = GetComponent<WeaponManager>();
        _playerController = GetComponent<PlayerController>();
        _playerHealth = GetComponent<PlayerHealth>();
        _thirdPersonCam = FindFirstObjectByType<CamaraTerceraPersona>();
    }

    private void Start()
    {
        OnXpChanged?.Invoke(currentXp, xpToNextLevel, currentLevel);
    }

    public void AddExperience(int baseAmount)
    {
        if (_playerHealth != null && _playerHealth.IsDead) return;

        currentXp += baseAmount * xpGainMultiplier;

        if (currentXp >= xpToNextLevel)
        {
            LevelUp();
        }
        else
        {
            OnXpChanged?.Invoke(currentXp, xpToNextLevel, currentLevel);
        }
    }

    private void LevelUp()
    {
        currentXp -= xpToNextLevel;
        currentLevel++;
        xpToNextLevel = Mathf.Round(xpToNextLevel * xpGrowthMultiplier);

        OnXpChanged?.Invoke(currentXp, xpToNextLevel, currentLevel);

        List<RolledUpgradeOption> choices = RollUpgradeOptions(3);
        if (choices.Count == 0) return;

        Time.timeScale = 0f;
        if (_thirdPersonCam != null) _thirdPersonCam.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        OnLevelUpChoicesReady?.Invoke(choices);
    }

    private List<RolledUpgradeOption> RollUpgradeOptions(int count)
    {
        List<UpgradeData> validPool = new List<UpgradeData>();

        foreach (UpgradeData upg in availableUpgrades)
        {
            if (upg == null) continue;

            if (upg.category == UpgradeCategory.Weapon)
            {
                if (upg.weaponData == null || _weaponManager == null) continue;

                bool alreadyOwned = _weaponManager.equippedWeapons.Exists(w => w.data == upg.weaponData);
                if (alreadyOwned || _weaponManager.equippedWeapons.Count < _weaponManager.maxWeaponSlots)
                {
                    validPool.Add(upg);
                }
            }
            else
            {
                validPool.Add(upg);
            }
        }

        List<RolledUpgradeOption> results = new List<RolledUpgradeOption>();
        for (int i = 0; i < count && validPool.Count > 0; i++)
        {
            int idx = UnityEngine.Random.Range(0, validPool.Count);
            UpgradeData chosen = validPool[idx];
            validPool.RemoveAt(idx);

            UpgradeRarity rarity = RollRarity();
            results.Add(BuildOptionPresentation(chosen, rarity));
        }

        return results;
    }

    private UpgradeRarity RollRarity()
    {
        float roll = UnityEngine.Random.value;
        if (roll < 0.05f) return UpgradeRarity.Legendario;
        if (roll < 0.20f) return UpgradeRarity.Epico;
        if (roll < 0.50f) return UpgradeRarity.Raro;
        return UpgradeRarity.Comun;
    }

    private RolledUpgradeOption BuildOptionPresentation(UpgradeData upg, UpgradeRarity rarity)
    {
        if (upg.category == UpgradeCategory.Weapon && _weaponManager != null)
        {
            EquippedWeapon existing = _weaponManager.equippedWeapons.Find(w => w.data == upg.weaponData);
            int nextLvl = existing != null ? existing.level + 1 : 1;
            string tag = existing != null ? $"Nivel {nextLvl}" : "¡Nueva Arma!";

            return new RolledUpgradeOption
            {
                data = upg,
                rarity = UpgradeRarity.Comun,
                formattedTitle = $"{upg.weaponData.weaponName} ({tag})",
                formattedDescription = upg.description
            };
        }

        float val = upg.GetScaledValue(rarity);
        string formattedVal = upg.statType switch
        {
            TomeStatType.MaxHpFlat => $"+{Mathf.RoundToInt(val)} HP",
            TomeStatType.ArmorFlat => $"+{Mathf.RoundToInt(val)} Armadura",
            TomeStatType.BonusProjectilesFlat => $"+{Mathf.Max(1, Mathf.RoundToInt(val))} Proyectil",
            TomeStatType.HpRegenFlat => $"+{val:F1} HP/s",
            TomeStatType.MagnetRadiusFlat => $"+{val:F1}m",
            _ => $"+{Mathf.RoundToInt(val * 100f)}%"
        };

        return new RolledUpgradeOption
        {
            data = upg,
            rarity = rarity,
            formattedTitle = $"{upg.title} [{rarity}]",
            formattedDescription = $"{upg.description} ({formattedVal})"
        };
    }

    public void SelectUpgrade(RolledUpgradeOption chosenOption)
    {
        ApplyUpgrade(chosenOption);
        OnUpgradeApplied?.Invoke();

        if (currentXp >= xpToNextLevel)
        {
            LevelUp();
            return;
        }

        Time.timeScale = 1f;
        if (_thirdPersonCam != null) _thirdPersonCam.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ApplyUpgrade(RolledUpgradeOption option)
    {
        UpgradeData upg = option.data;

        if (upg.category == UpgradeCategory.Weapon && _weaponManager != null && upg.weaponData != null)
        {
            EquippedWeapon existing = _weaponManager.equippedWeapons.Find(w => w.data == upg.weaponData);
            if (existing != null)
            {
                existing.level++;
            }
            else if (_weaponManager.equippedWeapons.Count < _weaponManager.maxWeaponSlots)
            {
                _weaponManager.equippedWeapons.Add(new EquippedWeapon { data = upg.weaponData, level = 1 });
            }
            return;
        }

        float val = upg.GetScaledValue(option.rarity);
        switch (upg.statType)
        {
            case TomeStatType.DamagePercent:
                if (_weaponManager != null) _weaponManager.damageMultiplier += val;
                break;
            case TomeStatType.AttackSpeedPercent:
                if (_weaponManager != null) _weaponManager.attackSpeedMultiplier += val;
                break;
            case TomeStatType.AreaPercent:
                if (_weaponManager != null) _weaponManager.areaMultiplier += val;
                break;
            case TomeStatType.CritChancePercent:
                if (_weaponManager != null) _weaponManager.critChance += val;
                break;
            case TomeStatType.CritDamagePercent:
                if (_weaponManager != null) _weaponManager.critDamageMultiplier += val;
                break;
            case TomeStatType.BonusProjectilesFlat:
                if (_weaponManager != null) _weaponManager.bonusProjectiles += Mathf.Max(1, Mathf.RoundToInt(val));
                break;
            case TomeStatType.MoveSpeedPercent:
                if (_playerController != null) _playerController.moveSpeed *= (1f + val);
                break;
            case TomeStatType.MaxHpFlat:
                if (_playerHealth != null) _playerHealth.IncreaseMaxHp(Mathf.Round(val));
                break;
            case TomeStatType.HpRegenFlat:
                if (_playerHealth != null) _playerHealth.hpRegenPerSecond += val;
                break;
            case TomeStatType.ArmorFlat:
                if (_playerHealth != null) _playerHealth.armor += Mathf.Round(val);
                break;
            case TomeStatType.XpGainPercent:
                xpGainMultiplier += val;
                break;
            case TomeStatType.MagnetRadiusFlat:
                magnetRadius += val;
                break;
        }
    }
}