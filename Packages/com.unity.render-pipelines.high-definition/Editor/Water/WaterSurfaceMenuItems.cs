using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering
{
    static class WaterSurfaceMenuItems
    {
        [MenuItem("GameObject/Water Surface/Ocean Sea or Lake", priority = CoreUtils.Priorities.gameObjectMenuPriority)]
        static void CreateOcean(MenuCommand menuCommand)
        {
            // Create the holding game object
            var go = CoreEditorUtils.CreateGameObject("Ocean", menuCommand.context);

            // Place it at origin and set its scale
            go.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            // Add the water surface component
            var waterSurface = go.AddComponent<WaterSurface>();
            WaterSurfacePresets.ApplyWaterOceanPreset(waterSurface);

        }

        [MenuItem("GameObject/Water Surface/River", priority = CoreUtils.Priorities.gameObjectMenuPriority)]
        static void CreateRiver(MenuCommand menuCommand)
        {
            // Create the holding game object
            var go = CoreEditorUtils.CreateGameObject("River", menuCommand.context);

            // Place it at origin and set its scale
            go.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
            go.transform.localScale = new Vector3(150.0f, 1.0f, 20.0f);

            // Add the water surface component
            var waterSurface = go.AddComponent<WaterSurface>();
            WaterSurfacePresets.ApplyWaterRiverPreset(waterSurface);
        }

        [MenuItem("GameObject/Water Surface/Pool", priority = CoreUtils.Priorities.gameObjectMenuPriority)]
        static void CreatePool(MenuCommand menuCommand)
        {
            // Create the holding game object
            var go = CoreEditorUtils.CreateGameObject("Pool", menuCommand.context);

            // Place it at origin and set its scale
            go.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
            go.transform.localScale = new Vector3(10.0f, 1.0f, 10.0f);

            // Add the water surface component
            var waterSurface = go.AddComponent<WaterSurface>();
            WaterSurfacePresets.ApplyWaterPoolPreset(waterSurface);

            // Box collider
            var boxCollider = go.AddComponent<BoxCollider>();
            waterSurface.volumeBounds = boxCollider;
            boxCollider.center = new Vector3(0, -2, 0);
            boxCollider.size = new Vector3(1, 5, 1);
        }
    }
}
