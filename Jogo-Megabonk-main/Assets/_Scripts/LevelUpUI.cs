using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class UpgradeCardUI
{
    public Button cardButton;
    public Image rarityFrameImage; // El Image del botón principal (marco exterior)
    public Image cardArtImage;     // El Image que muestra la ilustración completa de la carta
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
}

public class LevelUpUI : MonoBehaviour
{
    [Header("Referencias")]
    public ExperienceManager experienceManager;
    public Sprite defaultCardArt; // Opcional: carta genérica para Tomos que aún no tengan dibujo

    [Header("Barra Superior de XP")]
    public Image xpFillImage;
    public TextMeshProUGUI levelText;

    [Header("Panel y Tarjetas de Subida de Nivel")]
    public GameObject levelUpPanel;
    public List<UpgradeCardUI> cards = new List<UpgradeCardUI>();

    private void OnEnable()
    {
        if (experienceManager != null)
        {
            experienceManager.OnXpChanged += UpdateXpBar;
            experienceManager.OnLevelUpChoicesReady += ShowUpgradeCards;
        }
    }

    private void OnDisable()
    {
        if (experienceManager != null)
        {
            experienceManager.OnXpChanged -= UpdateXpBar;
            experienceManager.OnLevelUpChoicesReady -= ShowUpgradeCards;
        }
    }

    private void Start()
    {
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);
    }

    private void UpdateXpBar(float currentXp, float maxXp, int level)
    {
        if (xpFillImage != null)
            xpFillImage.fillAmount = maxXp > 0f ? currentXp / maxXp : 0f;

        if (levelText != null)
            levelText.text = $"LVL {level}";
    }

    private void ShowUpgradeCards(List<RolledUpgradeOption> options)
    {
        if (levelUpPanel != null)
            levelUpPanel.SetActive(true);

        for (int i = 0; i < cards.Count; i++)
        {
            if (i < options.Count)
            {
                RolledUpgradeOption option = options[i];
                UpgradeCardUI card = cards[i];

                card.cardButton.gameObject.SetActive(true);

                Color rarityColor = GetRarityColor(option.rarity);

                if (card.titleText != null)
                {
                    card.titleText.text = option.formattedTitle;
                    card.titleText.color = option.rarity == UpgradeRarity.Comun ? Color.white : rarityColor;
                }

                if (card.descriptionText != null)
                    card.descriptionText.text = option.formattedDescription;

                if (card.cardArtImage != null)
                {
                    Sprite artToUse = option.data.icon != null ? option.data.icon : defaultCardArt;
                    card.cardArtImage.enabled = artToUse != null;
                    card.cardArtImage.sprite = artToUse;
                    card.cardArtImage.color = Color.white; // Mantiene intactos los colores originales del dibujo
                }

                if (card.rarityFrameImage != null)
                {
                    card.rarityFrameImage.color = rarityColor;
                }

                card.cardButton.onClick.RemoveAllListeners();
                card.cardButton.onClick.AddListener(() => OnCardClicked(option));
            }
            else
            {
                cards[i].cardButton.gameObject.SetActive(false);
            }
        }
    }

    private void OnCardClicked(RolledUpgradeOption chosen)
    {
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);

        experienceManager.SelectUpgrade(chosen);
    }

    private Color GetRarityColor(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Raro => new Color(0.30f, 0.65f, 1f),
            UpgradeRarity.Epico => new Color(0.75f, 0.35f, 1f),
            UpgradeRarity.Legendario => new Color(1f, 0.80f, 0.20f),
            _ => new Color(0.15f, 0.15f, 0.20f)
        };
    }
}
