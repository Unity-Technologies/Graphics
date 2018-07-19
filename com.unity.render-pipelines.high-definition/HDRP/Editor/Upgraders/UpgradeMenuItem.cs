using System;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class UpgradeMenuItems
    {

        //[MenuItem("Internal/HDRenderPipeline/Update/Update material for subsurface")]
        static void UpdateMaterialForSubsurface()
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
                        string.Format("{0} / {1} materials subsurface updated.", i, length),
                        i / (float)(length - 1));

                    bool VCSEnabled = (UnityEditor.VersionControl.Provider.enabled && UnityEditor.VersionControl.Provider.isActive);

                    if (mat.shader.name == "HDRenderPipeline/LitTessellation" ||
                        mat.shader.name == "HDRenderPipeline/Lit" ||
                        mat.shader.name == "HDRenderPipeline/LayeredLit" ||
                        mat.shader.name == "HDRenderPipeline/LayeredLitTessellation")
                    {
                        float materialID = mat.GetInt("_MaterialID");
                        if (materialID != 0.0)
                            continue;

                        if (mat.HasProperty("_SSSAndTransmissionType"))
                        {
                            CoreEditorUtils.CheckOutFile(VCSEnabled, mat);

                            int materialSSSAndTransmissionID = mat.GetInt("_SSSAndTransmissionType");

                            // Both;, SSS only, Transmission only
                            if (materialSSSAndTransmissionID == 2.0)
                            {
                                mat.SetInt("_MaterialID", 5);
                            }
                            else
                            {
                                if (materialSSSAndTransmissionID == 0.0)
                                    mat.SetFloat("_TransmissionEnable", 1.0f);
                                else
                                    mat.SetFloat("_TransmissionEnable", 0.0f);
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

        //[MenuItem("Internal/HDRenderPipeline/Update/Update Height Maps parametrization")]
        static void UpdateHeightMapParametrization()
        {
            try
            {
                var matIds = AssetDatabase.FindAssets("t:Material");

                for (int i = 0, length = matIds.Length; i < length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(matIds[i]);
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                    EditorUtility.DisplayProgressBar(
                        "Updating Materials...",
                        string.Format("{0} / {1} materials updated.", i, length),
                        i / (float)(length - 1));

                    bool VCSEnabled = (UnityEditor.VersionControl.Provider.enabled && UnityEditor.VersionControl.Provider.isActive);

                    if (mat.shader.name == "HDRenderPipeline/LitTessellation" ||
                        mat.shader.name == "HDRenderPipeline/Lit")
                    {
                        // Need only test one of the new properties
                        if (mat.HasProperty("_HeightPoMAmplitude"))
                        {
                            CoreEditorUtils.CheckOutFile(VCSEnabled, mat);

                            float valueMax = mat.GetFloat("_HeightMax");
                            float valueMin = mat.GetFloat("_HeightMin");
                            float center = mat.GetFloat("_HeightCenter");
                            float amplitude = valueMax - valueMin;
                            mat.SetInt("_HeightMapParametrization", 1);
                            mat.SetFloat("_HeightPoMAmplitude", amplitude);
                            mat.SetFloat("_HeightTessAmplitude", amplitude);
                            mat.SetFloat("_HeightOffset", 0.0f);
                            mat.SetFloat("_HeightTessCenter", center);

                            BaseLitGUI.DisplacementMode displaceMode = (BaseLitGUI.DisplacementMode)mat.GetInt("_DisplacementMode");
                            if (displaceMode == BaseLitGUI.DisplacementMode.Pixel)
                            {
                                mat.SetFloat("_HeightCenter", 1.0f); // With PoM this is always 1.0f. We set it here to avoid having to open the UI to update it.
                            }

                            EditorUtility.SetDirty(mat);
                        }
                    }
                    else if (mat.shader.name == "HDRenderPipeline/LayeredLit" ||
                             mat.shader.name == "HDRenderPipeline/LayeredLitTessellation")
                    {
                        int numLayer = (int)mat.GetFloat("_LayerCount");

                        if (mat.HasProperty("_HeightPoMAmplitude0"))
                        {
                            CoreEditorUtils.CheckOutFile(VCSEnabled, mat);

                            for (int x = 0; x < numLayer; ++x)
                            {
                                float valueMax = mat.GetFloat("_HeightMax" + x);
                                float valueMin = mat.GetFloat("_HeightMin" + x);
                                float center = mat.GetFloat("_HeightCenter" + x);
                                float amplitude = valueMax - valueMin;
                                mat.SetInt("_HeightMapParametrization" + x, 1);
                                mat.SetFloat("_HeightPoMAmplitude" + x, amplitude);
                                mat.SetFloat("_HeightTessAmplitude" + x, amplitude);
                                mat.SetFloat("_HeightOffset" + x, 0.0f);
                                mat.SetFloat("_HeightTessCenter" + x, center);

                                BaseLitGUI.DisplacementMode displaceMode = (BaseLitGUI.DisplacementMode)mat.GetInt("_DisplacementMode");
                                if (displaceMode == BaseLitGUI.DisplacementMode.Pixel)
                                {
                                    mat.SetFloat("_HeightCenter" + x, 1.0f); // With PoM this is always 1.0f. We set it here to avoid having to open the UI to update it.
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

        // Update EmissiveColor after we remove EmissiveIntensity from all shaders in 2018.2
        // Now EmissiveColor is HDR and it must be update to the value new EmissiveColor = old EmissiveColor * EmissiveIntensity
        static bool UpdateMaterial_EmissiveColor(string path, Material mat)
        {
            // Find the missing property in the file and update EmissiveColor
            string[] readText = File.ReadAllLines(path);

            foreach (string line in readText)
            {
                if (line.Contains("_EmissiveIntensity:"))
                {
                    int startPos = line.IndexOf(":") + 1;
                    string sub = line.Substring(startPos);
                    float emissiveIntensity = float.Parse(sub);

                    Color emissiveColor = Color.black;
                    if (mat.HasProperty("_EmissiveColor"))
                    {
                        emissiveColor = mat.GetColor("_EmissiveColor");
                    }

                    emissiveColor *= emissiveIntensity;
                    emissiveColor.a = 1.0f;
                    mat.SetColor("_EmissiveColor", emissiveColor);
                    // Also fix EmissionColor if needed (Allow to let HD handle GI, if black GI is disabled by legacy)
                    mat.SetColor("_EmissionColor", Color.white);

                    return true;
                }
            }

            return false;
        }

        static void UpdateMaterialFile_EmissiveColor(string path)
        {
            string[] readText = File.ReadAllLines(path);

            foreach (string line in readText)
            {
                if (line.Contains("_EmissiveIntensity:"))
                {
                    // Remove emissive intensity line
                    string[] writeText = readText.Where(l => l != line).ToArray();
                    File.WriteAllLines(path, writeText);
                    return;
                }
            }

            return;
        }

        delegate bool UpdateMaterial(string path, Material mat);
        delegate void UpdateMaterialFile(string path);

        static void UpdateMaterialToNewerVersion(string caption, UpdateMaterial updateMaterial, UpdateMaterialFile updateMaterialFile = null)
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
                        "Update material to new version " + caption + "...",
                        string.Format("{0} / {1} materials updated.", i, length),
                        i / (float)(length - 1));

                    if (mat.shader.name == "HDRenderPipeline/LitTessellation" ||
                        mat.shader.name == "HDRenderPipeline/Lit" ||
                        mat.shader.name == "HDRenderPipeline/LayeredLit" ||
                        mat.shader.name == "HDRenderPipeline/LayeredLitTessellation" ||
                        mat.shader.name == "HDRenderPipeline/StackLit" ||
                        mat.shader.name == "HDRenderPipeline/Unlit"
                         )
                    {
                        // Need to be processed in order - All function here should be re-entrant (i.e after upgrade it can be recall)
                        bool dirty = updateMaterial(path, mat);

                        if (dirty)
                        {
                            // Checkout the file and tag it as dirty
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

        [MenuItem("Edit/Render Pipeline/Single step upgrade script/Upgrade all Materials EmissionColor", priority = CoreUtils.editMenuPriority3)]
        static public void UpdateMaterialToNewerVersionEmissiveColor()
        {
            UpdateMaterialToNewerVersion("(EmissiveColor)", UpdateMaterial_EmissiveColor, UpdateMaterialFile_EmissiveColor);
        }

        [MenuItem("Edit/Render Pipeline/Upgrade all Materials to latest version", priority = CoreUtils.editMenuPriority3)]
        static public void UpdateMaterialToNewerVersion()
        {
            // Add here all the material upgrade function supported in this version
            // Caution: All the functions here MUST be re-entrant (call multiple time) without failing.

            float currentVersion = HDRPVersion.GetCurrentHDRPProjectVersion();
            if (currentVersion < 1.0)
            {
                // Appear in hdrp version 1.0
                UpdateMaterialToNewerVersion("(EmissiveColor)", UpdateMaterial_EmissiveColor, UpdateMaterialFile_EmissiveColor);
            }
        }
    }
}
