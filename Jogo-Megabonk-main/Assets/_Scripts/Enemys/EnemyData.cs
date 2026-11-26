using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Game/Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identidad")]
    public string enemyName = "Esqueleto";
    public GameObject prefab;

    [Header("Estadísticas Base")]
    public float maxHp = 40f;
    public float moveSpeed = 4.5f;
    public float contactDamage = 10f;
    public float knockbackResistance = 1f; // >1 reduce empuje, 0 = inmune
    public int xpDropAmount = 10;

    [Header("Comportamiento Aéreo")]
    public bool isFlying = false;
    public float spawnHeightOffset = 4f; // Altura inicial sobre el suelo al aparecer

    [Header("Configuración de Jefe (Boss)")]
    public bool isBoss = false;
    public GameObject bossGemPrefab;
}
