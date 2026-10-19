// UIManager.SceneLayout.cs
// The node-lab UI is rebuilt from code on every load so each level gets the correct
// buttons, palette cards and level selector. Edits made to LoopLabRootUI in the scene
// (layout, colors, sprites, fonts, text, visibility, added components and objects) are
// copied onto the freshly built UI so they show up in Play Mode. Gameplay wiring
// (button handlers, cards, per-level state) always comes from code.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class UIManager
{
    // Created at runtime or no longer used by the game; never restored from the scene.
    private static readonly HashSet<string> RuntimeOnlyUiNames = new HashSet<string>
    {
        "LevelReplayDropdown",
        "ProgramFailurePanel",
    };

    // Shown and hidden by game state, so the visibility saved in the scene is ignored.
    private static readonly HashSet<string> StateDrivenUiNames = new HashSet<string>
    {
        "CameraTunerPanel",
        "VictoryBannerOverlay",
    };

    private static bool IsSceneLayoutSkipped(Transform t)
    {
        // Command cards are per level; restoring saved ones would spawn the wrong nodes.
        return t.GetComponent<NodeCardUI>() != null || RuntimeOnlyUiNames.Contains(t.name);
    }

    private static void ApplySceneUiLayout(Transform sceneRoot, Transform builtRoot)
    {
        var sceneIndex = IndexUiTree(sceneRoot);
        var builtIndex = IndexUiTree(builtRoot);
        bool copyText = UiTextRevision.IsCurrent(sceneRoot);

        CopyUiAppearance(sceneRoot, builtRoot, false, copyText);
        var addedInScene = new List<KeyValuePair<string, Transform>>();
        foreach (var pair in sceneIndex)
        {
            if (builtIndex.TryGetValue(pair.Key, out Transform target))
                CopyUiAppearance(pair.Value, target, true, copyText);
            else
                addedInScene.Add(pair);
        }

        // Objects added in the scene move across whole, keeping their own components and
        // Inspector-assigned events. Only the topmost added object is moved; children follow.
        foreach (var pair in addedInScene)
        {
            string parentKey = pair.Key.Substring(0, pair.Key.LastIndexOf('/'));
            Transform parent = parentKey.Length == 0 ? builtRoot
                : builtIndex.TryGetValue(parentKey, out Transform p) ? p : null;
            if (parent == null) continue;
            int sibling = pair.Value.GetSiblingIndex();
            pair.Value.SetParent(parent, false);
            pair.Value.SetSiblingIndex(Mathf.Min(sibling, parent.childCount - 1));
        }
    }

    // Keys are name paths with an occurrence index, so same-named siblings stay distinct.
    private static Dictionary<string, Transform> IndexUiTree(Transform root)
    {
        var map = new Dictionary<string, Transform>();
        IndexUiChildren(root, "", map);
        return map;
    }

    private static void IndexUiChildren(Transform parent, string parentKey, Dictionary<string, Transform> map)
    {
        var seen = new Dictionary<string, int>();
        foreach (Transform child in parent)
        {
            if (IsSceneLayoutSkipped(child)) continue;
            seen.TryGetValue(child.name, out int n);
            seen[child.name] = n + 1;
            string key = parentKey + "/" + child.name + "#" + n;
            map[key] = child;
            IndexUiChildren(child, key, map);
        }
    }

    private static void CopyUiAppearance(Transform src, Transform dst, bool copyActive, bool copyText)
    {
        if (src is RectTransform s && dst is RectTransform d)
        {
            d.anchorMin = s.anchorMin;
            d.anchorMax = s.anchorMax;
            d.pivot = s.pivot;
            d.anchoredPosition = s.anchoredPosition;
            d.sizeDelta = s.sizeDelta;
        }
        dst.localRotation = src.localRotation;
        dst.localScale = src.localScale;
        if (copyActive && !StateDrivenUiNames.Contains(src.name))
            dst.gameObject.SetActive(src.gameObject.activeSelf);

        var srcImage = src.GetComponent<Image>();
        var dstImage = dst.GetComponent<Image>();
        if (srcImage != null && dstImage != null)
        {
            dstImage.sprite = srcImage.sprite;
            dstImage.type = srcImage.type;
            dstImage.preserveAspect = srcImage.preserveAspect;
            dstImage.color = srcImage.color;
        }

        var srcText = src.GetComponent<TextMeshProUGUI>();
        var dstText = dst.GetComponent<TextMeshProUGUI>();
        if (srcText != null && dstText != null && copyText)
        {
            // Labels driven by the game (level name, status, CAM mode) are rewritten after this.
            dstText.text = srcText.text;
            dstText.font = srcText.font;
            dstText.fontStyle = srcText.fontStyle;
            dstText.fontSize = srcText.fontSize;
            dstText.enableAutoSizing = srcText.enableAutoSizing;
            dstText.fontSizeMin = srcText.fontSizeMin;
            dstText.fontSizeMax = srcText.fontSizeMax;
            dstText.color = srcText.color;
            dstText.alignment = srcText.alignment;
            dstText.margin = srcText.margin;
        }

        var srcButton = src.GetComponent<Button>();
        var dstButton = dst.GetComponent<Button>();
        if (srcButton != null && dstButton != null)
        {
            // Visual states only; click handlers stay the ones the game wires up.
            dstButton.transition = srcButton.transition;
            dstButton.colors = srcButton.colors;
            dstButton.spriteState = srcButton.spriteState;
        }

        // Built-in UI components added in the scene (Outline, Shadow, LayoutElement, ...) are
        // carried over. Game scripts are not, so no gameplay behaviour is added or duplicated.
        foreach (var behaviour in src.GetComponents<MonoBehaviour>())
        {
            if (behaviour == null) continue;
            System.Type type = behaviour.GetType();
            string ns = type.Namespace ?? "";
            if (!ns.StartsWith("UnityEngine") && !ns.StartsWith("TMPro")) continue;
            if (dst.GetComponent(type) != null) continue;
            var copy = dst.gameObject.AddComponent(type) as MonoBehaviour;
            if (copy != null) JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(behaviour), copy);
        }
    }
}
