#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Keep camera framing available as an explicit tool; scene reloads must not rewrite camera settings.
public static class MatrixWeaveViewFix
{
    [MenuItem("Tools/Joysticks/Fit Matrix Weave View")]
    public static void FitView()
    {
        var manager = Object.FindFirstObjectByType<LevelManager>();
        if (manager != null && Camera.main != null)
            IsometricBoardBuilder.FrameCameraForLevel(Camera.main, manager.CurrentLevel,
                manager.cellSpacing, manager.customBackdropTexture);
        ResetGameViewZoom();
        SceneView.RepaintAll();
        EditorApplication.QueuePlayerLoopUpdate();
    }

    public static int ResetGameViewZoom()
    {
        int adjusted = 0;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
        {
            if (window.GetType().Name != "GameView") continue;
            var field = window.GetType().GetField("m_ZoomArea", flags);
            var zoomArea = field != null ? field.GetValue(window) : null;
            if (zoomArea == null) continue;
            var scale = zoomArea.GetType().GetProperty("scale", flags);
            if (scale == null || !scale.CanWrite || scale.PropertyType != typeof(Vector2)) continue;
            scale.SetValue(zoomArea, Vector2.one);
            var translation = zoomArea.GetType().GetProperty("translation", flags);
            if (translation != null && translation.CanWrite && translation.PropertyType == typeof(Vector2))
                translation.SetValue(zoomArea, Vector2.zero);
            window.Repaint();
            adjusted++;
        }
        if (adjusted > 0) Debug.Log("Matrix Weave: Game-view zoom reset to1x; camera fitted with safe margins.");
        return adjusted;
    }
}
#endif
