using System;
using System.IO;
using UnityEngine;
using UnityEditor.AssetImporters;

namespace UnityEditor.Rendering.Universal
{
    class ThreeDSMaterialDescriptionPreprocessor : AssetPostprocessor
    {
        static readonly uint k_Version = 1;
        static readonly int k_Order = 2;

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
            var lowerCasePath = Path.GetExtension(assetPath).ToLower();
            if (lowerCasePath != ".3ds")
                return;

            string path = AssetDatabase.GUIDToAssetPath(ShaderUtils.GetShaderGUID(ShaderPathID.Lit));
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader == null)
                return;
            material.shader = shader;

            TexturePropertyDescription textureProperty;
            float floatProperty;
            Vector4 vectorProperty;

            description.TryGetProperty("diffuse", out vectorProperty);
            vectorProperty.x /= 255.0f;
            vectorProperty.y /= 255.0f;
            vectorProperty.z /= 255.0f;
            vectorProperty.w /= 255.0f;
            description.TryGetProperty("transparency", out floatProperty);

            bool isTransparent = vectorProperty.w <= 0.99f || floatProperty > .0f;
            if (isTransparent)
            {
                material.SetFloat("_Mode", (float)3.0); // From C# enum BlendMode
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.SetInt("_Surface", 1);
            }
            else
            {
                material.SetFloat("_Mode", (float)0.0); // From C# enum BlendMode
                material.SetOverrideTag("RenderType", "");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
                material.SetInt("_Surface", 0);
            }

            if (floatProperty > .0f)
                vectorProperty.w = 1.0f - floatProperty;

            Color diffuseColor = vectorProperty;

            material.SetColor("_Color", PlayerSettings.colorSpace == ColorSpace.Linear ? diffuseColor.gamma : diffuseColor);
            material.SetColor("_BaseColor", PlayerSettings.colorSpace == ColorSpace.Linear ? diffuseColor.gamma : diffuseColor);

            if (description.TryGetProperty("EmissiveColor", out vectorProperty))
            {
                material.SetColor("_EmissionColor", vectorProperty);
                material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.RealtimeEmissive;
                material.EnableKeyword("_EMISSION");
            }

            if (description.TryGetProperty("texturemap1", out textureProperty))
            {
                SetMaterialTextureProperty("_BaseMap", material, textureProperty);
            }

            if (description.TryGetProperty("bumpmap", out textureProperty))
            {
                if (material.HasProperty("_BumpMap"))
                {
                    SetMaterialTextureProperty("_BumpMap", material, textureProperty);
                    material.EnableKeyword("_NORMALMAP");
                }
            }
        }

        static void SetMaterialTextureProperty(string propertyName, Material material, TexturePropertyDescription textureProperty)
        {
            material.SetTexture(propertyName, textureProperty.texture);
            material.SetTextureOffset(propertyName, textureProperty.offset);
            material.SetTextureScale(propertyName, textureProperty.scale);
        }
    }
}

