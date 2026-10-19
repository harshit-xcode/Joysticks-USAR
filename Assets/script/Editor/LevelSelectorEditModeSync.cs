#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// The Node Lab HUD seen in Edit Mode is whatever Tools/Joysticks/Setup last built.
// When the campaign gains levels, that stale HUD keeps the old P1..Pn selector until Play Mode rebuilds it.
// After every script reload / scene open, rebuild the Edit-Mode HUD if its level buttons no longer match the catalog.
// The scene is not marked dirty or saved; run Tools/Joysticks/Setup to persist the HUD.
[InitializeOnLoad]
public static class LevelSelectorEditModeSync
{
    static LevelSelectorEditModeSync()
    {
        EditorApplication.delayCall += Sync;
        EditorSceneManager.sceneOpened += (_, __) => EditorApplication.delayCall += Sync;
    }

    private static void Sync()
    {
        if (Application.isBatchMode) return;

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

        int buttons = 0;
        foreach (Transform child in selector)
            if (child.name.StartsWith("LevelBtn_")) buttons++;
        if (buttons == LevelCampaignCatalog.CampaignLevelCount) return;

        levelManager.EnsureCampaignInitialized();
        ui.EnsureSplitScreenNodeLabUI();
        ui.ConfigureForLevel(levelManager.CurrentLevel, levelManager.currentLevelIndex, levelManager.campaignLevels.Count);
        Debug.Log($"LevelSelectorEditModeSync: Edit-Mode level selector refreshed to P1..P{LevelCampaignCatalog.CampaignLevelCount}.");
    }
}
#endif
