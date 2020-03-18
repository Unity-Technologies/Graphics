using UnityEngine;
using UnityEditor.AssetImporters;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal
{
    public class AutodeskInteractiveMaterialImport : AssetPostprocessor
    {
        static readonly uint k_Version = 1;
        static readonly int k_Order = 3;
        public override uint GetVersion()
        {
            return k_Version;
        }
        public override int GetPostprocessOrder()
        {
            return k_Order;
        }

        public void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            if (IsAutodeskInteractiveMaterial(description))
            {
                float floatProperty;
                Vector4 vectorProperty;
                TexturePropertyDescription textureProperty;

                bool isMasked = description.TryGetProperty("mask_threshold",out floatProperty);
                bool isTransparent = description.TryGetProperty("opacity",out floatProperty);

                Shader shader;
                if (isMasked)
                    shader = GraphicsSettings.currentRenderPipeline.autodeskInteractiveMaskedShader;
                else if (isTransparent)
                    shader = GraphicsSettings.currentRenderPipeline.autodeskInteractiveTransparentShader;
                else
                    shader = GraphicsSettings.currentRenderPipeline.autodeskInteractiveShader;

                if (shader == null)
                    return;

                material.shader = shader;
                foreach (var clip in clips)
                {
                    clip.ClearCurves();
                }

                if (description.TryGetProperty("base_color", out vectorProperty))
                    material.SetColor("_Color", vectorProperty);
                if (description.TryGetProperty("emissive", out vectorProperty))
                    material.SetColor("_EmissionColor", vectorProperty);

                if (description.TryGetProperty("roughness", out floatProperty))
                    material.SetFloat("_Roughness", floatProperty);

                if (description.TryGetProperty("metallic", out floatProperty))
                    material.SetFloat("_Metallic", floatProperty);

                if (description.TryGetProperty("uvTransform", out vectorProperty))
                {
                    material.SetVector("_UvOffset", new Vector4(vectorProperty.x, vectorProperty.y, .0f, .0f));
                    material.SetVector("_UvTiling", new Vector4(vectorProperty.w, vectorProperty.z, .0f, .0f));
                }

                if (description.TryGetProperty("TEX_color_map", out textureProperty))
                {
                    material.SetTexture("_MainTex", textureProperty.texture);
                    material.SetFloat("_UseColorMap", 1.0f);
                }
                else
                {
                    material.SetFloat("_UseColorMap", 0.0f);
                }

                if (description.TryGetProperty("TEX_normal_map", out textureProperty))
                {
                    material.SetTexture("_BumpMap", textureProperty.texture);
                    material.SetFloat("_UseNormalMap", 1.0f);
                }
                else
                {
                    material.SetFloat("_UseNormalMap", 0.0f);
                }

                if (description.TryGetProperty("TEX_roughness_map", out textureProperty))
                {
                    material.SetTexture("RoughnessMap", textureProperty.texture);
                    material.SetFloat("_UseRoughnessMap", 1.0f);
                }
                else
                {
                    material.SetFloat("_UseRoughnessMap", 0.0f);
                }

                if (description.TryGetProperty("TEX_metallic_map", out textureProperty))
                {
                    material.SetTexture("_MetallicMap", textureProperty.texture);
                    material.SetFloat("_UseMetallicMap", 1.0f);
                }
                else
                {
                    material.SetFloat("_UseMetallicMap", 0.0f);
                }

                if (description.TryGetProperty("TEX_emissive_map", out textureProperty))
                {
                    material.SetTexture("_EmissionMap", textureProperty.texture);
                    material.SetFloat("_UseEmissiveMap", 1.0f);
                }
                else
                {
                    material.SetFloat("_UseEmissiveMap", 0.0f);
                }

                if (description.TryGetProperty("hasTransparencyTexture", out floatProperty))
                    material.SetFloat("_UseOpacityMap", floatProperty);

                if (description.TryGetProperty("transparencyMaskThreshold", out floatProperty))
                    material.SetFloat("_OpacityThreshold", floatProperty);

                if (description.TryGetProperty("TEX_ao_map", out textureProperty))
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture>(textureProperty.relativePath);
                    material.SetTexture("AoMap", tex);
                    material.SetFloat("UseAoMap", 1.0f);
                }
                else
                {
                    material.SetFloat("UseAoMap", 0.0f);
                }

                RemapColorCurves(description, clips, "base_color", "_Color");
                RemapCurve(description, clips, "mask_threshold", "_Cutoff");
                RemapCurve(description, clips, "metallic", "_Metallic");
                RemapCurve(description, clips, "roughness", "_Glossiness");

                for (int i = 0; i < clips.Length; i++)
                {
                    if (description.HasAnimationCurveInClip(clips[i].name, "uv_scale.x") || description.HasAnimationCurveInClip(clips[i].name, "uv_scale.y"))
                    {
                        AnimationCurve curve;
                        if (description.TryGetAnimationCurve(clips[i].name, "uv_scale.x", out curve))
                            clips[i].SetCurve("", typeof(Material), "_UvTiling.x", curve);
                        else
                            clips[i].SetCurve("", typeof(Material), "_UvTiling.x", AnimationCurve.Constant(0.0f, 1.0f, 1.0f));

                        if (description.TryGetAnimationCurve(clips[i].name, "uv_scale.y", out curve))
                            clips[i].SetCurve("", typeof(Material), "_UvTiling.y", curve);
                        else
                            clips[i].SetCurve("", typeof(Material), "_UvTiling.y", AnimationCurve.Constant(0.0f, 1.0f, 1.0f));
                    }

                    if (description.HasAnimationCurveInClip(clips[i].name, "uv_offset.x") || description.HasAnimationCurveInClip(clips[i].name, "uv_offset.y"))
                    {
                        AnimationCurve curve;
                        if (description.TryGetAnimationCurve(clips[i].name, "uv_offset.x", out curve))
                            clips[i].SetCurve("", typeof(Material), "_UvOffset.x", curve);
                        else
                            clips[i].SetCurve("", typeof(Material), "_UvOffset.x", AnimationCurve.Constant(0.0f, 1.0f, 0.0f));

                        if (description.TryGetAnimationCurve(clips[i].name, "uv_offset.y", out curve))
                        {
                            ConvertKeys(curve, ConvertFloatNegate);
                            clips[i].SetCurve("", typeof(Material), "_UvOffset.y", curve);
                        }
                        else
                            clips[i].SetCurve("", typeof(Material), "_UvOffset.y", AnimationCurve.Constant(0.0f, 1.0f, 0.0f));
                    }
                }

                if (description.HasAnimationCurve("emissive_intensity"))
                {
                    Vector4 emissiveColor;
                    description.TryGetProperty("emissive", out emissiveColor);

                    for (int i = 0; i < clips.Length; i++)
                    {
                        AnimationCurve curve;
                        description.TryGetAnimationCurve(clips[i].name, "emissive_intensity", out curve);
                        // remap emissive intensity to emission color
                        clips[i].SetCurve("", typeof(Material), "_EmissionColor.r", curve);
                        clips[i].SetCurve("", typeof(Material), "_EmissionColor.g", curve);
                        clips[i].SetCurve("", typeof(Material), "_EmissionColor.b", curve);
                    }
                }
                else if (description.TryGetProperty("emissive", out vectorProperty))
                {
                    if (vectorProperty.x > 0.0f || vectorProperty.y > 0.0f || vectorProperty.z > 0.0f)
                    {
                        material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.RealtimeEmissive;
                        material.EnableKeyword("_EMISSION");
                    }

                    if (description.TryGetProperty("emissive_intensity", out floatProperty))
                    {
                        vectorProperty *= floatProperty;
                    }

                    material.SetColor("_EmissionColor", vectorProperty);


                    if (description.HasAnimationCurve("emissive.x"))
                    {
                        if (description.HasAnimationCurve("emissive_intensity"))
                        {
                            // combine color and intensity.
                            for (int i = 0; i < clips.Length; i++)
                            {
                                AnimationCurve curve;
                                AnimationCurve intensityCurve;
                                description.TryGetAnimationCurve(clips[i].name, "emissive_intensity", out intensityCurve);

                                description.TryGetAnimationCurve(clips[i].name, "emissive.x", out curve);
                                MultiplyCurves(curve, intensityCurve);
                                clips[i].SetCurve("", typeof(Material), "_EmissionColor.r", curve);

                                description.TryGetAnimationCurve(clips[i].name, "emissive.y", out curve);
                                MultiplyCurves(curve, intensityCurve);
                                clips[i].SetCurve("", typeof(Material), "_EmissionColor.g", curve);

                                description.TryGetAnimationCurve(clips[i].name, "emissive.z", out curve);
                                MultiplyCurves(curve, intensityCurve);
                                clips[i].SetCurve("", typeof(Material), "_EmissionColor.b", curve);
                            }
                        }
                        else
                        {
                            RemapColorCurves(description, clips, "emissive", "_EmissionColor");
                        }
                    }
                    else if (description.HasAnimationCurve("emissive_intensity"))
                    {
                        Vector4 emissiveColor;
                        description.TryGetProperty("emissive", out emissiveColor);

                        for (int i = 0; i < clips.Length; i++)
                        {
                            AnimationCurve curve;
                            description.TryGetAnimationCurve(clips[i].name, "emissive_intensity", out curve);
                            // remap emissive intensity to emission color
                            AnimationCurve curveR = new AnimationCurve();
                            ConvertAndCopyKeys(curveR, curve, value => ConvertFloatMultiply(emissiveColor.x, value));
                            clips[i].SetCurve("", typeof(Material), "_EmissionColor.r", curveR);

                            AnimationCurve curveG = new AnimationCurve();
                            ConvertAndCopyKeys(curveG, curve, value => ConvertFloatMultiply(emissiveColor.y, value));
                            clips[i].SetCurve("", typeof(Material), "_EmissionColor.g", curveG);

                            AnimationCurve curveB = new AnimationCurve();
                            ConvertAndCopyKeys(curveB, curve, value => ConvertFloatMultiply(emissiveColor.z, value));
                            clips[i].SetCurve("", typeof(Material), "_EmissionColor.b", curveB);
                        }
                    }
                }
            }
        }

        static bool IsAutodeskInteractiveMaterial(MaterialDescription description)
        {
            return description.TryGetProperty("renderAPI", out string stringValue) && stringValue == "SFX_PBS_SHADER";
        }

        static void ConvertKeys(AnimationCurve curve, System.Func<float, float> convertionDelegate)
        {
            Keyframe[] keyframes = curve.keys;
            for (int i = 0; i < keyframes.Length; i++)
            {
                keyframes[i].value = convertionDelegate(keyframes[i].value);
            }
            curve.keys = keyframes;
        }

        static void ConvertAndCopyKeys(AnimationCurve curveDest, AnimationCurve curveSource, System.Func<float, float> convertionDelegate)
        {
            for (int i = 0; i < curveSource.keys.Length; i++)
            {
                var sourceKey = curveSource.keys[i];
                curveDest.AddKey(new Keyframe(sourceKey.time, convertionDelegate(sourceKey.value), sourceKey.inTangent, sourceKey.outTangent, sourceKey.inWeight, sourceKey.outWeight));
            }
        }

        static float ConvertFloatNegate(float value)
        {
            return -value;
        }

        static float ConvertFloatMultiply(float value, float multiplier)
        {
            return value * multiplier;
        }

        static void MultiplyCurves(AnimationCurve curve, AnimationCurve curveMultiplier)
        {
            Keyframe[] keyframes = curve.keys;
            for (int i = 0; i < keyframes.Length; i++)
            {
                keyframes[i].value *= curveMultiplier.Evaluate(keyframes[i].time);
            }
            curve.keys = keyframes;
        }

        static void RemapCurve(MaterialDescription description, AnimationClip[] clips, string originalPropertyName, string newPropertyName)
        {
            AnimationCurve curve;
            for (int i = 0; i < clips.Length; i++)
            {
                if (description.TryGetAnimationCurve(clips[i].name, originalPropertyName, out curve))
                {
                    clips[i].SetCurve("", typeof(Material), newPropertyName, curve);
                }
            }
        }

        static void RemapColorCurves(MaterialDescription description, AnimationClip[] clips, string originalPropertyName, string newPropertyName)
        {
            AnimationCurve curve;
            for (int i = 0; i < clips.Length; i++)
            {
                if (description.TryGetAnimationCurve(clips[i].name, originalPropertyName + ".x", out curve))
                {
                    clips[i].SetCurve("", typeof(Material), newPropertyName + ".r", curve);
                }

                if (description.TryGetAnimationCurve(clips[i].name, originalPropertyName + ".y", out curve))
                {
                    clips[i].SetCurve("", typeof(Material), newPropertyName + ".g", curve);
                }

                if (description.TryGetAnimationCurve(clips[i].name, originalPropertyName + ".z", out curve))
                {
                    clips[i].SetCurve("", typeof(Material), newPropertyName + ".b", curve);
                }
            }
        }
    }
}
