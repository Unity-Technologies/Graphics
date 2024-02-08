using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using RenderingLayerMask = UnityEngine.RenderingLayerMask;

namespace UnityEditor.Rendering.HighDefinition
{
    using UnityObject = UnityEngine.Object;

    class HDRenderPipelineMenuItems
    {
        // Function used only to check performance of data with and without tessellation
        //[MenuItem("Internal/HDRP/Test/Remove tessellation materials (not reversible)")]
        static void RemoveTessellationMaterials()
        {
            var materials = Resources.FindObjectsOfTypeAll<Material>();

            var litShader = Shader.Find("HDRP/Lit");
            var layeredLitShader = Shader.Find("HDRP/LayeredLit");

            foreach (var mat in materials)
            {
                if (mat.shader.name == "HDRP/LitTessellation")
                {
                    mat.shader = litShader;
                    // We remove all keyword already present
                    CoreEditorUtils.RemoveMaterialKeywords(mat);
                    LitAPI.ValidateMaterial(mat);
                    EditorUtility.SetDirty(mat);
                }
                else if (mat.shader.name == "HDRP/LayeredLitTessellation")
                {
                    mat.shader = layeredLitShader;
                    // We remove all keyword already present
                    CoreEditorUtils.RemoveMaterialKeywords(mat);
                    LayeredLitAPI.ValidateMaterial(mat);
                    EditorUtility.SetDirty(mat);
                }
            }
        }

        [MenuItem("Edit/Rendering/Export HDRP Sky to Image", priority = CoreUtils.Sections.section2 + CoreUtils.Priorities.editMenuPriority + 2)]
        static void ExportSkyToImage()
        {
            var renderpipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (renderpipeline == null)
            {
                Debug.LogError("HDRenderPipeline is not instantiated.");
                return;
            }

            var result = renderpipeline.ExportSkyToTexture(Camera.main);
            if (result == null)
                return;

            // Encode texture into PNG
            byte[] bytes = result.EncodeToEXR(Texture2D.EXRFlags.CompressZIP);
            UnityObject.DestroyImmediate(result);

            string assetPath = EditorUtility.SaveFilePanel("Export Sky", "Assets", "SkyExport", "exr");
            if (!string.IsNullOrEmpty(assetPath))
            {
                File.WriteAllBytes(assetPath, bytes);
                AssetDatabase.Refresh();
            }
        }

        [MenuItem("GameObject/Light/Directional Moon Light", priority = 2)]
        static void CreateMoonLight(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = CoreEditorUtils.CreateGameObject("Directional Moon Light", parent);
            go.GetComponent<Transform>().eulerAngles = new Vector3(150, -30, 0);

            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;

            var hdLight = go.AddComponent<HDAdditionalLightData>();
            HDAdditionalLightData.InitDefaultHDAdditionalLightData(hdLight);

            hdLight.celestialBodyShadingSource = HDAdditionalLightData.CelestialBodyShadingSource.ReflectSunLight;
            hdLight.distance = 384400000;
            hdLight.diameterMultiplerMode = false;
            hdLight.diameterOverride = 3.0f;
            hdLight.flareSize = 0.0f;

            light.colorTemperature = 4100;
            light.intensity = 0.5f; // 0.5 lux is actually a bit more than max moon light intensity on a full moon

            if (GraphicsSettings.TryGetRenderPipelineSettings<HDRenderPipelineEditorTextures>(out var defaultRenderPipelineTextures))
            {
                hdLight.surfaceTexture = defaultRenderPipelineTextures.moonAlbedo;
            }
            else
            {
                Debug.LogWarning($"{go.name} is missing the {nameof(HDAdditionalLightData.surfaceTexture)} due to not being able to find {nameof(HDRenderPipelineEditorTextures.moonAlbedo)} Texture.");
            }
        }

