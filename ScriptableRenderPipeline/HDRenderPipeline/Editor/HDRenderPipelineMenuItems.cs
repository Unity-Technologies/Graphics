using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using UnityObject = UnityEngine.Object;

    public class HDRenderPipelineMenuItems
    {
        [MenuItem("HDRenderPipeline/Add \"Additional Light-shadow Data\" (if not present)")]
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

        [MenuItem("HDRenderPipeline/Add \"Additional Camera Data\" (if not present)")]
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

        // This script is a helper for the artists to re-synchronize all layered materials
        [MenuItem("HDRenderPipeline/Synchronize all Layered materials")]
        static void SynchronizeAllLayeredMaterial()
        {
            var materials = Resources.FindObjectsOfTypeAll<Material>();

            foreach (var mat in materials)
            {
                if (mat.shader.name == "HDRenderPipeline/LayeredLit" || mat.shader.name == "HDRenderPipeline/LayeredLitTessellation")
                {
                    LayeredLitGUI.SynchronizeAllLayers(mat);
                    EditorUtility.SetDirty(mat);
                }
            }
        }

        // The goal of this script is to help maintenance of data that have already been produced but need to update to the latest shader code change.
        // In case the shader code have change and the inspector have been update with new kind of keywords we need to regenerate the set of keywords use by the material.
        // This script will remove all keyword of a material and trigger the inspector that will re-setup all the used keywords.
        // It require that the inspector of the material have a static function call that update all keyword based on material properties.
        [MenuItem("HDRenderPipeline/Test/Reset all materials keywords")]
        static void ResetAllMaterialKeywords()
        {
            try
            {
                var materials = Resources.FindObjectsOfTypeAll<Material>();

                for (int i = 0, length = materials.Length; i < length; i++)
                {
                    EditorUtility.DisplayProgressBar(
                        "Setup materials Keywords...",
                        string.Format("{0} / {1} materials cleaned.", i, length),
                        i / (float)(length - 1));

                    HDEditorUtils.ResetMaterialKeywords(materials[i]);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("HDRenderPipeline/Test/Reset all materials keywords in project")]
        static void ResetAllMaterialKeywordsInProject()
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
                        string.Format("{0} / {1} materials cleaned.", i, length),
                        i / (float)(length - 1));

                    HDEditorUtils.ResetMaterialKeywords(mat);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // Function used only to check performance of data with and without tessellation
        [MenuItem("HDRenderPipeline/Test/Remove tessellation materials (not reversible)")]
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
                    HDEditorUtils.RemoveMaterialKeywords(mat);
                    LitGUI.SetupMaterialKeywordsAndPass(mat);
                    EditorUtility.SetDirty(mat);
                }
                else if (mat.shader.name == "HDRenderPipeline/LayeredLitTessellation")
                {
                    mat.shader = layeredLitShader;
                    // We remove all keyword already present
                    HDEditorUtils.RemoveMaterialKeywords(mat);
                    LayeredLitGUI.SetupMaterialKeywordsAndPass(mat);
                    EditorUtility.SetDirty(mat);
                }
            }
        }

        [MenuItem("HDRenderPipeline/Export Sky to Image")]
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

        [MenuItem("GameObject/HD Render Pipeline/Scene Settings", false, 10)]
        static void CreateCustomGameObject(MenuCommand menuCommand)
        {
            var sceneSettings = new GameObject("Scene Settings");
            GameObjectUtility.SetParentAndAlign(sceneSettings, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(sceneSettings, "Create " + sceneSettings.name);
            Selection.activeObject = sceneSettings;
            sceneSettings.AddComponent<SceneSettings>();
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

        class DoCreateNewAssetCommonSettings : DoCreateNewAsset<CommonSettings> {}
        class DoCreateNewAssetHDRISkySettings : DoCreateNewAsset<HDRISkySettings> {}
        class DoCreateNewAssetBlacksmithSkySettings : DoCreateNewAsset<BlacksmithSkySettings> {}
        class DoCreateNewAssetProceduralSkySettings : DoCreateNewAsset<ProceduralSkySettings> {}
        class DoCreateNewAssetSubsurfaceScatteringSettings : DoCreateNewAsset<SubsurfaceScatteringSettings> {}

        [MenuItem("Assets/Create/HDRenderPipeline/Common Settings", priority = 700)]
        static void MenuCreateCommonSettings()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateNewAssetCommonSettings>(), "New CommonSettings.asset", icon, null);
        }

        [MenuItem("Assets/Create/HDRenderPipeline/Subsurface Scattering Settings", priority = 702)]
        static void MenuCreateSubsurfaceScatteringProfile()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateNewAssetSubsurfaceScatteringSettings>(), "New SSS Profile.asset", icon, null);
        }

        [MenuItem("Assets/Create/HDRenderPipeline/HDRISky Settings", priority = 750)]
        static void MenuCreateHDRISkySettings()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateNewAssetHDRISkySettings>(), "New HDRISkySettings.asset", icon, null);
        }

        [MenuItem("Assets/Create/HDRenderPipeline/BlacksmithSky Settings", priority = 751)]
        static void MenuCreateBlacksmithSkySettings()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateNewAssetBlacksmithSkySettings>(), "New BlacksmithSkySettings.asset", icon, null);
        }

        [MenuItem("Assets/Create/HDRenderPipeline/ProceduralSky Settings", priority = 752)]
        static void MenuCreateProceduralSkySettings()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateNewAssetProceduralSkySettings>(), "New ProceduralSkySettings.asset", icon, null);
        }

    }
}
