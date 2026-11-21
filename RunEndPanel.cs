using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RunEndPanel : MonoBehaviour
{
    [SerializeField] private Font thaiFont;
    [SerializeField] private Sprite roundedPanel;
    [SerializeField] private Sprite[] statIcons;
    [SerializeField] private Sprite[] cardArtwork;

    private static readonly Color TextColor = new(.98f, .95f, 1f);
    private static readonly Color Muted = new(.78f, .73f, .85f);
    private static readonly Color Purple = new(.67f, .44f, .97f);
    private static readonly Color PanelFill = new(.064f, .039f, .105f);
    private const float Width = 960;
    private const float Height = 626;

    private TMP_FontAsset font;
    private RectTransform frame;

    public void Show(PlayerHealth player, bool victorious, Action restart)
    {
        font = TMP_FontAsset.CreateFontAsset(thaiFont);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var root = (RectTransform)transform;
        var backdrop = DrawImage(root, "Backdrop", Vector2.zero, Vector2.zero, new Color(.015f, .011f, .026f, .98f)).rectTransform;
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
        frame = Framed(root, "Run Summary", Vector2.zero, new Vector2(Width, Height), Purple, new Color(.036f, .023f, .061f));
        var summary = Framed(frame, "Result", new Vector2(0, 171), new Vector2(854, 209), new Color(.31f, .19f, .44f), PanelFill);

        var heading = AddText(summary, "End Heading", "END", new Vector2(-235, 52), new Vector2(275, 75), 68, TextColor);
        heading.fontStyle = FontStyles.Bold;
        var result = victorious ? "ชนะบอสสุดท้าย" : $"แพ้ที่{StageName(player.gameObject.scene.name)}";
        AddText(summary, "Outcome", result, new Vector2(-235, -1), new Vector2(275, 38), 24,
            victorious ? new Color(.87f, .68f, 1f) : new Color(1f, .54f, .66f));
        AddText(summary, "Time Label", "เวลาเล่น", new Vector2(-235, -41), new Vector2(275, 29), 18, Muted);
        AddText(summary, "Elapsed Time", FormatTime(player.PlayTime), new Vector2(-235, -70), new Vector2(275, 38), 30, TextColor);

        DrawImage(summary, "Summary Divider", new Vector2(-55, 0), new Vector2(2, 158), new Color(.24f, .15f, .33f));
        var attack = player.GetComponent<SwordAttack>();
        var movement = player.GetComponent<PlayerMovement>();
        AddStat(summary, 0, 56, "พลังชีวิตที่เหลือ", $"{Number(Mathf.CeilToInt(player.CurrentHealth))} / {Number(Mathf.CeilToInt(player.maxHealth))}");
        AddStat(summary, 1, 0, "พลังโจมตี", Number(attack.attackDamage));
        AddStat(summary, 2, -56, "ความเร็วเคลื่อนที่", Number(movement.moveSpeed));

        AddText(frame, "Upgrades Heading", "ตัวช่วยที่เลือก", new Vector2(0, 38), new Vector2(560, 45), 29, TextColor);
        var detail = AddText(frame, "Upgrade Details", "", new Vector2(0, -211), new Vector2(854, 40), 19, Muted, TextAlignmentOptions.Center);
        var picks = player.SelectedUpgrades;
        if (picks.Count == 0)
            AddText(frame, "No Upgrades", "ยังไม่ได้เลือกตัวช่วย", new Vector2(0, -73), new Vector2(530, 44), 22, Muted, TextAlignmentOptions.Center);
        else
        {
            for (int i = 0; i < picks.Count; i++)
                AddCard(picks[i], (i - (picks.Count - 1) * .5f) * 174, detail);
        }

        var restartFrame = DrawImage(frame, "Restart", new Vector2(0, -270), new Vector2(192, 46), new Color(.32f, .16f, .51f), roundedPanel);
        var restartButton = MakeButton(restartFrame);
        restartButton.onClick.AddListener(() =>
        {
            restartButton.interactable = false;
            restart();
        });
        AddText(restartFrame.rectTransform, "Restart Label", "เล่นใหม่", Vector2.zero, new Vector2(186, 41), 23, TextColor, TextAlignmentOptions.Center);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
        FitFrame();
    }

    private void LateUpdate() => FitFrame();

    private void FitFrame()
    {
        if (frame == null)
            return;

        Vector2 size = ((RectTransform)transform).rect.size;
        float scale = Mathf.Min(1f, size.x / (Width + 32), size.y / (Height + 28));
        frame.localScale = Vector3.one * scale;
    }

    private void AddStat(RectTransform parent, int icon, float y, string label, string value)
    {
        var image = DrawImage(parent, label + " Icon", new Vector2(-3, y), new Vector2(33, 33), Color.white, statIcons[icon]);
        image.preserveAspect = true;
        AddText(parent, label, label, new Vector2(132, y), new Vector2(194, 44), 21, Muted);
        AddText(parent, label + " Value", value, new Vector2(312, y), new Vector2(174, 44), 24, TextColor, TextAlignmentOptions.Right);
    }

    private void AddCard(PlayerHealth.UpgradePick pick, float x, TextMeshProUGUI detail)
    {
        var artwork = DrawImage(frame, pick.upgrade.ToString(), new Vector2(x, -87), new Vector2(133, 180), Color.white, cardArtwork[(int)pick.upgrade]);
        artwork.preserveAspect = true;
        AddText(frame, "Card Stage", StageName(pick.stage), new Vector2(x, -184), new Vector2(158, 32), 17, Muted, TextAlignmentOptions.Center);
        var button = MakeButton(artwork);
        button.onClick.AddListener(() =>
        {
            detail.text = $"{StageName(pick.stage)} · {UpgradeName(pick.upgrade)} {Number(pick.before)} เป็น {Number(pick.after)}";
        });
    }

    private RectTransform Framed(RectTransform parent, string label, Vector2 position, Vector2 size, Color border, Color fill)
    {
        var edge = DrawImage(parent, label, position, size, border, roundedPanel).rectTransform;
        return DrawImage(edge, "Surface", Vector2.zero, size - Vector2.one * 4f, fill, roundedPanel).rectTransform;
    }

    private static RectTransform Element(RectTransform parent, string label, Vector2 position, Vector2 size)
    {
        var child = new GameObject(label, typeof(RectTransform));
        child.layer = parent.gameObject.layer;
        var rect = (RectTransform)child.transform;
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Image DrawImage(RectTransform parent, string name, Vector2 position, Vector2 size, Color color, Sprite sprite = null)
    {
        var image = Element(parent, name, position, size).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        if (sprite != null && sprite.border.sqrMagnitude > 0)
            image.type = Image.Type.Sliced;
        return image;
    }

    private static Button MakeButton(Image graphic)
    {
        graphic.raycastTarget = true;
        var button = graphic.gameObject.AddComponent<Button>();
        button.targetGraphic = graphic;
        var colors = button.colors;
        colors.highlightedColor = new Color(.86f, .76f, 1f);
        colors.selectedColor = new Color(.92f, .84f, 1f);
        button.colors = colors;
        return button;
    }

    private TextMeshProUGUI AddText(RectTransform parent, string name, string text, Vector2 position, Vector2 size, float fontSize, Color color,
        TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var label = Element(parent, name, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = color;
        label.text = text;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return label;
    }

    private static string Number(float value) => value.ToString("#,0.##", CultureInfo.InvariantCulture);

    private static string FormatTime(double playTime)
    {
        long seconds = (long)playTime;
        return $"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}";
    }

    private static string StageName(string stage) => stage switch
    {
        "SlumLevel" => "ตรอกสลัม",
        "CityDistrict" => "ย่านในเมือง",
        "FactoryEntrance" => "ทางเข้าโรงงาน",
        "FactoryRooftop" => "ดาดฟ้าโรงงาน",
        _ => stage
    };

    private static string UpgradeName(StageUpgrade upgrade) => upgrade switch
    {
        StageUpgrade.MaxHealth => "พลังชีวิตสูงสุด",
        StageUpgrade.Heal => "พลังชีวิตที่เหลือ",
        StageUpgrade.AttackDamage => "พลังโจมตี",
        StageUpgrade.AttackSpeed => "ตัวคูณความเร็วโจมตี",
        StageUpgrade.MoveSpeed => "ความเร็วเคลื่อนที่",
        StageUpgrade.Dash => "จำนวนชาร์จ Dash",
        _ => upgrade.ToString()
    };

    private void OnDestroy()
    {
        if (font == null)
            return;

        foreach (var atlas in font.atlasTextures)
            Destroy(atlas);
        Destroy(font.material);
        Destroy(font);
    }
}
