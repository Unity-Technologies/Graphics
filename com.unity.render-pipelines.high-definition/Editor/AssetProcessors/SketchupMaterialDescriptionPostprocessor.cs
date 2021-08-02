using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.AssetImporters;

namespace UnityEditor.Rendering.HighDefinition
{
    class SketchupMaterialDescriptionPreprocessor : AssetPostprocessor
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
            var pipelineAsset = GraphicsSettings.currentRenderPipeline;
            if (!pipelineAsset || pipelineAsset.GetType() != typeof(HDRenderPipelineAsset))
                return;

            var lowerCasePath = Path.GetExtension(assetPath).ToLower();
            if (lowerCasePath != ".skp")
                return;

            var shader = Shader.Find("HDRP/Lit");
            if (shader == null)
                return;
            material.shader = shader;

            float floatProperty;
            Vector4 vectorProperty;
            TexturePropertyDescription textureProperty;

            material.SetShaderPassEnabled("DistortionVectors", false);
            material.SetShaderPassEnabled("TransparentDepthPrepass",false);
            material.SetShaderPassEnabled("TransparentDepthPostpass", false);
            material.SetShaderPassEnabled("TransparentBackface", false);
            material.SetShaderPassEnabled("MOTIONVECTORS", false);

			if (description.TryGetProperty("DiffuseMap", out textureProperty) && textureProperty.texture!=null)
            {
                SetMaterialTextureProperty("_BaseColorMap", material, textureProperty);
                SetMaterialTextureProperty("_MainTex", material, textureProperty);
				var color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                material.SetColor("_BaseColor", color);
                material.SetColor("_Color", color);
            }
			else if (description.TryGetProperty("DiffuseColor", out vectorProperty))
            {
				Color diffuseColor = vectorProperty;
				diffuseColor = PlayerSettings.colorSpace == ColorSpace.Linear ? diffuseColor.gamma : diffuseColor;
                material.SetColor("_BaseColor", diffuseColor);
                material.SetColor("_Color", diffuseColor);
            }
            if (description.TryGetProperty("IsTransparent", out floatProperty) && floatProperty == 1.0f)
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
        }

        static void SetMaterialTextureProperty(string propertyName, Material material, TexturePropertyDescription textureProperty)
        {
            material.SetTexture(propertyName, textureProperty.texture);
            material.SetTextureOffset(propertyName, textureProperty.offset);
            material.SetTextureScale(propertyName, textureProperty.scale);
        }
    }
}

