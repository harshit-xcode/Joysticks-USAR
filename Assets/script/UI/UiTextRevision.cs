using UnityEngine;

// Records which revision of the built-in UI wording a LoopLabRootUI was built with.
// Scene copies from an older revision still pass on their layout and colors, but not
// their text or font style, so updated wording is not overwritten by stale scene text.
[DisallowMultipleComponent]
public class UiTextRevision : MonoBehaviour
{
    public const int Current = 2;
    public int revision;

    public static bool IsCurrent(Transform root)
    {
        var marker = root != null ? root.GetComponent<UiTextRevision>() : null;
        return marker != null && marker.revision == Current;
    }
}
