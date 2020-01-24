using System.IO;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;

namespace UnityEditor.Rendering.HighDefinition
{
    class UpgradeMenuItems
    {
        // Version 3

        // Added RenderStates for shader graphs, we need to ovewrite the material prperties used for the rendering
        // in these materials and synch them with the master node values (which are the shader default values, that's
        // why we create a new material in this script).

        // List of the render state properties to sync
        static readonly string[] floatPropertiesToReset = {
            kStencilRef, kStencilWriteMask,
            kStencilRefDepth, kStencilWriteMaskDepth,
            kStencilRefMV, kStencilWriteMaskMV,
            kStencilRefDistortionVec, kStencilWriteMaskDistortionVec,
            kStencilRefGBuffer, kStencilWriteMaskGBuffer, kZTestGBuffer,
            kSurfaceType, kBlendMode, "_SrcBlend", "_DstBlend", "_AlphaSrcBlend", "_AlphaDstBlend",
            kZWrite, "_CullMode", "_CullModeForward", kTransparentCullMode,
            kZTestDepthEqualForOpaque,
            kAlphaCutoffEnabled,
            kTransparentSortPriority, "_UseShadowThreshold",
            kDoubleSidedEnable, kDoubleSidedNormalMode,
            kTransparentBackfaceEnable, kReceivesSSR, kUseSplitLighting
        };

        static readonly string[] vectorPropertiesToReset = {
            "_DoubleSidedConstants",
        };

        // This upgrade functioncopy all the keywords needed for the BlendStates
        // to be synced with their master node values, then it calls the HDRP material keyword reset function and finally
        // it set the render queue of the material to match the one on the shader graph.
        // It's required to sync the shader default properties with the material because when you create a new material,
        // by default the Lit shader is assigned to it and so write all his properties into the material. It's a problem
        // because now that the shader graphs uses these properties, the material properties don't match the shader settings.
        // This function basically fix this.
        static bool UpdateMaterial_ShaderGraphRenderStates(string path, Material mat)
        {
            // We only need to upgrade shadergraphs materials
            if (GraphUtil.IsShaderGraph(mat.shader))
            {
                var defaultProperties = new Material(mat.shader);

                foreach (var floatToReset in floatPropertiesToReset)
                    if (mat.HasProperty(floatToReset))
                        mat.SetFloat(floatToReset, defaultProperties.GetFloat(floatToReset));
                foreach (var vectorToReset in vectorPropertiesToReset)
                    if (mat.HasProperty(vectorToReset))
                        mat.SetVector(vectorToReset, defaultProperties.GetVector(vectorToReset));

                HDShaderUtils.ResetMaterialKeywords(mat);

                mat.renderQueue = mat.shader.renderQueue;

                defaultProperties = null;

                return true;
            }

            return false;
        }

        delegate bool UpdateMaterial(string path, Material mat);
        delegate void UpdateMaterialFile(string path);

        static void ProcessUpdateMaterial(string caption, float scriptVersion, UpdateMaterial updateMaterial, UpdateMaterialFile updateMaterialFile = null)
        {
            bool VCSEnabled = (UnityEditor.VersionControl.Provider.enabled && UnityEditor.VersionControl.Provider.isActive);
            var matIds = AssetDatabase.FindAssets("t:Material");
            List<string> materialFiles = new List<string>(); // Contain the list dirty files

            try
            {
                for (int i = 0, length = matIds.Length; i < length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(matIds[i]);
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                    EditorUtility.DisplayProgressBar(
                        "Update material " + caption + "...",
                        string.Format("{0} / {1} materials updated.", i, length),
                        i / (float)(length - 1));

                    if (HDShaderUtils.IsHDRPShader(mat.shader))
                    {
                        // We don't handle embed material as we can't rewrite fbx files
                        if (Path.GetExtension(path).ToLower() == ".fbx")
                        {
                            continue;
                        }

                        bool dirty = updateMaterial(path, mat);

                        // Checkout the file and tag it as dirty
                        if (dirty)
                        {
                            CoreEditorUtils.CheckOutFile(VCSEnabled, mat);
                            EditorUtility.SetDirty(mat);
                            materialFiles.Add(path);
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                // Save all dirty assets
                AssetDatabase.SaveAssets();
            }

            if (updateMaterialFile == null)
                return;

            // Now that all the asset have been modified and save, we can safely update the .mat file and remove removed property
            try
            {
                for (int i = 0, length = materialFiles.Count; i < length; i++)
                {
                    string path = materialFiles[i];

                    EditorUtility.DisplayProgressBar(
                        "Update .mat files...",
                        string.Format("{0} / {1} materials .mat file updated.", i, length),
                        i / (float)(length - 1));

                    // Note: The file is supposed to be checkout by the previous loop
                    updateMaterialFile(path);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                // No need to save in this case
            }
        }

        // This function go through all loaded materials so it can be used to update any materials that is
        // not serialized as an asset (i.e materials saved in scenes)
        static void UpgradeSceneMaterials()
        {
#pragma warning disable 618
            var hdAsset = HDRenderPipeline.currentAsset;
            // For each loaded materials
            foreach (var mat in Resources.FindObjectsOfTypeAll<Material>())
            {
                DiffusionProfileSettings.UpgradeMaterial(mat, hdAsset.diffusionProfileSettings);
            }
#pragma warning restore 618
        }

        [MenuItem("Edit/Render Pipeline/Upgrade all Materials to newer version", priority = CoreUtils.editMenuPriority3)]
        static public void UpdateMaterialToNewerVersion()
        {
            // TODO: We need to handle material that are embed inside scene! + How to handle embed material in fbx?

            // Add here all the material upgrade functions
            // Note: This is a slow path as we go through all files for each script + update the version number after each script execution,
            // but it is the safest way to do it currently for incremental upgrade
            // Caution: When calling SaveAsset, Unity will update the material with latest addition at the same time, so for example
            // unity can add a supportDecal when executing script for version 1 whereas they only appear in version 2 because it is now part
            // of the shader. Most of the time this have no consequence, but we never know.

            // Caution: Version of latest script and default version in all HDRP shader must match

            UpgradeSceneMaterials();
        }

        [MenuItem("Edit/Render Pipeline/Reset All ShaderGraph Materials BlendStates (Project)")]
        static public void UpgradeAllShaderGraphMaterialBlendStatesProject()
        {
            ProcessUpdateMaterial("(ShaderGraphRenderStates_3)", 3.0f, UpdateMaterial_ShaderGraphRenderStates);
        }

        [MenuItem("Edit/Render Pipeline/Reset All ShaderGraph Materials BlendStates (Scene)")]
        static public void UpgradeAllShaderGraphMaterialBlendStatesScene()
        {
            var materials = Resources.FindObjectsOfTypeAll<Material>();

            foreach (var mat in materials)
            {
                string path = AssetDatabase.GetAssetPath(mat);

                if (!string.IsNullOrEmpty(path))
                    UpdateMaterial_ShaderGraphRenderStates(path, mat);
            }
        }

        [MenuItem("Edit/Render Pipeline/Fix Warning 'referenced script in (Game Object 'SceneIDMap') is missing' in loaded scenes")]
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
                        Object.DestroyImmediate(roots[i]);

                        // If we do any any modification on the scene
                        // we need to dirty it, otherwise, the editor won't commit the change to the disk
                        // and the issue will still persist.
                        if (!markSceneAsDirty)
                            markSceneAsDirty = true;
                    }
                }
            }
            if(markSceneAsDirty)
                SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
