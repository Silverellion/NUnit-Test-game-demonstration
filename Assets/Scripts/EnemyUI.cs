using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays the enemy's current HP bar, block badge, and animated intent badge
/// with Sword / Shield icons from Assets/Assets/AdventureGameIconPack.
/// </summary>
public class EnemyUI : MonoBehaviour
{
    [Header("HP Bar")]
    public Slider hpSlider;
    public Image hpFillImage;
    public TextMeshProUGUI hpText;

    [Header("Block")]
    public GameObject blockContainer;
    public TextMeshProUGUI blockText;

    [Header("Intent")]
    public GameObject intentContainer;
    public Image intentIcon;
    public TextMeshProUGUI intentText;

    [Header("Name")]
    public TextMeshProUGUI nameText;

    [Header("Sprite")]
    public Image enemySprite;

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
        if (intentText != null)
        {
            UIFonts.ApplyBody(intentText, semiBold: true, autoSize: true, minSize: 11f, maxSize: 18f);
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

    public void ShowIntent(string action, int value)
    {
        if (intentContainer == null) return;

        intentContainer.SetActive(true);
        Image bgImg = intentContainer.GetComponent<Image>();

        if (action == "Attack")
        {
            if (bgImg != null)
            {
                bgImg.sprite = UISprites.IntentBadgeAttack;
                bgImg.type = Image.Type.Sliced;
                bgImg.color = Color.white;
            }

            if (intentIcon != null)
            {
                Sprite sword = UISprites.GetSwordIcon();
                if (sword != null)
                {
                    intentIcon.sprite = sword;
                    intentIcon.color = Color.white;
                    intentIcon.preserveAspect = true;
                    intentIcon.gameObject.SetActive(true);
                }
            }

            if (intentText != null)
            {
                intentText.text = $"ATK {value}";
                intentText.color = new Color(1f, 0.95f, 0.95f);
            }
        }
        else if (action == "Defend")
        {
            if (bgImg != null)
            {
                bgImg.sprite = UISprites.IntentBadgeDefend;
                bgImg.type = Image.Type.Sliced;
                bgImg.color = Color.white;
            }

            if (intentIcon != null)
            {
                Sprite shield = UISprites.GetShieldIcon();
                if (shield != null)
                {
                    intentIcon.sprite = shield;
                    intentIcon.color = Color.white;
                    intentIcon.preserveAspect = true;
                    intentIcon.gameObject.SetActive(true);
                }
            }

            if (intentText != null)
            {
                intentText.text = $"DEF {value}";
                intentText.color = new Color(0.95f, 0.95f, 1f);
            }
        }
        else
        {
            if (intentText != null)
                intentText.text = action;
        }

        // Pop-in bounce animation
        RectTransform rt = intentContainer.GetComponent<RectTransform>();
        if (rt != null)
        {
            UITween.PopIn(rt, 0.35f);
        }
    }

    public void HideIntent()
    {
        if (intentContainer != null && intentContainer.activeSelf)
        {
            RectTransform rt = intentContainer.GetComponent<RectTransform>();
            if (rt != null)
            {
                UITween.Scale(rt, Vector3.zero, 0.16f, EaseType.EaseInQuad, () =>
                {
                    intentContainer.SetActive(false);
                });
            }
            else
            {
                intentContainer.SetActive(false);
            }
        }
    }
}
