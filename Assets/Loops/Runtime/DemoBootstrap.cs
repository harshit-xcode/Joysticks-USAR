using UnityEngine;
using UnityEngine.UIElements;

namespace Loops
{
    // The committed preview scene works without first running the editor generation menu.
    // Use Loops > Create or Rebuild Demo Level for a scene with editable tile objects.
    public sealed class DemoBootstrap : MonoBehaviour
    {
        public Material stone, lava, goal, wood, robotMaterial, face, grass;
        PanelSettings panel;

        void Awake()
        {
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("LoopsTheme");
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1440, 900);
            DemoLevelFactory.Populate(panel, GetMaterial);
        }

        Material GetMaterial(string materialName, Color color, bool emission)
        {
            switch (materialName)
            {
                case "Stone": return stone;
                case "Lava": return lava;
                case "Goal": return goal;
                case "Wood": return wood;
                case "Robot": return robotMaterial;
                case "Face": return face;
                default: return grass;
            }
        }

        void OnDestroy() { if (panel != null) Destroy(panel); }
    }
}
