using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour, InDamage
{
    [Header("Estadísticas Base (Escalables por Tomos/Ítems)")]
    public float maxHp = 100f;
    public float hpRegenPerSecond = 0.5f;
    public float armor = 0f; // Fórmula: daño * (100 / (100 + armor))
    public float invulnerabilityDuration = 0.5f;

    [Header("Feedback Visual")]
    public Renderer meshRenderer;
    public Color hitFlashColor = Color.red;

    public float CurrentHp { get; private set; }
    public bool IsDead { get; private set; }

    public event Action<float, float> OnHealthChanged;
    public event Action OnPlayerDied;

    private float _invulnerabilityTimer;
    private MaterialPropertyBlock _propBlock;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private Color _originalColor = Color.white;
    private Coroutine _flashRoutine;

    private void Awake()
    {
        CurrentHp = maxHp;

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
        OnHealthChanged?.Invoke(CurrentHp, maxHp);
    }

    private void Update()
    {
        if (IsDead)
        {
            // Reinicio rápido con R mientras armamos la pantalla de Game Over
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }

        if (_invulnerabilityTimer > 0f)
        {
            _invulnerabilityTimer -= Time.deltaTime;
        }

        // Regeneración pasiva de vida
        if (hpRegenPerSecond > 0f && CurrentHp < maxHp)
        {
            Heal(hpRegenPerSecond * Time.deltaTime);
        }
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 knockbackForce, bool isCrit)
    {
        if (IsDead || _invulnerabilityTimer > 0f) return;

        // Reducción porcentual progresiva (nunca llega a 100% de inmunidad)
        float damageMultiplier = 100f / (100f + Mathf.Max(0f, armor));
        float finalDamage = Mathf.Max(1f, amount * damageMultiplier);

        CurrentHp = Mathf.Clamp(CurrentHp - finalDamage, 0f, maxHp);
        _invulnerabilityTimer = invulnerabilityDuration;

        OnHealthChanged?.Invoke(CurrentHp, maxHp);
        TriggerHitFlash();

        if (CurrentHp <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        CurrentHp = Mathf.Clamp(CurrentHp + amount, 0f, maxHp);
        OnHealthChanged?.Invoke(CurrentHp, maxHp);
    }

    public void IncreaseMaxHp(float bonusHp, bool healDifference = true)
    {
        maxHp += bonusHp;
        if (healDifference)
        {
            CurrentHp = Mathf.Clamp(CurrentHp + bonusHp, 0f, maxHp);
        }
        OnHealthChanged?.Invoke(CurrentHp, maxHp);
    }

    private void TriggerHitFlash()
    {
        if (meshRenderer == null) return;
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashCoroutine());
    }

    private IEnumerator FlashCoroutine()
    {
        SetColor(hitFlashColor);
        yield return new WaitForSeconds(0.12f);
        SetColor(_originalColor);
    }

    private void SetColor(Color color)
    {
        if (meshRenderer == null) return;
        meshRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(BaseColorId, color);
        meshRenderer.SetPropertyBlock(_propBlock);
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;

        if (TryGetComponent(out PlayerController controller))
            controller.enabled = false;

        if (TryGetComponent(out WeaponManager weaponManager))
            weaponManager.enabled = false;

        CamaraTerceraPersona cam = FindFirstObjectByType<CamaraTerceraPersona>();
        if (cam != null)
            cam.enabled = false;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        OnPlayerDied?.Invoke();
    }
}
