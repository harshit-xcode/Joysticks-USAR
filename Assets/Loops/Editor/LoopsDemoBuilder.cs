using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Loops.Editor
{
    public static class LoopsDemoBuilder
    {
        const string ScenePath = "Assets/Loops/Scenes/LoopsDemo.unity";

        [MenuItem("Loops/Create or Rebuild Demo Level")]
        public static void CreateDemo()
        {
            if (File.Exists(ScenePath) && !Application.isBatchMode &&
                !EditorUtility.DisplayDialog("Rebuild demo?", "This replaces LoopsDemo.unity. Your other scenes are unaffected.", "Rebuild", "Cancel")) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory("Assets/Loops/Scenes");
            Directory.CreateDirectory("Assets/Loops/Materials");
            AssetDatabase.Refresh();
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Loops/Resources/LoopsPanel.asset");
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, "Assets/Loops/Resources/LoopsPanel.asset");
            }
            panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("LoopsTheme");
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1440, 900);
            EditorUtility.SetDirty(panel);
            DemoLevelFactory.Populate(panel, Material);
            PlayerSettings.companyName = "Joysticks USAR";
            PlayerSettings.productName = "Loops Robot Lab";
            PlayerSettings.defaultScreenWidth = 1440;
            PlayerSettings.defaultScreenHeight = 900;
            EditorSettings.serializationMode = SerializationMode.ForceText;
            EditorSceneManager.SaveScene(SceneManagerActive(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("LOOPS_DEMO_CREATED: " + ScenePath);
        }

        static UnityEngine.SceneManagement.Scene SceneManagerActive() => UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        static UnityEngine.Material Material(string name, Color color, bool emission = false)
        {
            string path = "Assets/Loops/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
            if (material == null)
            {
                material = new UnityEngine.Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * .6f);
            }
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
