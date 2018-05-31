using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class UpgradeMenuItems
    {
        //[MenuItem("Internal/HDRenderPipeline/Upgrade Scene Light Intensity to physical light unit", priority = CoreUtils.editMenuPriority2)]
        static void UpgradeLightsPLU()
        {
            Light[] lights = Resources.FindObjectsOfTypeAll<Light>();

            foreach (var l in lights)
            {
                var add = l.GetComponent<HDAdditionalLightData>();

                if (add == null)
                {
                    continue;
                }

                // We only need to update the new intensity parameters on additional data, no need to change intensity
                if (add.lightTypeExtent == LightTypeExtent.Punctual)
                {
                    switch (l.type)
                    {
                        case LightType.Point:
                            add.punctualIntensity = l.intensity / LightUtils.ConvertPointLightIntensity(1.0f);
                            break;

                        case LightType.Spot:
                            add.punctualIntensity = l.intensity / LightUtils.ConvertPointLightIntensity(1.0f);
                            break;

                        case LightType.Directional:
                            add.directionalIntensity = l.intensity;
                            break;
                    }
                }
                else if (add.lightTypeExtent == LightTypeExtent.Rectangle)
                {
                    add.areaIntensity = l.intensity / LightUtils.ConvertRectLightIntensity(1.0f, add.shapeWidth, add.shapeHeight);
                }
                else if (add.lightTypeExtent == LightTypeExtent.Line)
                {
                    add.areaIntensity = l.intensity / LightUtils.CalculateLineLightIntensity(1.0f, add.shapeWidth);
                }
            }

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
        }

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

                    bool VSCEnabled = (UnityEditor.VersionControl.Provider.enabled && UnityEditor.VersionControl.Provider.isActive);

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
                            CoreUtils.CheckOutFile(VSCEnabled, mat);

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

                    bool VSCEnabled = (UnityEditor.VersionControl.Provider.enabled && UnityEditor.VersionControl.Provider.isActive);

                    if (mat.shader.name == "HDRenderPipeline/LitTessellation" ||
                        mat.shader.name == "HDRenderPipeline/Lit")
                    {
                        // Need only test one of the new properties
                        if (mat.HasProperty("_HeightPoMAmplitude"))
                        {
                            CoreUtils.CheckOutFile(VSCEnabled, mat);

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
                            CoreUtils.CheckOutFile(VSCEnabled, mat);

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
    }
}