        [MenuItem("GameObject/Volume/Sky and Fog Global Volume", priority = CoreUtils.Priorities.gameObjectMenuPriority + 1)]
        static void CreateSceneSettingsGameObject(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var settings = CoreEditorUtils.CreateGameObject("Sky and Fog Global Volume", parent);

            var profile = VolumeProfileFactory.CreateVolumeProfile(settings.scene, "Sky and Fog Settings");
            var visualEnv = VolumeProfileFactory.CreateVolumeComponent<VisualEnvironment>(profile, true, false);

            visualEnv.skyType.value = SkySettings.GetUniqueID<PhysicallyBasedSky>();
            visualEnv.skyAmbientMode.overrideState = false;
            VolumeProfileFactory.CreateVolumeComponent<PhysicallyBasedSky>(profile, false, false);
            var fog = VolumeProfileFactory.CreateVolumeComponent<Fog>(profile, false, true);
            fog.enabled.Override(true);
            fog.enableVolumetricFog.Override(true);

            var volume = settings.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        [MenuItem("Edit/Rendering/Materials/Upgrade HDRP Materials to Latest Version", priority = CoreUtils.Priorities.editMenuPriority)]
        internal static void UpgradeMaterials()
        {
            // Force reimport of all materials, this will upgrade the needed one and save the assets if needed
            MaterialReimporter.ReimportAllMaterials();
        }

        [MenuItem("Edit/Rendering/Materials/Generate Material Resources", priority = CoreUtils.Priorities.editMenuPriority)]
        internal static void GenerateLookUpTables()
        {
            var resources = new List<RenderTexture>();

            // Ask for each render pipeline material to build their look-ups
            HDUtils.GetRenderPipelineMaterialList().ForEach(material => material.BuildOffline(ref resources));

            // Write the resources to disk.
            resources.ForEach(resource => HDTextureUtilities.WriteTextureToAsset(resource, $"Assets/HDRPDefaultResources/Generated/{resource.name}.asset"));

            // Release
            resources.ForEach(RenderTexture.ReleaseTemporary);
        }

        [MenuItem("Edit/Rendering/Rendering Layers/Add HDRP Default Layer Mask to Loaded Mesh Renderers and Terrains", priority = CoreUtils.Priorities.editMenuPriority + 2)]
        internal static void UpgradeDefaultRenderingLayerMask()
        {
            var meshRenderers = Resources.FindObjectsOfTypeAll<MeshRenderer>();

            foreach (var mesh in meshRenderers)
            {
                Undo.RecordObject(mesh, "MeshRenderer Layer Mask update");
                mesh.renderingLayerMask |= RenderingLayerMask.defaultRenderingLayerMask;
                EditorUtility.SetDirty(mesh);
            }

            var terrains = Resources.FindObjectsOfTypeAll<Terrain>();

            foreach (var terrain in terrains)
            {
                Undo.RecordObject(terrain, "Terrain Layer Mask update");
                terrain.renderingLayerMask |= RenderingLayerMask.defaultRenderingLayerMask;
                EditorUtility.SetDirty(terrain);
            }
        }

        [MenuItem("Edit/Rendering/Rendering Layers/Add HDRP Default Layer Mask to Selected Mesh Renderers and Terrains", priority = CoreUtils.Priorities.editMenuPriority + 1)]
        internal static void UpgradeDefaultRenderingLayerMaskForSelection()
        {
            var selection = UnityEditor.Selection.objects;

            foreach (var obj in selection)
            {
                if (obj is GameObject)
                {
                    GameObject gameObj = obj as GameObject;
                    MeshRenderer mesh;
                    if (gameObj.TryGetComponent<MeshRenderer>(out mesh))
                    {
                        Undo.RecordObject(mesh, "MeshRenderer Layer Mask update");
                        mesh.renderingLayerMask |= RenderingLayerMask.defaultRenderingLayerMask;
                        EditorUtility.SetDirty(mesh);
                    }

                    Terrain terrain;
                    if (gameObj.TryGetComponent<Terrain>(out terrain))
                    {
                        Undo.RecordObject(terrain, "Terrain Layer Mask update");
                        terrain.renderingLayerMask |= RenderingLayerMask.defaultRenderingLayerMask;
                        EditorUtility.SetDirty(terrain);
                    }
                }
            }
        }

        static bool ContributesGI(GameObject go) => (GameObjectUtility.GetStaticEditorFlags(go) & StaticEditorFlags.ContributeGI) != 0;

        [MenuItem("Edit/Rendering/Global Illumination/Convert Selected Mesh Renderers to receive GI from Light Probes", priority = CoreUtils.Priorities.editMenuPriority + 1)]
        internal static void ConvertSelectedObjectsToAPV()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is GameObject)
                {
                    GameObject gameObj = obj as GameObject;
                    if (gameObj.TryGetComponent<MeshRenderer>(out var mesh) && ContributesGI(gameObj) && mesh.receiveGI == ReceiveGI.Lightmaps)
                    {
                        Undo.RecordObject(mesh, "MeshRenderer Receive GI update");
                        mesh.receiveGI = ReceiveGI.LightProbes;
                        EditorUtility.SetDirty(mesh);
                    }
                }
            }
        }

        [MenuItem("Edit/Rendering/Global Illumination/Convert Loaded Mesh Renderers to receive GI from Light Probes", priority = CoreUtils.Priorities.editMenuPriority + 1)]
        internal static void ConvertLoadedObjectsToAPV()
        {
            foreach (var mesh in Resources.FindObjectsOfTypeAll<MeshRenderer>())
            {
                if (ContributesGI(mesh.gameObject) && mesh.receiveGI == ReceiveGI.Lightmaps)
                {
                    Undo.RecordObject(mesh, "MeshRenderer Receive GI update");
                    mesh.receiveGI = ReceiveGI.LightProbes;
                    EditorUtility.SetDirty(mesh);
                }
            }
        }

        class DoCreateNewAsset<TAssetType> : ProjectWindowCallback.EndNameEditAction where TAssetType : ScriptableObject
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var newAsset = CreateInstance<TAssetType>();
                newAsset.name = Path.GetFileName(pathName);
                AssetDatabase.CreateAsset(newAsset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(newAsset);
                PostCreateAssetWork(newAsset);
            }

            protected virtual void PostCreateAssetWork(TAssetType asset) { }
        }

        class DoCreateNewAssetDiffusionProfileSettings : DoCreateNewAsset<DiffusionProfileSettings>
        {
            protected override void PostCreateAssetWork(DiffusionProfileSettings asset)
            {
                // Update the hash after that the asset was saved on the disk (hash requires the GUID of the asset)
                DiffusionProfileHashTable.UpdateDiffusionProfileHashNow(asset);
            }
        }

        [MenuItem("Assets/Create/Rendering/HDRP Diffusion Profile", priority = CoreUtils.Sections.section4 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority)]
        static void MenuCreateDiffusionProfile()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateNewAssetDiffusionProfileSettings>(), "New Diffusion Profile.asset", icon, null);
        }

        [MenuItem("Assets/Create/Shader/HDRP Custom FullScreen Pass")]
        static void MenuCreateCustomFullScreenPassShader()
        {
            string templatePath = $"{HDUtils.GetHDRenderPipelinePath()}/Editor/RenderPipeline/CustomPass/CustomPassFullScreenShader.template";
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "New FullScreen CustomPass.shader");
        }

        [MenuItem("Assets/Create/Shader/HDRP Custom Renderers Pass")]
        static void MenuCreateCustomRenderersPassShader()
        {
            string templatePath = $"{HDUtils.GetHDRenderPipelinePath()}/Editor/RenderPipeline/CustomPass/CustomPassRenderersShader.template";
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "New Renderers CustomPass.shader");
        }

        [MenuItem("Assets/Create/Rendering/HDRP C# Custom Pass", priority = CoreUtils.Sections.section6 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority)]
        static void MenuCreateCustomPassCSharpScript()
        {
            string templatePath = $"{HDUtils.GetHDRenderPipelinePath()}/Editor/RenderPipeline/CustomPass/CustomPassCSharpScript.template";
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "New Custom Pass.cs");
        }

        [MenuItem("Assets/Create/Rendering/HDRP C# Post Process Volume", priority = CoreUtils.Sections.section6 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority)]
        static void MenuCreateCSharpPostProcessVolume()
        {
            string templatePath = $"{HDUtils.GetHDRenderPipelinePath()}/Editor/PostProcessing/Templates/CustomPostProcessingVolume.template";
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "NewPostProcessVolume.cs");
        }

        [MenuItem("Assets/Create/Shader/HDRP Post Process", priority = CoreUtils.Sections.section1)]
        static void MenuCreatePostProcessShader()
        {
            string templatePath = $"{HDUtils.GetHDRenderPipelinePath()}/Editor/PostProcessing/Templates/CustomPostProcessingShader.template";
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "New Post Process Volume.shader");
        }

        //[MenuItem("Internal/HDRP/Add \"Additional Light-shadow Data\" (if not present)")]
        static void AddAdditionalLightData()
        {
            var lights = UnityObject.FindObjectsByType<Light>(FindObjectsSortMode.InstanceID);

            foreach (var light in lights)
            {
                // Do not add a component if there already is one.
                if (!light.TryGetComponent<HDAdditionalLightData>(out _))
                {
                    var hdLight = light.gameObject.AddComponent<HDAdditionalLightData>();
                    HDAdditionalLightData.InitDefaultHDAdditionalLightData(hdLight);
                }
            }
        }

        //[MenuItem("Internal/HDRP/Add \"Additional Camera Data\" (if not present)")]
        static void AddAdditionalCameraData()
        {
            var cameras = UnityObject.FindObjectsByType<Camera>(FindObjectsSortMode.InstanceID);

            foreach (var camera in cameras)
            {
                // Do not add a component if there already is one.
                if (!camera.TryGetComponent<HDAdditionalCameraData>(out _))
                    camera.gameObject.AddComponent<HDAdditionalCameraData>();
            }
        }

        // This script is a helper for the artists to re-synchronize all layered materials
        //[MenuItem("Internal/HDRP/Synchronize all Layered materials")]
        static void SynchronizeAllLayeredMaterial()
        {
            var materials = Resources.FindObjectsOfTypeAll<Material>();

            bool VCSEnabled = (UnityEditor.VersionControl.Provider.enabled && UnityEditor.VersionControl.Provider.isActive);

            foreach (var mat in materials)
            {
                if (mat.shader.name == "HDRP/LayeredLit" || mat.shader.name == "HDRP/LayeredLitTessellation")
                {
                    CoreEditorUtils.CheckOutFile(VCSEnabled, mat);
                    LayeredLitGUI.SynchronizeAllLayers(mat);
                    EditorUtility.SetDirty(mat);
                }
            }
        }

        // We now do this automatically when upgrading Material version, so not required anymore - keep it in case you want to manually do it
        // The goal of this script is to help maintenance of data that have already been produced but need to update to the latest shader code change.
        // In case the shader code have change and the inspector have been update with new kind of keywords we need to regenerate the set of keywords use by the material.
        // This script will remove all keyword of a material and trigger the inspector that will re-setup all the used keywords.
        // It require that the inspector of the material have a static function call that update all keyword based on material properties.
        // [MenuItem("Edit/Render Pipeline/HD Render Pipeline/Reset All Loaded High Definition Materials Keywords")]
        static void ResetAllMaterialKeywords()
        {
            try
            {
                ResetAllLoadedMaterialKeywords(string.Empty, 1, 0);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // Don't expose, ResetAllMaterialKeywordsInProjectAndScenes include it anyway
        //[MenuItem("Edit/Render Pipeline/HD Render Pipeline/Reset All Material Asset's Keywords (Materials in Project)")]
        static void ResetAllMaterialAssetsKeywords()
        {
            try
            {
                ResetAllMaterialAssetsKeywords(1, 0);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // We now do this automatically when upgrading Material version, so not required anymore - keep it in case you want to manually do it
        // [MenuItem("Edit/Render Pipeline/HD Render Pipeline/Reset All Project and Scene High Definition Materials Keywords")]
        static void ResetAllMaterialKeywordsInProjectAndScenes()
        {
            var openedScenes = new string[SceneManager.loadedSceneCount];
            for (var i = 0; i < openedScenes.Length; ++i)
                openedScenes[i] = SceneManager.GetSceneAt(i).path;

            bool VCSEnabled = (UnityEditor.VersionControl.Provider.enabled && UnityEditor.VersionControl.Provider.isActive);

            try
            {
                var scenes = AssetDatabase.FindAssets("t:Scene", new string[] { "Assets" });
                var scale = 1f / Mathf.Max(1, scenes.Length);
                for (var i = 0; i < scenes.Length; ++i)
                {
                    var scenePath = AssetDatabase.GUIDToAssetPath(scenes[i]);
                    var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                    CoreEditorUtils.CheckOutFile(VCSEnabled, sceneAsset);
                    EditorSceneManager.OpenScene(scenePath);

                    var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                    var description = string.Format("{0} {1}/{2} - ", sceneName, i + 1, scenes.Length);

                    if (ResetAllLoadedMaterialKeywords(description, scale, scale * i))
                    {
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    }
                }

                ResetAllMaterialAssetsKeywords(scale, scale * (scenes.Length - 1));
            }
            finally
            {
                EditorUtility.ClearProgressBar();

                if (openedScenes.Length > 0)
                {
                    EditorSceneManager.OpenScene(openedScenes[0]);
                    for (var i = 1; i < openedScenes.Length; i++)
                        EditorSceneManager.OpenScene(openedScenes[i], OpenSceneMode.Additive);
                }
            }
        }

        static void ResetAllMaterialAssetsKeywords(float progressScale, float progressOffset)
        {
            var matIds = AssetDatabase.FindAssets("t:Material", new string[] { "Assets" }); // do not include packages

            bool VCSEnabled = (UnityEditor.VersionControl.Provider.enabled && UnityEditor.VersionControl.Provider.isActive);

            for (int i = 0, length = matIds.Length; i < length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(matIds[i]);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                EditorUtility.DisplayProgressBar(
                    "Setup material asset's Keywords...",
                    string.Format("{0} / {1} materials cleaned.", i, length),
                    (i / (float)(length - 1)) * progressScale + progressOffset);

                CoreEditorUtils.CheckOutFile(VCSEnabled, mat);
                var h = Debug.unityLogger.logHandler;
                Debug.unityLogger.logHandler = new UnityContextualLogHandler(mat);
                HDShaderUtils.ResetMaterialKeywords(mat);
                Debug.unityLogger.logHandler = h;
            }
        }

        static bool ResetAllLoadedMaterialKeywords(string descriptionPrefix, float progressScale, float progressOffset)
        {
            var materials = Resources.FindObjectsOfTypeAll<Material>();

            bool VCSEnabled = (UnityEditor.VersionControl.Provider.enabled && UnityEditor.VersionControl.Provider.isActive);

            bool anyMaterialDirty = false; // Will be true if any material is dirty.

            for (int i = 0, length = materials.Length; i < length; i++)
            {
                EditorUtility.DisplayProgressBar(
                    "Setup materials Keywords...",
                    string.Format("{0}{1} / {2} materials cleaned.", descriptionPrefix, i, length),
                    (i / (float)(length - 1)) * progressScale + progressOffset);

                CoreEditorUtils.CheckOutFile(VCSEnabled, materials[i]);

                if (HDShaderUtils.ResetMaterialKeywords(materials[i]))
                {
                    anyMaterialDirty = true;
                }
            }

            return anyMaterialDirty;
        }

        [MenuItem("GameObject/Volume/Custom Pass", priority = CoreUtils.Sections.section2 + CoreUtils.Priorities.gameObjectMenuPriority + 1)]
        static void CreateGlobalVolume(MenuCommand menuCommand)
        {
            var go = CoreEditorUtils.CreateGameObject("Custom Pass", menuCommand.context);
            var volume = go.AddComponent<CustomPassVolume>();
            volume.isGlobal = true;
        }

        class UnityContextualLogHandler : ILogHandler
        {
            UnityObject m_Context;
            static readonly ILogHandler k_DefaultLogHandler = Debug.unityLogger.logHandler;

            public UnityContextualLogHandler(UnityObject context)
            {
                m_Context = context;
            }

            public void LogFormat(LogType logType, UnityObject context, string format, params object[] args)
            {
                k_DefaultLogHandler.LogFormat(LogType.Log, m_Context, "Context: {0} ({1})", m_Context, AssetDatabase.GetAssetPath(m_Context));
                k_DefaultLogHandler.LogFormat(logType, context, format, args);
            }

            public void LogException(Exception exception, UnityObject context)
            {
                k_DefaultLogHandler.LogFormat(LogType.Log, m_Context, "Context: {0} ({1})", m_Context, AssetDatabase.GetAssetPath(m_Context));
                k_DefaultLogHandler.LogException(exception, context);
            }
        }

        [MenuItem("Edit/Rendering/Check Scene Content for HDRP Ray Tracing", priority = CoreUtils.Sections.section2 + CoreUtils.Priorities.editMenuPriority + 3)]
        static void CheckSceneContentForRayTracing(MenuCommand menuCommand)
        {
            // Flag that holds
            bool generalErrorFlag = false;
            var rendererArray = UnityEngine.GameObject.FindObjectsByType<Renderer>(FindObjectsSortMode.InstanceID);
            var lodGroupArray = UnityEngine.GameObject.FindObjectsByType<LODGroup>(FindObjectsSortMode.InstanceID);
            List<Material> materialArray = new List<Material>(32);
            ReflectionProbe reflectionProbe = new ReflectionProbe();

            foreach (Renderer currentRenderer in rendererArray)
            {
                // If this is a reflection probe, we can ignore it.
                if (currentRenderer.gameObject.TryGetComponent<ReflectionProbe>(out reflectionProbe)) continue;

                // Get all the materials of the mesh renderer
                currentRenderer.GetSharedMaterials(materialArray);
                if (materialArray == null)
                {
                    Debug.LogWarning("The object " + currentRenderer.name + " has a null material array.");
                    generalErrorFlag = true;
                    continue;
                }

                // For every sub-mesh/sub-material let's build the right flags
                int numSubMeshes = 1;
                if (!(currentRenderer.GetType() == typeof(SkinnedMeshRenderer)))
                {
                    currentRenderer.TryGetComponent(out MeshFilter meshFilter);
                    if (meshFilter == null || meshFilter.sharedMesh == null)
                    {
                        Debug.LogWarning("The object " + currentRenderer.name + " has null meshfilter or mesh.");
                        generalErrorFlag = true;
                        continue;
                    }
                    numSubMeshes = meshFilter.sharedMesh.subMeshCount;
                }
                else
                {
                    SkinnedMeshRenderer skinnedMesh = (SkinnedMeshRenderer)currentRenderer;
                    if (skinnedMesh.sharedMesh == null)
                    {
                        Debug.LogWarning("The object " + currentRenderer.name + " has null mesh.");
                        generalErrorFlag = true;
                        continue;
                    }
                    numSubMeshes = skinnedMesh.sharedMesh.subMeshCount;
                }

                // Check the number of sub-meshes
                if (numSubMeshes >= 32)
                {
                    Debug.LogWarning("The object " + currentRenderer.name + " has more than 32 sub-meshes. Above this limit, the cutoff and double sided flags may not match the one defined in the materials.");
                    generalErrorFlag = true;
                    continue;
                }

                bool materialIsOnlyTransparent = true;
                bool hasTransparentSubMaterial = false;
                bool singleSided = true;
                bool hasSingleSided = false;

                for (int meshIdx = 0; meshIdx < numSubMeshes; ++meshIdx)
                {
                    // Initially we consider the potential mesh as invalid
                    if (materialArray.Count > meshIdx)
                    {
                        // Grab the material for the current sub-mesh
                        Material currentMaterial = materialArray[meshIdx];

                        // The material is transparent if either it has the requested keyword or is in the transparent queue range
                        if (currentMaterial != null)
                        {
                            bool materialIsTransparent = currentMaterial.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT")
                                || (HDRenderQueue.k_RenderQueue_Transparent.lowerBound <= currentMaterial.renderQueue
                                    && HDRenderQueue.k_RenderQueue_Transparent.upperBound >= currentMaterial.renderQueue);

                            // aggregate the transparency info
                            materialIsOnlyTransparent &= materialIsTransparent;
                            hasTransparentSubMaterial |= materialIsTransparent;

                            // Evaluate if it is single sided
                            bool doubleSided = currentMaterial.doubleSidedGI || currentMaterial.IsKeywordEnabled("_DOUBLESIDED_ON");

                            // Aggregate the double sided information
                            hasSingleSided |= !doubleSided;
                            singleSided &= !doubleSided;
                        }
                        else
                        {
                            Debug.LogWarning("The object " + currentRenderer.name + " has null material.");
                            generalErrorFlag = true;
                        }
                    }
                }

                if (!materialIsOnlyTransparent && hasTransparentSubMaterial)
                {
                    Debug.LogWarning("The object " + currentRenderer.name + " has both transparent and opaque sub-meshes. This may cause performance issues");
                    generalErrorFlag = true;
                }

                if (!singleSided && hasSingleSided)
                {
                    Debug.LogWarning("The object " + currentRenderer.name + " has both double sided and single sided sub-meshes. All materials will be considered double-sided for ray-traced effects.");
                    generalErrorFlag = true;
                }
            }

            //Check if one LOD is missing a renderer
            for (var i = 0; i < lodGroupArray.Length; i++)
            {
                // Grab the current LOD group
                LODGroup lodGroup = lodGroupArray[i];
                LOD[] lodArray = lodGroup.GetLODs();
                for (int lodIdx = 0; lodIdx < lodArray.Length; ++lodIdx)
                {
                    LOD currentLOD = lodArray[lodIdx];
                    for (int rendererIdx = 0; rendererIdx < currentLOD.renderers.Length; ++rendererIdx)
                    {
                        if (currentLOD.renderers[rendererIdx] == null)
                        {
                            Debug.LogWarning("The LOD Group " + lodGroup.gameObject.name + " has at least one missing renderer in its children.");
                            generalErrorFlag = true;
                        }
                    }
                }
            }

            if (!generalErrorFlag)
            {
                Debug.Log("No errors were detected in the process.");
            }
        }

        [MenuItem("Edit/Rendering/Fix Warning 'referenced script in (Game Object 'SceneIDMap') is missing' in loaded scenes", priority = CoreUtils.Sections.section2 + CoreUtils.Priorities.editMenuPriority + 4)]
        static public void FixWarningGameObjectSceneIDMapIsMissingInLoadedScenes()
        {
            var rootCache = new List<GameObject>();
            for (var i = 0; i < SceneManager.sceneCount; ++i)
                FixWarningGameObjectSceneIDMapIsMissingFor(SceneManager.GetSceneAt(i), rootCache);
        }

        static void FixWarningGameObjectSceneIDMapIsMissingFor(Scene scene, List<GameObject> rootCache)
        {
            Assert.IsTrue(scene.isLoaded);

            var roots = rootCache ?? new List<GameObject>();
            roots.Clear();
            scene.GetRootGameObjects(roots);
            bool markSceneAsDirty = false;
            for (var i = roots.Count - 1; i >= 0; --i)
            {
                if (roots[i].name == "SceneIDMap")
                {
                    if (roots[i].GetComponent<SceneObjectIDMapSceneAsset>() == null)
                    {
                        // This gameObject must have SceneObjectIDMapSceneAsset
                        // If not, then Unity can't find the component.
                        // We can remove it, it will be regenerated properly by rebaking
                        // the probes.
                        //
                        // This happens for scene with baked probes authored before renaming
                        // the HDRP's namespaces without the 'Experiemental' prefix.
                        // The serialization used this path explicitly, thus the Unity serialization
                        // system lost the reference to the MonoBehaviour
                        UnityEngine.Object.DestroyImmediate(roots[i]);

                        // If we do any any modification on the scene
                        // we need to dirty it, otherwise, the editor won't commit the change to the disk
                        // and the issue will still persist.
                        if (!markSceneAsDirty)
                            markSceneAsDirty = true;
                    }
                }
            }
            if (markSceneAsDirty)
                SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
