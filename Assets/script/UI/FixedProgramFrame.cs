using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class FixedProgramFrame
{
    public static void PositionRow(RectTransform rect, float y)
    {
        if (rect == null) return;
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, 1);
        rect.anchoredPosition = new Vector2(0, y);
        rect.sizeDelta = new Vector2(-28, 50);
        rect.localScale = Vector3.one;
    }

    public static void AddBoundary(RectTransform parent, string objectName, string caption, float y)
    {
        var existing = parent.Find(objectName);
        GameObject obj;
        if (existing == null)
        {
            obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
        }
        else obj = existing.gameObject;
        obj.SetActive(true);
        PositionRow(obj.transform as RectTransform, y);
        var image = obj.GetComponent<Image>();
        if (image != null)
        {
            image.color = caption == "START" ? new Color(.12f, .55f, .3f) : new Color(.7f, .2f, .2f);
            image.raycastTarget = true;
        }
        var label = obj.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null)
        {
            var textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(obj.transform, false);
            label = textObject.GetComponent<TextMeshProUGUI>();
        }
        label.text = caption + " (fixed)";
        label.fontSize = 22;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
    }
}
