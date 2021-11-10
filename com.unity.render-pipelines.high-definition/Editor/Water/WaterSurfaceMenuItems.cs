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

            // No caustics for oceans atm
            waterSurface.causticsIntensity = 0.0f;
        }

        [MenuItem("GameObject/Water Surface/River", priority = CoreUtils.Priorities.gameObjectMenuPriority)]
        static void CreateRiver(MenuCommand menuCommand)
        {
            // Create the holding game object
            var go = CoreEditorUtils.CreateGameObject("River", menuCommand.context);

            // Place it at origin and set its scale
            go.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
            go.transform.localScale = new Vector3(30.0f, 1.0f, 500.0f);

            // Add the water surface component
            var waterSurface = go.AddComponent<WaterSurface>();
            // Not an finite surface
            waterSurface.infinite = false;

            // The max patch size should be smaller
            waterSurface.waterMaxPatchSize = 200.0f;

            // The first band's amplitude is quite small
            waterSurface.waveAmplitude.x = 0.2f;

            // Rivers have no foam
            waterSurface.surfaceFoamIntensity = 0.0f;
            waterSurface.deepFoam = 0.0f;

            // Wind is quite light on rivers
            waterSurface.windSpeed = 20.0f;

            // No caustics for rivers ATM
            waterSurface.causticsIntensity = 0.0f;
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

            // We only need 2 bands
            waterSurface.highBandCount = false;

            // The max patch size should be smaller
            waterSurface.waterMaxPatchSize = 20.0f;

            // The two bands have very little amplitude
            waterSurface.waveAmplitude.x = 0.1f;
            waterSurface.waveAmplitude.y = 0.15f;

            // Scattering & transparency data
            waterSurface.transparentColor = new Color(0.45f, 0.65f, 0.85f);
            waterSurface.maxAbsorptionDistance = 10.0f;
            waterSurface.scatteringColor = new Color(0.25f, 0.70f, 1.0f);

            // No choppiness for the water
            waterSurface.choppiness = 0.0f;

            // Pools have no foam
            waterSurface.surfaceFoamIntensity = 0.0f;
            waterSurface.deepFoam = 0.0f;

            // Wind is quite light on rivers
            waterSurface.windSpeed = 30.0f;

            // Setup caustics for pools
            waterSurface.causticsIntensity = 0.75f;
            waterSurface.causticsTiling = 2.0f;
            waterSurface.causticsSpeed = 1.0f;
            waterSurface.causticsPlaneOffset = 1.5f;
        }
    }
}
