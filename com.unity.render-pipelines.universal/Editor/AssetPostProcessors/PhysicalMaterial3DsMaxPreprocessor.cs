using UnityEditor.AssetImporters;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor.Rendering.Universal
{
    class PhysicalMaterial3DsMaxPreprocessor : AssetPostprocessor
    {
        static readonly uint k_Version = 1;
        static readonly int k_Order = 4;
        static readonly string k_ShaderPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/PhysicalMaterial3DsMax/PhysicalMaterial3DsMax.ShaderGraph";
        static readonly string k_ShaderTransparentPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/PhysicalMaterial3DsMax/PhysicalMaterial3DsMaxTransparent.ShaderGraph";

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
            description.TryGetProperty("ClassIDa", out classIdA);
            description.TryGetProperty("ClassIDb", out classIdB);
            return classIdA == 1030429932 && classIdB == -559038463;
        }

        public void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            if (Is3DsMaxPhysicalMaterial(description))
            {
                CreateFrom3DsPhysicalMaterial(description, material, clips);
            }
        }

        void CreateFrom3DsPhysicalMaterial(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            float floatProperty;
            Vector4 vectorProperty;
            TexturePropertyDescription textureProperty;
            Shader shader;

            description.TryGetProperty("transparency", out floatProperty);
            bool hasTransparencyMap =
                description.TryGetProperty("transparency_map", out textureProperty);

            if (floatProperty > 0.0f || hasTransparencyMap)
            {
                shader = AssetDatabase.LoadAssetAtPath<Shader>(k_ShaderTransparentPath);
                if (shader == null)
                    return;

                material.shader = shader;
                if (hasTransparencyMap)
                {
                    material.SetTexture("_TRANSPARENCY_MAP", textureProperty.texture);
                    material.SetFloat("_TRANSPARENCY", 1.0f);
                }
                else
                {
                    material.SetFloat("_TRANSPARENCY", floatProperty);
                }
            }
            else
            {
                shader = AssetDatabase.LoadAssetAtPath<Shader>(k_ShaderPath);
                if (shader == null)
                    return;

                material.shader = shader;
            }

            foreach (var clip in clips)
            {
                clip.ClearCurves();
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
            else if(description.TryGetProperty(inPropName, out Vector4 color))
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
            else if(description.TryGetProperty(inPropName, out float floatProperty))
            {
                material.SetFloat(outPropName, floatProperty);
            }
        }
    }
}
