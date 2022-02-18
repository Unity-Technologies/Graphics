using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.AssetImporters;

namespace UnityEditor.Rendering.HighDefinition
{
    class ThreeDSMaterialDescriptionPreprocessor : AssetPostprocessor
    {
        static readonly uint k_Version = 2;
        static readonly int k_Order = -980;

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
            if (HDRenderPipeline.currentAsset == null)
                return;

            var lowerCasePath = Path.GetExtension(assetPath).ToLower();
            if (lowerCasePath != ".3ds")
                return;

            var shader = Shader.Find("HDRP/Lit");
            if (shader == null)
                return;
            material.shader = shader;

            material.SetShaderPassEnabled("DistortionVectors", false);
            material.SetShaderPassEnabled("TransparentDepthPrepass", false);
            material.SetShaderPassEnabled("TransparentDepthPostpass", false);
            material.SetShaderPassEnabled("TransparentBackface", false);
            material.SetShaderPassEnabled("MOTIONVECTORS", false);

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
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.SetFloat("_BlendMode", (float)BlendMode.Alpha);
                material.SetFloat("_EnableBlendModePreserveSpecularLighting", 1.0f);
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.EnableKeyword("_ENABLE_FOG_ON_TRANSPARENT");
                material.EnableKeyword("_ALPHATEST_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.SetFloat("_SurfaceType", 1.0f);
                material.SetFloat("_Cutoff", .0f);
                material.SetFloat("_AlphaCutoffEnable", 1.0f);
                material.SetFloat("_AlphaCutoff", .0f);
                material.SetFloat("_AlphaCutoffShadow", 1.0f);
                material.SetFloat("_UseShadowThreshold", 1.0f);
            }
            else
            {
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.renderQueue = -1;
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
                SetMaterialTextureProperty("_MainTex", material, textureProperty);
                SetMaterialTextureProperty("_BaseColorMap", material, textureProperty);
            }

            if (description.TryGetProperty("bumpmap", out textureProperty) && textureProperty.texture != null)
            {
                if (material.HasProperty("_BumpMap"))
                {
                    SetMaterialTextureProperty("_BumpMap", material, textureProperty);
                    material.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
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
