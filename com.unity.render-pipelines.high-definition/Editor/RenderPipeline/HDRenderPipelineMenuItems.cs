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
                    LitGUI.SetupMaterialKeywordsAndPass(mat);
                    EditorUtility.SetDirty(mat);
                }
                else if (mat.shader.name == "HDRP/LayeredLitTessellation")
                {
                    mat.shader = layeredLitShader;
                    // We remove all keyword already present
                    CoreEditorUtils.RemoveMaterialKeywords(mat);
                    LayeredLitGUI.SetupMaterialKeywordsAndPass(mat);
                    EditorUtility.SetDirty(mat);
                }
            }
        }

        [MenuItem("Edit/Render Pipeline/Export Sky to Image", priority = CoreUtils.editMenuPriority3)]
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

        [MenuItem("GameObject/Volume/Sky and Fog Volume", priority = CoreUtils.gameObjectMenuPriority)]
        static void CreateSceneSettingsGameObject(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var settings = CoreEditorUtils.CreateGameObject(parent, "Sky and Fog Volume");
            GameObjectUtility.SetParentAndAlign(settings, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(settings, "Create " + settings.name);
            Selection.activeObject = settings;

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

        [MenuItem("Edit/Render Pipeline/Upgrade Fog Volume Components", priority = CoreUtils.editMenuPriority2)]
        static void UpgradeFogVolumeComponents(MenuCommand menuCommand)
        {
            void OverrideCommonParameters(AtmosphericScattering input, Fog output)
            {
                if (input.colorMode.overrideState)
                    output.colorMode.Override(input.colorMode.value);
                if (input.color.overrideState)
                    output.color.Override(input.color.value);
                if (input.maxFogDistance.overrideState)
                    output.maxFogDistance.Override(input.maxFogDistance.value);
                if (input.mipFogMaxMip.overrideState)
                    output.mipFogMaxMip.Override(input.mipFogMaxMip.value);
                if (input.mipFogNear.overrideState)
                    output.mipFogNear.Override(input.mipFogNear.value);
                if (input.mipFogFar.overrideState)
                    output.mipFogFar.Override(input.mipFogFar.value);
                if (input.tint.overrideState)
                    output.tint.Override(input.tint.value);
            }

            Fog CreateFogComponentIfNeeded(VolumeProfile profile)
            {
                Fog fogComponent = null;
                if (!profile.TryGet(out fogComponent))
                {
                    fogComponent = VolumeProfileFactory.CreateVolumeComponent<Fog>(profile, false, false);
                }

                return fogComponent;
            }

            if (!EditorUtility.DisplayDialog(DialogText.title, "This will upgrade all Volume Profiles containing Exponential or Volumetric Fog components to the new Fog component. " + DialogText.projectBackMessage, DialogText.proceed, DialogText.cancel))
                return;

            var profilePathList = AssetDatabase.FindAssets("t:VolumeProfile", new string[]{ "Assets" });

            int profileCount = profilePathList.Length;
            int profileIndex = 0;
            foreach (string guid in profilePathList)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                profileIndex++;
                if (EditorUtility.DisplayCancelableProgressBar("Upgrade Fog Volume Components", string.Format("({0} of {1}) {2}", profileIndex, profileCount, assetPath), (float)profileIndex / (float)profileCount))
                    break;

                VolumeProfile profile = AssetDatabase.LoadAssetAtPath(assetPath, typeof(VolumeProfile)) as VolumeProfile;

                if (profile.TryGet<VisualEnvironment>(out var visualEnv))
                {
                    if (visualEnv.fogType.value == FogType.Exponential || visualEnv.fogType.value == FogType.Volumetric)
                    {
                        var fog = CreateFogComponentIfNeeded(profile);
                        fog.enabled.Override(true);
                    }
                }


                if (profile.TryGet<ExponentialFog>(out var expFog))
                {
                    var fog = CreateFogComponentIfNeeded(profile);

                    // We only migrate distance because the height parameters are not compatible.
                    if (expFog.fogDistance.overrideState)
                        fog.meanFreePath.Override(expFog.fogDistance.value);

                    OverrideCommonParameters(expFog, fog);
                    EditorUtility.SetDirty(profile);
                }

                if (profile.TryGet<VolumetricFog>(out var volFog))
                {
                    var fog = CreateFogComponentIfNeeded(profile);

                    fog.enableVolumetricFog.Override(true);
                    if (volFog.meanFreePath.overrideState)
                        fog.meanFreePath.Override(volFog.meanFreePath.value);
                    if (volFog.albedo.overrideState)
                        fog.albedo.Override(volFog.albedo.value);
                    if (volFog.baseHeight.overrideState)
                        fog.baseHeight.Override(volFog.baseHeight.value);
                    if (volFog.maximumHeight.overrideState)
                        fog.maximumHeight.Override(volFog.maximumHeight.value);
                    if (volFog.anisotropy.overrideState)
                        fog.anisotropy.Override(volFog.anisotropy.value);
                    if (volFog.globalLightProbeDimmer.overrideState)
                        fog.globalLightProbeDimmer.Override(volFog.globalLightProbeDimmer.value);

                    OverrideCommonParameters(volFog, fog);
                    EditorUtility.SetDirty(profile);
                }

                if (profile.TryGet<VolumetricLightingController>(out var volController))
                {
                    var fog = CreateFogComponentIfNeeded(profile);
                    if (volController.depthExtent.overrideState)
                        fog.depthExtent.Override(volController.depthExtent.value);
                    if (volController.sliceDistributionUniformity.overrideState)
                        fog.sliceDistributionUniformity.Override(volController.sliceDistributionUniformity.value);

                    EditorUtility.SetDirty(profile);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }

        [MenuItem("Edit/Render Pipeline/Upgrade Sky Intensity Mode", priority = CoreUtils.editMenuPriority2)]
        static void UpgradeSkyIntensityMode(MenuCommand menuCommand)
        {
            if (!EditorUtility.DisplayDialog(DialogText.title, "This will upgrade all Volume Profiles containing Sky components with the new intensity mode paradigm. " + DialogText.projectBackMessage, DialogText.proceed, DialogText.cancel))
                return;

            var profilePathList = AssetDatabase.FindAssets("t:VolumeProfile", new string[] { "Assets" });

            int profileCount = profilePathList.Length;
            int profileIndex = 0;
            foreach (string guid in profilePathList)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                profileIndex++;
                if (EditorUtility.DisplayCancelableProgressBar("Upgrade Sky Components", string.Format("({0} of {1}) {2}", profileIndex, profileCount, assetPath), (float)profileIndex / (float)profileCount))
                    break;

                VolumeProfile profile = AssetDatabase.LoadAssetAtPath(assetPath, typeof(VolumeProfile)) as VolumeProfile;

                List<SkySettings> m_VolumeSkyList = new List<SkySettings>();
                if (profile.TryGetAllSubclassOf<SkySettings>(typeof(SkySettings), m_VolumeSkyList))
                {
                    foreach (var sky in m_VolumeSkyList)
                    {
                        // Trivial case where multiplier is not used we ignore, otherwise we end up with a multiplier of 0.833 for a 0.0 EV100 exposure
                        if (sky.multiplier.value == 1.0f)
                            continue;
                        else if (sky.skyIntensityMode.value == SkyIntensityMode.Exposure) // Not Lux
                        {
                            // Any component using Exposure and Multiplier at the same time must switch to multiplier as we will convert exposure*multiplier into a multiplier.
                            sky.skyIntensityMode.Override(SkyIntensityMode.Multiplier);
                        }

                        // Convert exposure * multiplier to multiplier and reset exposure for all non trivial cases.
                        sky.multiplier.Override(sky.multiplier.value * ColorUtils.ConvertEV100ToExposure(-sky.exposure.value));
                        sky.exposure.Override(0.0f);

                        EditorUtility.SetDirty(profile);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
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

            protected virtual void PostCreateAssetWork(TAssetType asset) {}
        }

        class DoCreateNewAssetDiffusionProfileSettings : DoCreateNewAsset<DiffusionProfileSettings>
        {
            protected override void PostCreateAssetWork(DiffusionProfileSettings asset)
            {
                // Update the hash after that the asset was saved on the disk (hash requires the GUID of the asset)
                DiffusionProfileHashTable.UpdateDiffusionProfileHashNow(asset);
            }
        }

        [MenuItem("Assets/Create/Rendering/Diffusion Profile", priority = CoreUtils.assetCreateMenuPriority2)]
        static void MenuCreateDiffusionProfile()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateNewAssetDiffusionProfileSettings>(), "New Diffusion Profile.asset", icon, null);
        }

        [MenuItem("Assets/Create/Shader/HDRP/Custom FullScreen Pass")]
        static void MenuCreateCustomFullScreenPassShader()
        {
            string templatePath = $"{HDUtils.GetHDRenderPipelinePath()}/Editor/RenderPipeline/CustomPass/CustomPassFullScreenShader.template";
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "New FullScreen CustomPass.shader");
        }

        [MenuItem("Assets/Create/Shader/HDRP/Custom Renderers Pass")]
        static void MenuCreateCustomRenderersPassShader()
        {
            string templatePath = $"{HDUtils.GetHDRenderPipelinePath()}/Editor/RenderPipeline/CustomPass/CustomPassRenderersShader.template";
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "New Renderers CustomPass.shader");
        }

        [MenuItem("Assets/Create/Rendering/C# Custom Pass")]
        static void MenuCreateCustomPassCSharpScript()
        {
            string templatePath = $"{HDUtils.GetHDRenderPipelinePath()}/Editor/RenderPipeline/CustomPass/CustomPassCSharpScript.template";
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "New Custom Pass.cs");
        }

        [MenuItem("Assets/Create/Rendering/C# Post Process Volume", priority = CoreUtils.assetCreateMenuPriority3)]
        static void MenuCreateCSharpPostProcessVolume()
        {
            string templatePath = $"{HDUtils.GetHDRenderPipelinePath()}/Editor/PostProcessing/Templates/CustomPostProcessingVolume.template";
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "New Post Process Volume.cs");
        }

        [MenuItem("Assets/Create/Shader/HDRP/Post Process", priority = CoreUtils.assetCreateMenuPriority3)]
        static void MenuCreatePostProcessShader()
        {
            string templatePath = $"{HDUtils.GetHDRenderPipelinePath()}/Editor/PostProcessing/Templates/CustomPostProcessingShader.template";
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "New Post Process Shader.shader");
        }

        //[MenuItem("Internal/HDRP/Add \"Additional Light-shadow Data\" (if not present)")]
        static void AddAdditionalLightData()
        {
            var lights = UnityObject.FindObjectsOfType(typeof(Light)) as Light[];

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
            var cameras = UnityObject.FindObjectsOfType(typeof(Camera)) as Camera[];

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

        // The goal of this script is to help maintenance of data that have already been produced but need to update to the latest shader code change.
        // In case the shader code have change and the inspector have been update with new kind of keywords we need to regenerate the set of keywords use by the material.
        // This script will remove all keyword of a material and trigger the inspector that will re-setup all the used keywords.
        // It require that the inspector of the material have a static function call that update all keyword based on material properties.
        [MenuItem("Edit/Render Pipeline/Reset All Loaded High Definition Materials Keywords", priority = CoreUtils.editMenuPriority3)]
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
        //[MenuItem("Edit/Render Pipeline/Reset All Material Asset's Keywords (Materials in Project)", priority = CoreUtils.editMenuPriority3)]
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

        [MenuItem("Edit/Render Pipeline/Reset All Project and Scene High Definition Materials Keywords", priority = CoreUtils.editMenuPriority3)]
        static void ResetAllMaterialKeywordsInProjectAndScenes()
        {
            var openedScenes = new string[EditorSceneManager.loadedSceneCount];
            for (var i = 0; i < openedScenes.Length; ++i)
                openedScenes[i] = SceneManager.GetSceneAt(i).path;

            bool VCSEnabled = (UnityEditor.VersionControl.Provider.enabled && UnityEditor.VersionControl.Provider.isActive);

            try
            {
                var scenes = AssetDatabase.FindAssets("t:Scene", new string[]{ "Assets" });
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
            var matIds = AssetDatabase.FindAssets("t:Material", new string[]{ "Assets" }); // do not include packages

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

        [MenuItem("GameObject/Volume/Custom Pass", priority = CoreUtils.gameObjectMenuPriority)]
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

        [MenuItem("Edit/Render Pipeline/Check Scene Content for Ray Tracing", priority = CoreUtils.editMenuPriority4)]
        static void CheckSceneContentForRayTracing(MenuCommand menuCommand)
        {
            // Flag that holds
            bool generalErrorFlag = false;
            var rendererArray = UnityEngine.GameObject.FindObjectsOfType<Renderer>();
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
                    Debug.LogWarning("The object "+ currentRenderer.name + " has a null material array.");
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

                bool materialIsOnlyTransparent = true;
                bool hasTransparentSubMaterial = false;

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
                                && HDRenderQueue.k_RenderQueue_Transparent.upperBound >= currentMaterial.renderQueue)
                                || (HDRenderQueue.k_RenderQueue_AllTransparentRaytracing.lowerBound <= currentMaterial.renderQueue
                                && HDRenderQueue.k_RenderQueue_AllTransparentRaytracing.upperBound >= currentMaterial.renderQueue);

                            // aggregate the transparency info
                            materialIsOnlyTransparent &= materialIsTransparent;
                            hasTransparentSubMaterial |= materialIsTransparent;
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
            }

            if (!generalErrorFlag)
            {
                Debug.Log("No errors were detected in the process.");
            }
        }
    }
}
