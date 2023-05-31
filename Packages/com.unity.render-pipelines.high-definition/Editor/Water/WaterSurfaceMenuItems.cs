using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering
{
    static class WaterSurfaceMenuItems
    {
        [MenuItem("GameObject/Water/Surface/Ocean Sea or Lake", priority = 13)]
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

        [MenuItem("GameObject/Water/Surface/River", priority = 15)]
        static void CreateRiver(MenuCommand menuCommand)
        {
            // Create the holding game object
            var go = CoreEditorUtils.CreateGameObject("River", menuCommand.context);

            // Set a default scale
            go.transform.localScale = new Vector3(150.0f, 1.0f, 20.0f);

            // Add the water surface component
            var waterSurface = go.AddComponent<WaterSurface>();
            WaterSurfacePresets.ApplyWaterRiverPreset(waterSurface);
        }

        [MenuItem("GameObject/Water/Surface/Pool", priority = 14)]
        static void CreatePool(MenuCommand menuCommand)
        {
            // Create the holding game object
            var go = CoreEditorUtils.CreateGameObject("Pool", menuCommand.context);

            // Set a default scale
            go.transform.localScale = new Vector3(10.0f, 1.0f, 10.0f);

            // Add the water surface component
            var waterSurface = go.AddComponent<WaterSurface>();
            WaterSurfacePresets.ApplyWaterPoolPreset(waterSurface);

            // Box collider
            var boxCollider = go.AddComponent<BoxCollider>();
            waterSurface.volumeBounds = boxCollider;
            boxCollider.center = new Vector3(0, -2, 0);
            boxCollider.size = new Vector3(1, 5, 1);
            boxCollider.isTrigger = true;
        }

        [MenuItem("GameObject/Water/Excluder", priority = 13)]
        static void CreateWaterExcluder(MenuCommand menuCommand)
        {
            Transform targetParent = null;
            GameObject toReplace = null;
            bool replace = false;
            // If an object was selected at the moment of the creation we have a different execution path
            if (Selection.activeObject != null)
            {
                var gameObject = (GameObject)Selection.activeObject;
                if (gameObject != null)
                {
                    toReplace = gameObject;
                    targetParent = gameObject.transform.parent;
                    replace = true;
                }
            }

            // Create the parent game object
            var go = CoreEditorUtils.CreateGameObject("Water Excluder", menuCommand.context);
            if (replace)
                go.transform.SetParent(targetParent);

            // Add the water excluder component
            var waterExclusion = go.AddComponent<WaterExcluder>();

            // Create the child excluder
            waterExclusion.m_ExclusionRenderer = new GameObject("Water Excluder Renderer", typeof(MeshFilter), typeof(MeshRenderer));
            waterExclusion.m_ExclusionRenderer.hideFlags = HideFlags.NotEditable;
            waterExclusion.m_ExclusionRenderer.transform.SetParent(go.transform);
            waterExclusion.m_ExclusionRenderer.transform.localPosition = Vector3.zero;
            waterExclusion.m_ExclusionRenderer.transform.localRotation = Quaternion.identity;
            waterExclusion.m_ExclusionRenderer.transform.localScale = Vector3.one;

            // Assign the material
            var excluderMeshRenderer = waterExclusion.m_ExclusionRenderer.GetComponent<MeshRenderer>();
            excluderMeshRenderer.sharedMaterial = HDRenderPipeline.currentAsset.renderPipelineResources.materials.waterExclusionMaterial;
            excluderMeshRenderer.rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.Off;

            // If an object was selected and it had a mesh filter, assign it
            Mesh targetMesh = null;
            if (replace)
            {
                MeshFilter filter = null;
                if (toReplace.TryGetComponent<MeshFilter>(out filter))
                {
                    var excluderMeshFilter = waterExclusion.m_ExclusionRenderer.GetComponent<MeshFilter>();
                    targetMesh = filter.sharedMesh;
                    toReplace.SetActive(false);
                }
            }
            else
                targetMesh = Resources.GetBuiltinResource<Mesh>("Plane.fbx");

            waterExclusion.SetExclusionMesh(targetMesh);
        }

        [MenuItem("GameObject/Water/Deformer/Sphere", priority = 17)]
        static void CreateWaterSphereDeformer(MenuCommand menuCommand)
        {
            // Create the holding game object
            var go = CoreEditorUtils.CreateGameObject("Water Sphere Deformer", menuCommand.context);
            go.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            // Add the water surface component
            var waterDeformer = go.AddComponent<WaterDeformer>();

            // Set the parameters
            waterDeformer.type = WaterDeformerType.Sphere;
            WaterDeformerPresets.ApplyWaterSphereDeformerPreset(waterDeformer);
        }

        [MenuItem("GameObject/Water/Deformer/Box", priority = 14)]
        static void CreateWaterBoxDeformer(MenuCommand menuCommand)
        {
            // Create the holding game object
            var go = CoreEditorUtils.CreateGameObject("Water Box Deformer", menuCommand.context);
            go.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            // Add the water surface component
            var waterDeformer = go.AddComponent<WaterDeformer>();

            // Set the parameters
            waterDeformer.type = WaterDeformerType.Box;
            WaterDeformerPresets.ApplyWaterBoxDeformerPreset(waterDeformer);
        }

        [MenuItem("GameObject/Water/Deformer/Shore Wave", priority = 16)]
        static void CreateWaterWaveDeformer(MenuCommand menuCommand)
        {
            // Create the holding game object
            var go = CoreEditorUtils.CreateGameObject("Water Wave Deformer", menuCommand.context);
            go.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            // Add the water surface component
            var waterDeformer = go.AddComponent<WaterDeformer>();

            // Set the parameters
            waterDeformer.type = WaterDeformerType.ShoreWave;
            WaterDeformerPresets.ApplyWaterShoreWaveDeformerPreset(waterDeformer);
        }

        [MenuItem("GameObject/Water/Deformer/Bow Wave", priority = 15)]
        static void CreateBowWaveDeformer(MenuCommand menuCommand)
        {
            // Create the holding game object
            var go = CoreEditorUtils.CreateGameObject("Water Bow Wave Deformer", menuCommand.context);
            go.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            // Add the water surface component
            var waterDeformer = go.AddComponent<WaterDeformer>();

            // Set the parameters
            waterDeformer.type = WaterDeformerType.BowWave;
            WaterDeformerPresets.ApplyWaterBowWaveDeformerPreset(waterDeformer);
        }

        [MenuItem("GameObject/Water/Deformer/Texture", priority = 18)]
        static void CreateTextureDeformer(MenuCommand menuCommand)
        {
            // Create the holding game object
            var go = CoreEditorUtils.CreateGameObject("Water Texture Deformer", menuCommand.context);
            go.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            // Add the water surface component
            var waterDeformer = go.AddComponent<WaterDeformer>();

            // Set the parameters
            waterDeformer.type = WaterDeformerType.Texture;
            WaterDeformerPresets.ApplyWaterTextureDeformerPreset(waterDeformer);
        }

        [MenuItem("GameObject/Water/Foam Generator", priority = 14)]
        static void CreateFoamGenerator(MenuCommand menuCommand)
        {
            // Create the holding game object
            var go = CoreEditorUtils.CreateGameObject("Water Foam Generator", menuCommand.context);
            go.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            // Add the water surface component
            var foamGenerator = go.AddComponent<WaterFoamGenerator>();

            // Set the parameters
            foamGenerator.regionSize = new Vector2(10f, 10.0f);
        }
    }
}
