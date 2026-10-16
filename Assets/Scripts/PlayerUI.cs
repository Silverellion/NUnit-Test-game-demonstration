using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays the player's HP bar, block, energy counters, and pile counts
/// with styled Cinzel typography and smooth UI pop-ins.
/// </summary>
public class PlayerUI : MonoBehaviour
{
    [Header("HP Bar")]
    public Slider hpSlider;
    public Image hpFillImage;
    public TextMeshProUGUI hpText;

    [Header("Block")]
    public GameObject blockContainer;
    public TextMeshProUGUI blockText;

    [Header("Energy")]
    public TextMeshProUGUI energyText;

    [Header("Name")]
    public TextMeshProUGUI nameText;

    [Header("Sprite")]
    public Image playerSprite;

    [Header("Pile Counters")]
    public TextMeshProUGUI drawPileText;
    public TextMeshProUGUI discardPileText;

    private int maxHP;

    public void Initialize(string name, int hp)
    {
        maxHP = hp;
        if (nameText != null)
        {
            nameText.text = name;
            UIFonts.ApplyBody(nameText, semiBold: true, autoSize: true, minSize: 11f, maxSize: 18f);
        }
        if (hpText != null)
        {
            UIFonts.ApplyBody(hpText, semiBold: true, autoSize: false);
        }
        if (blockText != null)
        {
            UIFonts.ApplyBody(blockText, semiBold: true, autoSize: false);
        }
        if (energyText != null)
        {
            UIFonts.ApplyTitle(energyText, autoSize: false);
        }
        if (drawPileText != null)
        {
            UIFonts.ApplyTitle(drawPileText, autoSize: false);
        }
        if (discardPileText != null)
        {
            UIFonts.ApplyTitle(discardPileText, autoSize: false);
        }
        UpdateHP(hp, 0);
    }

    public void UpdateHP(int currentHP, int block)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }

        if (hpText != null)
        {
            hpText.text = $"{currentHP}/{maxHP}";
        }

        // Color the HP bar based on percentage
        if (hpFillImage != null)
        {
            float pct = (float)currentHP / maxHP;
            if (pct > 0.5f)
                hpFillImage.color = Color.Lerp(new Color(0.9f, 0.7f, 0.1f), new Color(0.3f, 0.8f, 0.3f), (pct - 0.5f) * 2f);
            else
                hpFillImage.color = Color.Lerp(new Color(0.85f, 0.15f, 0.15f), new Color(0.9f, 0.7f, 0.1f), pct * 2f);
        }

        // Block display with pop-in
        if (blockContainer != null)
        {
            bool wasActive = blockContainer.activeSelf;
            blockContainer.SetActive(block > 0);
            if (block > 0 && !wasActive)
            {
                UITween.PopIn(blockContainer.GetComponent<RectTransform>(), 0.30f);
            }
            if (blockText != null)
                blockText.text = block.ToString();
        }
    }

    public void UpdateEnergy(int current, int max)
    {
        if (energyText != null)
        {
            energyText.text = $"{current}/{max}\n<size=12>ENERGY</size>";
        }
    }

    public void UpdatePiles(int drawCount, int discardCount)
    {
        if (drawPileText != null)
            drawPileText.text = drawCount.ToString();
        if (discardPileText != null)
            discardPileText.text = discardCount.ToString();
    }
}
