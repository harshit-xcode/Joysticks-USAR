#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Older scenes may contain the former level-selector/audio HUD. Run this repair manually
// so opening or reloading a scene never changes authored UI behind the user's back.
public static class LevelSelectorEditModeSync
{
    [MenuItem("Tools/Joysticks/Repair Legacy Edit-Mode HUD")]
    public static void Sync()
    {
        if (Application.isBatchMode) return;
        if (!EditorUtility.DisplayDialog(
                "Repair Legacy HUD",
                "This will change legacy HUD objects in the active scene. Continue only if you want to apply this repair.",
                "Repair HUD", "Cancel")) return;

        // Also hide banners serialized in older scenes, without rebuilding the command board.
        foreach (var rect in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (rect.name == "HeaderBanner" && rect.gameObject.activeSelf)
                rect.gameObject.SetActive(false);

        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        var ui = Object.FindFirstObjectByType<UIManager>();
        var levelManager = Object.FindFirstObjectByType<LevelManager>();
        if (ui == null || levelManager == null) return;

        Transform selector = null;
        foreach (var rect in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (rect.name == "TopRightControls") { selector = rect; break; }
        }
        if (selector == null) return; // No Edit-Mode HUD built in this scene; Play Mode builds its own.

        bool legacyControls = false;
        foreach (Transform child in selector)
            if (child.name.StartsWith("LevelBtn_") || child.name == "MuteButton") legacyControls = true;
        if (!legacyControls) return;

        levelManager.EnsureCampaignInitialized();
        ui.EnsureSplitScreenNodeLabUI();
        ui.ConfigureForLevel(levelManager.CurrentLevel, levelManager.currentLevelIndex, levelManager.campaignLevels.Count);
        Debug.Log("LevelSelectorEditModeSync: Removed legacy level-selector/audio controls from the Edit-Mode HUD.");
    }
}
#endif
