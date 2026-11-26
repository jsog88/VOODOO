using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class TomeGeneratorEditor
{
    [MenuItem("Herramientas/Generar y Asignar Todos los Tomos")]
    public static void GenerateAllTomes()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");

        string folderPath = "Assets/Data/Tomos";
        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder("Assets/Data", "Tomos");

        var tomeDefinitions = new List<(string fileName, string title, string desc, TomeStatType stat, float baseVal)>
        {
            ("Tomo_Fuerza", "Tomo de Fuerza", "Aumenta el daño global de todas tus armas.", TomeStatType.DamagePercent, 0.15f),
            ("Tomo_Celeridad", "Tomo de Celeridad", "Reduce el enfriamiento y acelera tus ataques.", TomeStatType.AttackSpeedPercent, 0.15f),
            ("Tomo_Expansion", "Tomo de Expansión", "Aumenta el tamaño de proyectiles y áreas de efecto.", TomeStatType.AreaPercent, 0.18f),
            ("Tomo_Precision", "Tomo de Precisión", "Aumenta la probabilidad de golpe crítico y Overcrit.", TomeStatType.CritChancePercent, 0.10f),
            ("Tomo_Letalidad", "Tomo de Letalidad", "Multiplica el daño infligido por golpes críticos.", TomeStatType.CritDamagePercent, 0.35f),
            ("Tomo_Duplicacion", "Tomo de Duplicación", "Añade proyectiles adicionales a todas tus armas.", TomeStatType.BonusProjectilesFlat, 1f),
            ("Tomo_Vitalidad", "Tomo de Vitalidad", "Aumenta tu vida máxima y te cura al instante.", TomeStatType.MaxHpFlat, 25f),
            ("Tomo_Regeneracion", "Tomo de Regeneración", "Recupera salud constantemente cada segundo.", TomeStatType.HpRegenFlat, 1.0f),
            ("Tomo_Coraza", "Tomo de Coraza", "Aumenta tu armadura para mitigar el daño recibido.", TomeStatType.ArmorFlat, 12f),
            ("Tomo_Agilidad", "Tomo de Agilidad", "Aumenta la velocidad de desplazamiento e impulso.", TomeStatType.MoveSpeedPercent, 0.12f),
            ("Tomo_Sabiduria", "Tomo de Sabiduría", "Aumenta la experiencia obtenida al recoger gemas.", TomeStatType.XpGainPercent, 0.18f),
            ("Tomo_Magnetismo", "Tomo de Magnetismo", "Amplía el radio de atracción del imán de gemas.", TomeStatType.MagnetRadiusFlat, 2.5f)
        };

        List<UpgradeData> createdTomes = new List<UpgradeData>();

        foreach (var def in tomeDefinitions)
        {
            string assetPath = $"{folderPath}/{def.fileName}.asset";
            UpgradeData asset = AssetDatabase.LoadAssetAtPath<UpgradeData>(assetPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<UpgradeData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            asset.title = def.title;
            asset.description = def.desc;
            asset.category = UpgradeCategory.Tome;
            asset.statType = def.stat;
            asset.baseStatValue = def.baseVal;

            EditorUtility.SetDirty(asset);
            createdTomes.Add(asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Asigna los 12 Tomos automáticamente al ExperienceManager de tu Player en la escena
        ExperienceManager xpManager = Object.FindFirstObjectByType<ExperienceManager>();
        if (xpManager != null)
        {
            Undo.RecordObject(xpManager, "Asignar Todos los Tomos");

            // Conserva las cartas de Armas que ya tenías y reemplaza los Tomos viejos por los 12 nuevos
            xpManager.availableUpgrades.RemoveAll(u => u == null || u.category == UpgradeCategory.Tome);
            xpManager.availableUpgrades.AddRange(createdTomes);

            EditorUtility.SetDirty(xpManager);
            Debug.Log($"¡Listo! Se crearon los {createdTomes.Count} Tomos en '{folderPath}' y se conectaron automáticamente a '{xpManager.gameObject.name}'.");
        }
        else
        {
            Debug.LogWarning($"Se crearon los {createdTomes.Count} Tomos en '{folderPath}', pero no se encontró un ExperienceManager activo en la escena.");
        }
    }
}
