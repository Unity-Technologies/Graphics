using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering
{
    static class WaterSurfaceMenuItems
    {
        [MenuItem("GameObject/Water Surface/Ocean", priority = CoreUtils.Priorities.gameObjectMenuPriority)]
        static void CreateOcean(MenuCommand menuCommand)
        {
            // Create the holding game object
            var go = CoreEditorUtils.CreateGameObject("Ocean", menuCommand.context);

            // Place it at origin and set its scale
            go.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            // Add the water surface component
            var waterSurface = go.AddComponent<WaterSurface>();

            // Set the various parameters
            waterSurface.infinite = true;
            waterSurface.windSpeed = 50.0f;
            waterSurface.choppiness = 3.0f;
            waterSurface.windAffectCurrent = 0.2f;
            waterSurface.causticsIntensity = 0.0f;
        }

        [MenuItem("GameObject/Water Surface/River", priority = CoreUtils.Priorities.gameObjectMenuPriority)]
        static void CreateRiver(MenuCommand menuCommand)
        {
            // Create the holding game object
            var go = CoreEditorUtils.CreateGameObject("River", menuCommand.context);

            // Place it at origin and set its scale
            go.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
            go.transform.localScale = new Vector3(500.0f, 1.0f, 30.0f);

            // Add the water surface component
            var waterSurface = go.AddComponent<WaterSurface>();

            waterSurface.infinite = false;
            waterSurface.geometryType = WaterSurface.WaterGeometryType.Quad;
            waterSurface.waterMaxPatchSize = 70.0f;
            waterSurface.amplitude = new Vector2(0.5f, 1.0f);
            waterSurface.choppiness = 1.0f;
            waterSurface.timeMultiplier = 1.0f;
            waterSurface.refractionColor = new Color(0, 0.3f, 0.6f);
            waterSurface.maxRefractionDistance = 1.0f;
            waterSurface.maxAbsorptionDistance = 1.0f;
            waterSurface.scatteringColor = new Color(0.0f, 0.3f, 0.25f);
            waterSurface.windSpeed = 30.0f;
            waterSurface.causticsIntensity = 0.1f;
            waterSurface.causticsTiling = 0.8f;
            waterSurface.windAffectCurrent = 1.0f;
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

            // Not an finite surface
            waterSurface.infinite = false;

            // The max patch size should be smaller
            waterSurface.waterMaxPatchSize = 20.0f;

            // The two bands have very little amplitude
            waterSurface.highBandCount = false;
            waterSurface.amplitude.x = 1.0f;
            waterSurface.amplitude.y = 1.0f;

            // Scattering & transparency data
            waterSurface.refractionColor = new Color(0, 0.3f, 0.6f);
            waterSurface.maxRefractionDistance = 0.5f;
            waterSurface.maxAbsorptionDistance = 10.0f;
            waterSurface.scatteringColor = new Color(0.0f, 0.40f, 0.75f);

            // No choppiness for the water
            waterSurface.choppiness = 0.0f;

            // Wind is quite light on rivers
            waterSurface.windSpeed = 50.0f;

            // Setup caustics for pools
            waterSurface.causticsIntensity = 0.4f;
            waterSurface.causticsTiling = 1.5f;
            waterSurface.causticsSpeed = 0.0f;
            waterSurface.causticsPlaneOffset = 0.5f;
        }
    }
}
