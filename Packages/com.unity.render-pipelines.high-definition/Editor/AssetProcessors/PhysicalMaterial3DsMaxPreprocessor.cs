using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace UnityEditor.Rendering.HighDefinition
{
    class PhysicalMaterial3DsMaxPreprocessor : AssetPostprocessor
    {
        static readonly uint k_Version = 1;
        static readonly int k_Order = 4;
        static readonly string k_ShaderPath = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/PhysicalMaterial3DsMax/PhysicalMaterial3DsMax.shadergraph";

        public override uint GetVersion()
        {
            return k_Version;
        }

        public override int GetPostprocessOrder()
        {
            return k_Order;
        }

        static bool Is3DsMaxPhysicalMaterial(MaterialDescription description)
        {
            float classIdA;
            float classIdB;
            string originalMtl;
            description.TryGetProperty("ClassIDa", out classIdA);
            description.TryGetProperty("ClassIDb", out classIdB);
            description.TryGetProperty("ORIGINAL_MTL", out originalMtl);
            return classIdA == 1030429932 && classIdB == -559038463 || originalMtl == "PHYSICAL_MTL";
        }

        static bool Is3DsMaxSimplifiedPhysicalMaterial(MaterialDescription description)
        {
            float classIdA;
            float classIdB;
            float useGlossiness;
            description.TryGetProperty("ClassIDa", out classIdA);
            description.TryGetProperty("ClassIDb", out classIdB);
            description.TryGetProperty("useGlossiness", out useGlossiness);

            return classIdA == -804315648 && classIdB == -1099438848 && useGlossiness == 2.0f;
        }

        public void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            if (HDRenderPipeline.currentAsset == null)
                return;

            if (Is3DsMaxPhysicalMaterial(description))
            {
                CreateFrom3DsPhysicalMaterial(description, material, clips);
            }
            else if (Is3DsMaxSimplifiedPhysicalMaterial(description))
            {
                CreateFrom3DsSimplifiedPhysicalMaterial(description, material, clips);
            }
        }

        void CreateFrom3DsSimplifiedPhysicalMaterial(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            float floatProperty;
            Vector4 vectorProperty;
            TexturePropertyDescription textureProperty;

            description.TryGetProperty("basecolor", out vectorProperty);
            bool hasTransparencyScalar = vectorProperty.w != 1.0f;
            var hasTransparencyMap = description.TryGetProperty("opacity_map", out textureProperty);
            bool isTransparent = hasTransparencyMap | hasTransparencyScalar;


            Shader shader;
            if (isTransparent)
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

            if (hasTransparencyMap)
            {
                material.SetFloat("_UseOpacityMap", 1.0f);
                material.SetTexture("_OpacityMap", textureProperty.texture);
            }
            else if (hasTransparencyScalar)
            {
                material.SetFloat("_Opacity", vectorProperty.w);
            }

            if (description.TryGetProperty("basecolor", out vectorProperty))
                material.SetColor("_Color", vectorProperty);

            if (description.TryGetProperty("emit_color", out vectorProperty))
                material.SetColor("_EmissionColor", vectorProperty);

            if (description.TryGetProperty("roughness", out floatProperty))
                material.SetFloat("_Glossiness", floatProperty);

            if (description.TryGetProperty("metalness", out floatProperty))
                material.SetFloat("_Metallic", floatProperty);

            if (description.TryGetProperty("base_color_map", out textureProperty))
            {
                material.SetTexture("_MainTex", textureProperty.texture);
                material.SetFloat("_UseColorMap", 1.0f);
                material.SetColor("_UvTiling", new Vector4(textureProperty.scale.x, textureProperty.scale.y, 0.0f, 0.0f));
                material.SetColor("_UvOffset", new Vector4(textureProperty.offset.x, textureProperty.offset.y, 0.0f, 0.0f));
            }
            else
            {
                material.SetFloat("_UseColorMap", 0.0f);
            }

            if (description.TryGetProperty("norm_map", out textureProperty))
            {
                material.SetTexture("_BumpMap", textureProperty.texture);
                material.SetFloat("_UseNormalMap", 1.0f);
            }
            else
            {
                material.SetFloat("_UseNormalMap", 0.0f);
            }

            if (description.TryGetProperty("roughness_map", out textureProperty))
            {
                material.SetTexture("_SpecGlossMap", textureProperty.texture);
                material.SetFloat("_UseRoughnessMap", 1.0f);
            }
            else
            {
                material.SetFloat("_UseRoughnessMap", 0.0f);
            }

            if (description.TryGetProperty("metalness_map", out textureProperty))
            {
                material.SetTexture("_MetallicGlossMap", textureProperty.texture);
                material.SetFloat("_UseMetallicMap", 1.0f);
            }
            else
            {
                material.SetFloat("_UseMetallicMap", 0.0f);
            }

            if (description.TryGetProperty("emit_color_map", out textureProperty))
            {
                material.SetTexture("_EmissionMap", textureProperty.texture);
                material.SetFloat("_UseEmissiveMap", 1.0f);
            }
            else
            {
                material.SetFloat("_UseEmissiveMap", 0.0f);
            }

            if (description.TryGetProperty("ao_map", out textureProperty))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture>(textureProperty.relativePath);
                material.SetTexture("AoMap", tex);
                material.SetFloat("UseAoMap", 1.0f);
            }
            else
            {
                material.SetFloat("UseAoMap", 0.0f);
            }
        }

        void CreateFrom3DsPhysicalMaterial(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            float floatProperty;
            Vector4 vectorProperty;
            TexturePropertyDescription textureProperty;


            var shader = AssetDatabase.LoadAssetAtPath<Shader>(k_ShaderPath);
            if (shader == null)
                return;

            material.shader = shader;
            foreach (var clip in clips)
            {
                clip.ClearCurves();
            }

            description.TryGetProperty("transparency", out floatProperty);
            bool hasTransparencyMap =
                description.TryGetProperty("transparency_map", out textureProperty);

            if (floatProperty > 0.0f || hasTransparencyMap)
            {
                if (hasTransparencyMap)
                {
                    material.SetTexture("_TRANSPARENCY_MAP", textureProperty.texture);
                    material.SetFloat("_TRANSPARENCY", 1.0f);
                }
                else
                {
                    material.SetFloat("_TRANSPARENCY", floatProperty);
                }

                material.SetInt("_SrcBlend", 1);
                material.SetInt("_DstBlend", 10);
                material.SetFloat("_BlendMode", (float)BlendMode.Alpha);
                material.SetFloat("_EnableBlendModePreserveSpecularLighting", 1.0f);
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.EnableKeyword("_ENABLE_FOG_ON_TRANSPARENT");
                material.renderQueue = 3000;
            }
            else
            {
                material.EnableKeyword("_DOUBLESIDED_ON");
                material.SetInt("_CullMode", 0);
                material.SetInt("_CullModeForward", 0);
                material.doubleSidedGI = true;
            }

            RemapPropertyFloat(description, material, "base_weight", "_BASE_COLOR_WEIGHT");
            if (description.TryGetProperty("base_color_map", out textureProperty))
            {
                SetMaterialTextureProperty("_BASE_COLOR_MAP", material, textureProperty);
            }
            else if (description.TryGetProperty("base_color", out vectorProperty))
            {
                if (QualitySettings.activeColorSpace == ColorSpace.Gamma)
                {
                    vectorProperty.x = Mathf.LinearToGammaSpace(vectorProperty.x);
                    vectorProperty.y = Mathf.LinearToGammaSpace(vectorProperty.y);
                    vectorProperty.z = Mathf.LinearToGammaSpace(vectorProperty.z);
                    vectorProperty.w = Mathf.LinearToGammaSpace(vectorProperty.w);
                }
                material.SetColor("_BASE_COLOR", vectorProperty);
            }

            RemapPropertyFloat(description, material, "reflectivity", "_REFLECTIONS_WEIGHT");
            RemapPropertyTextureOrColor(description, material, "refl_color", "_REFLECTIONS_COLOR");
            RemapPropertyTextureOrFloat(description, material, "metalness", "_METALNESS");
            RemapPropertyTextureOrFloat(description, material, "roughness", "_REFLECTIONS_ROUGHNESS");
            RemapPropertyTextureOrFloat(description, material, "trans_ior", "_REFLECTIONS_IOR");
            RemapPropertyFloat(description, material, "emission", "_EMISSION_WEIGHT");
            RemapPropertyTextureOrColor(description, material, "emit_color", "_EMISSION_COLOR");

            RemapPropertyTextureOrFloat(description, material, "anisotropy", "_ANISOTROPY");

            RemapPropertyFloat(description, material, "bump_map_amt", "_BUMP_MAP_STRENGTH");
            RemapPropertyTexture(description, material, "bump_map", "_BUMP_MAP");
        }

        static void SetMaterialTextureProperty(string propertyName, Material material,
            TexturePropertyDescription textureProperty)
        {
            material.SetTexture(propertyName, textureProperty.texture);
            material.SetTextureOffset(propertyName, textureProperty.offset);
            material.SetTextureScale(propertyName, textureProperty.scale);
        }

        static void RemapPropertyFloat(MaterialDescription description, Material material, string inPropName,
            string outPropName)
        {
            if (description.TryGetProperty(inPropName, out float floatProperty))
            {
                material.SetFloat(outPropName, floatProperty);
            }
        }

        static void RemapPropertyTexture(MaterialDescription description, Material material, string inPropName,
            string outPropName)
        {
            if (description.TryGetProperty(inPropName, out TexturePropertyDescription textureProperty))
            {
                material.SetTexture(outPropName, textureProperty.texture);
            }
        }

        static void RemapPropertyTextureOrColor(MaterialDescription description, Material material,
            string inPropName, string outPropName)
        {
            if (description.TryGetProperty(inPropName + "_map", out TexturePropertyDescription textureProperty))
            {
                material.SetTexture(outPropName + "_MAP", textureProperty.texture);
                material.SetColor(outPropName, Color.white);
            }
            else if (description.TryGetProperty(inPropName, out Vector4 color))
            {
                material.SetColor(outPropName, color);
            }
        }

        static void RemapPropertyTextureOrFloat(MaterialDescription description, Material material,
            string inPropName, string outPropName)
        {
            if (description.TryGetProperty(inPropName + "_map", out TexturePropertyDescription textureProperty))
            {
                material.SetTexture(outPropName + "_MAP", textureProperty.texture);
                material.SetFloat(outPropName, 1.0f);
            }
            else if (description.TryGetProperty(inPropName, out float floatProperty))
            {
                material.SetFloat(outPropName, floatProperty);
            }
        }
    }
}
