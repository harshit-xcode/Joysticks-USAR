using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class UIManager
{
    private GameObject levelReplayOverlay;

    public void ToggleLevelReplayDropdown()
    {
        if (levelReplayOverlay != null && levelReplayOverlay.activeSelf)
        {
            CloseLevelReplayDropdown();
            return;
        }
        var manager = LevelManager.Instance;
        if (manager == null || rootUiRect == null) return;
        manager.EnsureCampaignInitialized();
        if (levelReplayOverlay != null)
        {
            levelReplayOverlay.SetActive(false);
            Destroy(levelReplayOverlay);
        }

        // Full-screen click catcher closes the list when the player taps outside it.
        levelReplayOverlay = CreateUiObject("LevelReplayDropdown", rootUiRect, true, true);
        var overlayRect = levelReplayOverlay.GetComponent<RectTransform>();
        StretchFull(overlayRect);
        var overlayImage = levelReplayOverlay.GetComponent<Image>();
        overlayImage.color = Color.clear;
        var outsideButton = levelReplayOverlay.GetComponent<Button>();
        outsideButton.targetGraphic = overlayImage;
        outsideButton.transition = Selectable.Transition.None;
        outsideButton.onClick.AddListener(CloseLevelReplayDropdown);

        var list = CreateUiObject("ClearedLevelList", overlayRect, true).GetComponent<RectTransform>();
        list.anchorMin = list.anchorMax = new Vector2(1f, 1f);
        list.pivot = Vector2.one;
        list.anchoredPosition = new Vector2(-144f, -58f);
        list.sizeDelta = new Vector2(340f, 44f + manager.campaignLevels.Count * 36f);
        list.GetComponent<Image>().color = new Color(.055f, .09f, .14f, .98f);
        var title = CreateStyledText(list, "ReplayTitle", "LEVELS — REPLAY / CONTINUE", 13f,
            new Color(.4f, .9f, 1f), TextAlignmentOptions.MidlineLeft);
        PositionReplayRow(title.rectTransform, 8f, 28f);
        title.margin = new Vector4(10, 0, 10, 0);

        for (int i = 0; i < manager.campaignLevels.Count; i++)
        {
            int index = i;
            bool cleared = manager.IsLevelSolved(index);
            bool canContinue = !cleared && index == manager.HighestUnlockedLevel - 1 && index != manager.currentLevelIndex;
            bool selectable = cleared || canContinue;
            var row = CreateUiObject("ReplayLevel_" + (index + 1), list, true, true);
            PositionReplayRow(row.GetComponent<RectTransform>(), 38f + index * 36f, 32f);
            var image = row.GetComponent<Image>();
            image.color = selectable ? new Color(.12f, .25f, .31f) : new Color(.09f, .12f, .17f);
            string name = manager.campaignLevels[index].title ?? "";
            string prefix = $"L{index + 1}. ";
            if (name.StartsWith(prefix, System.StringComparison.Ordinal)) name = name.Substring(prefix.Length);
            string state = cleared ? "CLEARED" : canContinue ? "CONTINUE" : index == manager.currentLevelIndex ? "CURRENT" : "LOCKED";
            var label = CreateStyledText(row.transform, "LevelLabel", $"{index + 1}. {name}   [{state}]", 13f,
                selectable ? new Color(.8f, .96f, 1f) : new Color(.46f, .51f, .58f), TextAlignmentOptions.MidlineLeft);
            label.margin = new Vector4(10, 0, 10, 0);
            label.enableAutoSizing = true;
            label.fontSizeMin = 11f;
            label.fontSizeMax = 13f;
            var button = row.GetComponent<Button>();
            button.targetGraphic = image;
            button.interactable = selectable;
            button.onClick.AddListener(() =>
            {
                // Check saved completion again at selection time, including programmatic invocations.
                if (manager.SelectAvailableLevel(index)) CloseLevelReplayDropdown();
            });
        }
        levelReplayOverlay.transform.SetAsLastSibling();
    }

    private static void PositionReplayRow(RectTransform rect, float top, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, 1f);
        rect.offsetMin = new Vector2(8f, -top - height);
        rect.offsetMax = new Vector2(-8f, -top);
    }

    public void CloseLevelReplayDropdown()
    {
        if (levelReplayOverlay != null) levelReplayOverlay.SetActive(false);
    }
}
