using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public enum StageUpgrade
{
    MaxHealth,
    Heal,
    AttackDamage,
    AttackSpeed,
    MoveSpeed,
    Dash
}

public class StageRewardPanel : MonoBehaviour
{
    [SerializeField] private Sprite[] cardArtwork;
    [SerializeField] private TMP_FontAsset titleFont;

    private PlayerHealth player;
    private Action onChosen;
    private float previousTimeScale;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;
    private bool showing;

    public static void Offer(StageRewardPanel prefab, PlayerHealth player, Action onChosen = null)
    {
        if (player == null || player.IsDead)
            return;

        Instantiate(prefab).Show(player, onChosen);
    }

    private void Show(PlayerHealth recipient, Action continuation)
    {
        player = recipient;
        onChosen = continuation;
        previousTimeScale = Time.timeScale;
        previousCursorVisible = Cursor.visible;
        previousCursorLock = Cursor.lockState;
        showing = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var layout = (RectTransform)transform;
        var background = AddElement("Backdrop", layout).gameObject.AddComponent<Image>();
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.offsetMin = background.rectTransform.offsetMax = Vector2.zero;
        background.color = new Color(.026f, .034f, .068f, .92f);

        var title = AddElement("Title", layout).gameObject.AddComponent<TextMeshProUGUI>();
        title.rectTransform.anchoredPosition = new Vector2(0, 225);
        title.rectTransform.sizeDelta = new Vector2(880, 62);
        title.font = titleFont;
        title.text = "CHOOSE 1 UPGRADE";
        title.fontSize = 34;
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 3;
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;

        var options = (StageUpgrade[])Enum.GetValues(typeof(StageUpgrade));
        Button first = null;
        for (int i = 0; i < 3; i++)
        {
            int pick = Random.Range(i, options.Length);
            (options[i], options[pick]) = (options[pick], options[i]);
            var button = AddCard(layout, options[i], (i - 1) * 289);
            if (i == 0)
                first = button;
        }

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    private Button AddCard(RectTransform parent, StageUpgrade upgrade, float x)
    {
        var card = AddElement(upgrade.ToString(), parent);
        card.anchoredPosition = new Vector2(x, -15);
        card.sizeDelta = new Vector2(262, 365);

        var picture = card.gameObject.AddComponent<Image>();
        picture.sprite = cardArtwork[(int)upgrade];
        picture.preserveAspect = true;

        var button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = picture;
        var colors = button.colors;
        colors.highlightedColor = new Color(.86f, .95f, 1f);
        colors.selectedColor = new Color(.93f, .98f, 1f);
        button.colors = colors;
        button.onClick.AddListener(() => Choose(upgrade));
        return button;
    }

    private static RectTransform AddElement(string name, RectTransform parent)
    {
        var element = new GameObject(name, typeof(RectTransform));
        element.layer = parent.gameObject.layer;
        var rect = (RectTransform)element.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private void Choose(StageUpgrade upgrade)
    {
        if (!showing)
            return;

        player.ApplyUpgrade(upgrade);
        RestoreGameplay();
        gameObject.SetActive(false);
        Destroy(gameObject);
        onChosen?.Invoke();
    }

    private void RestoreGameplay()
    {
        showing = false;
        Time.timeScale = previousTimeScale;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }

    private void OnDestroy()
    {
        if (showing)
            RestoreGameplay();
    }
}
