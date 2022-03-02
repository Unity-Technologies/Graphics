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
            waterSurface.geometryType = WaterGeometryType.Infinite;
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

            waterSurface.geometryType = WaterGeometryType.Quad;
            waterSurface.timeMultiplier = 1.0f;
            waterSurface.refractionColor = new Color(0, 0.3f, 0.6f);
            waterSurface.maxRefractionDistance = 1.0f;
            waterSurface.absorptionDistance = 1.0f;
            waterSurface.scatteringColor = new Color(0.0f, 0.3f, 0.25f);
            waterSurface.causticsIntensity = 0.1f;
            waterSurface.causticsTiling = 0.8f;
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

            // The max patch size should be smaller
            waterSurface.maximumWaveHeight = 0.5f;

            // The two bands have very little amplitude
            waterSurface.highFrequencyBands = false;

            // Scattering & transparency data
            waterSurface.refractionColor = new Color(0, 0.3f, 0.6f);
            waterSurface.maxRefractionDistance = 0.5f;
            waterSurface.absorptionDistance = 10.0f;
            waterSurface.scatteringColor = new Color(0.0f, 0.40f, 0.75f);

            // Setup caustics for pools
            waterSurface.causticsIntensity = 0.4f;
            waterSurface.causticsTiling = 1.5f;
            waterSurface.causticsSpeed = 0.0f;
        }
    }
}
