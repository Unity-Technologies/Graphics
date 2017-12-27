using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using UnityObject = UnityEngine.Object;

    public class HDRenderPipelineMenuItems
    {
        [MenuItem("Internal/HDRenderPipeline/Add \"Additional Light-shadow Data\" (if not present)")]
        static void AddAdditionalLightData()
        {
            var lights = UnityObject.FindObjectsOfType(typeof(Light)) as Light[];

            foreach (var light in lights)
            {
                // Do not add a component if there already is one.
                if (light.GetComponent<HDAdditionalLightData>() == null)
                    light.gameObject.AddComponent<HDAdditionalLightData>();

                if (light.GetComponent<AdditionalShadowData>() == null)
                    light.gameObject.AddComponent<AdditionalShadowData>();
            }
        }

        [MenuItem("Internal/HDRenderPipeline/Add \"Additional Camera Data\" (if not present)")]
        static void AddAdditionalCameraData()
        {
            var cameras = UnityObject.FindObjectsOfType(typeof(Camera)) as Camera[];

            foreach (var camera in cameras)
            {
                // Do not add a component if there already is one.
                if (camera.GetComponent<HDAdditionalCameraData>() == null)
                    camera.gameObject.AddComponent<HDAdditionalCameraData>();
            }
        }
        static void CheckOutFile(bool VSCEnabled, Material mat)
        {
            if (VSCEnabled)
            {
                UnityEditor.VersionControl.Task task = UnityEditor.VersionControl.Provider.Checkout(mat, UnityEditor.VersionControl.CheckoutMode.Both);

                if (!task.success)
                {
                    Debug.Log(task.text + " " + task.resultCode);
                }
            }
        }

        // This script is a helper for the artists to re-synchronize all layered materials
        [MenuItem("Internal/HDRenderPipeline/Synchronize all Layered materials")]
        static void SynchronizeAllLayeredMaterial()
        {
            var materials = Resources.FindObjectsOfTypeAll<Material>();

            bool VSCEnabled = (UnityEditor.VersionControl.Provider.enabled && UnityEditor.VersionControl.Provider.isActive);

            foreach (var mat in materials)
            {
                if (mat.shader.name == "HDRenderPipeline/LayeredLit" || mat.shader.name == "HDRenderPipeline/LayeredLitTessellation")
                {
                    CheckOutFile(VSCEnabled, mat);
                    LayeredLitGUI.SynchronizeAllLayers(mat);
                    EditorUtility.SetDirty(mat);
                }
            }
        }

        // The goal of this script is to help maintenance of data that have already been produced but need to update to the latest shader code change.
        // In case the shader code have change and the inspector have been update with new kind of keywords we need to regenerate the set of keywords use by the material.
        // This script will remove all keyword of a material and trigger the inspector that will re-setup all the used keywords.
        // It require that the inspector of the material have a static function call that update all keyword based on material properties.
        [MenuItem("Edit/Render Pipeline/Upgrade/High Definition/Reset All Materials Keywords (Loaded Materials)", priority = CoreUtils.editMenuPriority2)]
        static void ResetAllMaterialKeywords()
        {
            try
            {
                var materials = Resources.FindObjectsOfTypeAll<Material>();

                bool VSCEnabled = (UnityEditor.VersionControl.Provider.enabled && UnityEditor.VersionControl.Provider.isActive);

                for (int i = 0, length = materials.Length; i < length; i++)
                {
                    EditorUtility.DisplayProgressBar(
                        "Setup materials Keywords...",
                        string.Format("{0} / {1} materials cleaned.", i, length),
                        i / (float)(length - 1));

                    CheckOutFile(VSCEnabled, materials[i]);
                    HDEditorUtils.ResetMaterialKeywords(materials[i]);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Edit/Render Pipeline/Upgrade/High Definition/Reset All Materials Keywords (Materials in Project)", priority = CoreUtils.editMenuPriority2)]
        static void ResetAllMaterialKeywordsInProject()
        {
            try
            {
                var matIds = AssetDatabase.FindAssets("t:Material");

                bool VSCEnabled = (UnityEditor.VersionControl.Provider.enabled && UnityEditor.VersionControl.Provider.isActive);

                for (int i = 0, length = matIds.Length; i < length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(matIds[i]);
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                    EditorUtility.DisplayProgressBar(
                        "Setup materials Keywords...",
                        string.Format("{0} / {1} materials cleaned.", i, length),
                        i / (float)(length - 1));

                    CheckOutFile(VSCEnabled, mat);
                    HDEditorUtils.ResetMaterialKeywords(mat);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Internal/HDRenderPipeline/Update/Update SSS profile indices")]
        static void UpdateSSSProfileIndices()
        {
            try
            {
                var matIds = AssetDatabase.FindAssets("t:Material");

                for (int i = 0, length = matIds.Length; i < length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(matIds[i]);
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                    EditorUtility.DisplayProgressBar(
                        "Setup materials Keywords...",
                        string.Format("{0} / {1} materials SSS updated.", i, length),
                        i / (float)(length - 1));

                    bool VSCEnabled = (UnityEditor.VersionControl.Provider.enabled && UnityEditor.VersionControl.Provider.isActive);

                    if (mat.shader.name == "HDRenderPipeline/LitTessellation" ||
                        mat.shader.name == "HDRenderPipeline/Lit")
                    {
                        float fvalue = mat.GetFloat("_MaterialID");
                        if (fvalue == 0.0) // SSS
                        {
                            CheckOutFile(VSCEnabled, mat);
                            int ivalue = mat.GetInt("_SubsurfaceProfile");
                            if (ivalue == 15)
                            {
                                mat.SetInt("_SubsurfaceProfile", 0);
                            }
                            else
                            {
                                mat.SetInt("_SubsurfaceProfile", ivalue + 1);
                            }

                            EditorUtility.SetDirty(mat);
                        }
                    }
                    else if (mat.shader.name == "HDRenderPipeline/LayeredLit" ||
                                mat.shader.name == "HDRenderPipeline/LayeredLitTessellation")
                    {
                        float fvalue = mat.GetFloat("_MaterialID");
                        if (fvalue == 0.0) // SSS
                        {
                            CheckOutFile(VSCEnabled, mat);
                            int numLayer = (int)mat.GetFloat("_LayerCount");

                            for (int x = 0; x < numLayer; ++x)
                            {
                                int ivalue = mat.GetInt("_SubsurfaceProfile" + x);
                                if (ivalue == 15)
                                {
                                    mat.SetInt("_SubsurfaceProfile" + x, 0);
                                }
                                else
                                {
                                    mat.SetInt("_SubsurfaceProfile" + x, ivalue + 1);
                                }
                            }
                            EditorUtility.SetDirty(mat);
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // Function used only to check performance of data with and without tessellation
        [MenuItem("Internal/HDRenderPipeline/Test/Remove tessellation materials (not reversible)")]
        static void RemoveTessellationMaterials()
        {
            var materials = Resources.FindObjectsOfTypeAll<Material>();

            var litShader = Shader.Find("HDRenderPipeline/Lit");
            var layeredLitShader = Shader.Find("HDRenderPipeline/LayeredLit");

            foreach (var mat in materials)
            {
                if (mat.shader.name == "HDRenderPipeline/LitTessellation")
                {
                    mat.shader = litShader;
                    // We remove all keyword already present
                    CoreEditorUtils.RemoveMaterialKeywords(mat);
                    LitGUI.SetupMaterialKeywordsAndPass(mat);
                    EditorUtility.SetDirty(mat);
                }
                else if (mat.shader.name == "HDRenderPipeline/LayeredLitTessellation")
                {
                    mat.shader = layeredLitShader;
                    // We remove all keyword already present
                    CoreEditorUtils.RemoveMaterialKeywords(mat);
                    LayeredLitGUI.SetupMaterialKeywordsAndPass(mat);
                    EditorUtility.SetDirty(mat);
                }
            }
        }

        [MenuItem("Edit/Render Pipeline/Tools/High Definition/Export Sky to Image", priority = CoreUtils.editMenuPriority2)]
        static void ExportSkyToImage()
        {
            var renderpipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (renderpipeline == null)
            {
                Debug.LogError("HDRenderPipeline is not instantiated.");
                return;
            }

            var result = renderpipeline.ExportSkyToTexture();
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

        [MenuItem("GameObject/Render Pipeline/High Definition/Scene Settings", priority = 10)]
        static void CreateCustomGameObject(MenuCommand menuCommand)
        {
            var sceneSettings = new GameObject("Scene Settings");
            GameObjectUtility.SetParentAndAlign(sceneSettings, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(sceneSettings, "Create " + sceneSettings.name);
            Selection.activeObject = sceneSettings;
            var volume = sceneSettings.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.Add<HDShadowSettings>(true);
            var visualEnv = volume.Add<VisualEnvironment>(true);
            visualEnv.skyType.value = SkySettings.GetUniqueID<ProceduralSky>();
            visualEnv.fogType.value = FogType.Exponential;
            volume.Add<ProceduralSky>(true);
            volume.Add<ExponentialFog>(true);
        }

        class DoCreateNewAsset<TAssetType> : ProjectWindowCallback.EndNameEditAction where TAssetType : ScriptableObject
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var newAsset  = CreateInstance<TAssetType>();
                newAsset.name = Path.GetFileName(pathName);
                AssetDatabase.CreateAsset(newAsset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(newAsset);
            }
        }

        class DoCreateNewAssetSubsurfaceScatteringSettings : DoCreateNewAsset<SubsurfaceScatteringSettings> {}

        [MenuItem("Assets/Create/Render Pipeline/High Definition/Subsurface Scattering Settings", priority = CoreUtils.assetCreateMenuPriority2)]
        static void MenuCreateSubsurfaceScatteringProfile()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateNewAssetSubsurfaceScatteringSettings>(), "New SSS Settings.asset", icon, null);
        }
    }
}
