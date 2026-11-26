using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public Image healthFillImage;
    public TextMeshProUGUI healthText;

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged += UpdateHealthBar;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateHealthBar;
    }

    private void Start()
    {
        if (playerHealth != null)
            UpdateHealthBar(playerHealth.CurrentHp, playerHealth.maxHp);
    }

    private void UpdateHealthBar(float current, float max)
    {
        if (healthFillImage != null)
            healthFillImage.fillAmount = max > 0f ? current / max : 0f;

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }
}
