using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace UnityEngine.PathTracing.Core
{
    internal enum MaterialPropertyType { None = 0, Color, Texture }

    internal struct MaterialPropertyDesc
    {
        public MaterialPropertyType Type;
        public float3 Color; // Unused if type == Textured
    }

    internal enum TransmissionChannels { None = 0, RGB, Alpha }

    internal struct TransmissionDesc
    {
        public Texture SourceTexture;
        public TransmissionChannels Channels;
        public Vector2 Scale;
        public Vector2 Offset;
    }

    internal static class MaterialAspectOracle
    {
        // See gi::HasBakedEmissive in Materials.cpp
        public static MaterialPropertyDesc GetEmission(Material mat)
        {
            // If the material is not marked as baked emissive, or if it is black, there is no emission
            var emissiveFlags = mat.globalIlluminationFlags;
            bool isBlack = emissiveFlags.HasFlag(MaterialGlobalIlluminationFlags.EmissiveIsBlack);
            bool isBaked = emissiveFlags.HasFlag(MaterialGlobalIlluminationFlags.BakedEmissive);
            if (isBlack || !isBaked)
            {
                return new MaterialPropertyDesc { Type = MaterialPropertyType.None, Color = float3.zero };
            }

            // If the material has an emission keyword, but it is disabled, there is no emission
            if (IsMaterialWithEmissionKeyword(mat) && !EnumerableArrayContains(mat.shaderKeywords, "_EMISSION"))
            {
                return new MaterialPropertyDesc { Type = MaterialPropertyType.None, Color = float3.zero };
            }

            // If we have reached this point, the material should be emissive.
            // First we check for an emissive texture, and use that if it exists
            if (HasEmissionMap(mat))
            {
                return new MaterialPropertyDesc { Type = MaterialPropertyType.Texture, Color = float3.zero };
            }

            // Otherwise, if the material only has an emissive color, we use that
            if (mat.HasProperty(SID.EmissionColor))
            {
                return new MaterialPropertyDesc { Type = MaterialPropertyType.Color, Color = ToFloat3(mat.GetColor(SID.EmissionColor)) };
            }

            // If we found neither property, we assume that the material has an unusual meta pass implementation,
            // which will render the emission - thus we use texture mode
            return new MaterialPropertyDesc { Type = MaterialPropertyType.Texture, Color = float3.zero };
        }

        private static bool EnumerableArrayContains(IEnumerable<string> array, string value)
        {
            foreach (string element in array)
                if (EqualityComparer<string>.Default.Equals(element, value))
                    return true;

            return false;
        }

        // Check if a material has a property with a specific Shaderlab property flag
        private static bool MaterialHasPropertyWithFlag(Material mat, ShaderPropertyFlags flag)
        {
            if (mat.shader == null)
                return false;

            int numProperties = mat.shader.GetPropertyCount();
            for (int i = 0; i < numProperties; ++i)
            {
                var flags = mat.shader.GetPropertyFlags(i);
                if (flags.HasFlag(flag))
                {
                    return true;
                }
            }
            return false;
        }

        // See CreateBakeMaterial in ExtractBakeMaterials.cpp
        public static TransmissionDesc GetTransmission(Material mat)
        {
            // Full RGB transmission
            bool hasRGBTransparencyTexture = mat.HasProperty(SID.TransparencyLm) && mat.GetTexture(SID.TransparencyLm) != null;
            if (hasRGBTransparencyTexture)
            {
                return new TransmissionDesc
                {
                    Channels = TransmissionChannels.RGB,
                    SourceTexture = mat.GetTexture(SID.TransparencyLm),
                    Scale = mat.GetTextureScale(SID.TransparencyLm),
                    Offset = mat.GetTextureOffset(SID.TransparencyLm)
                };
            }

            // Alpha-only transmission, alpha from main texture (if exists)
            bool isOnTransparentQueue = mat.renderQueue >= (int)RenderQueue.AlphaTestRenderQueue && mat.renderQueue < (int)RenderQueue.OverlayRenderQueue;
            bool hasMainTexture = MaterialHasPropertyWithFlag(mat, ShaderPropertyFlags.MainTexture) || mat.HasProperty(SID.MainTex);
            if (isOnTransparentQueue && hasMainTexture)
            {
                return new TransmissionDesc
                {
                    Channels = TransmissionChannels.Alpha,
                    SourceTexture = mat.mainTexture,
                    Scale = mat.mainTextureScale,
                    Offset = mat.mainTextureOffset
                };
            }

            // No transmission
            return new TransmissionDesc { Channels = TransmissionChannels.None };
        }

        private static bool HasEmissionMap(Material mat)
        {
            return (mat.HasProperty(SID.EmissionMap) && mat.GetTexture(SID.EmissionMap) != null)
                && (!mat.HasProperty(SID.UseEmissiveMap) || mat.GetInt(SID.UseEmissiveMap) == 1);
        }

        private static bool IsMaterialWithEmissionKeyword(Material mat)
        {
            return EnumerableArrayContains(mat.shader.keywordSpace.keywordNames, "_EMISSION");
        }

        public static float GetAlpha(Material mat)
        {
            if (mat.HasProperty(SID.BaseColor))
            {
                return mat.GetColor(SID.BaseColor).a;
            }
            else if (mat.HasProperty(SID.Color))
            {
                return mat.GetColor(SID.Color).a;
            }
            else
            {
                for (int i = 0; i < mat.shader.GetPropertyCount(); i++)
                {
                    if (mat.shader.GetPropertyFlags(i).HasFlag(ShaderPropertyFlags.MainColor))
                    {
                        return mat.GetColor(mat.shader.GetPropertyNameId(i)).a;
                    }
                }
                
                return 1.0f;
            }
        }

        public static bool UsesAlphaCutoff(Material mat)
        {
            bool alphaTestQueue = mat.renderQueue >= (int)RenderQueue.AlphaTestRenderQueue && mat.renderQueue < (int)RenderQueue.TransparentRenderQueue;
            if (!alphaTestQueue)
                return false;

            return mat.HasProperty(SID.Cutoff) || mat.HasProperty(SID.AlphaTestRef);
        }

        public static float GetAlphaCutoff(Material mat)
        {
            if (mat.HasProperty(SID.Cutoff))
            {
                return mat.GetFloat(SID.Cutoff);
            }
            else if (mat.HasProperty(SID.AlphaTestRef))
            {
                return mat.GetFloat(SID.AlphaTestRef);
            }
            return 0.0f;
        }

        private static float3 ToFloat3(Color col)
        {
            return new float3(col.r, col.g, col.b);
        }

        private enum RenderQueue
        {
            GeometryRenderQueue = 2000,
            AlphaTestRenderQueue = 2450,
            TransparentRenderQueue = 3000,
            OverlayRenderQueue = 4000,
        }

        private static class SID
        {
            public static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
            public static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");
            public static readonly int UseEmissiveMap = Shader.PropertyToID("_UseEmissiveMap");
            public static readonly int TransparencyLm = Shader.PropertyToID("_TransparencyLM");
            public static readonly int Color = Shader.PropertyToID("_Color");
            public static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
            public static readonly int Cutoff = Shader.PropertyToID("_Cutoff");
            public static readonly int AlphaTestRef = Shader.PropertyToID("_AlphaTestRef");
            public static readonly int MainTex = Shader.PropertyToID("_MainTex");
        }

        // TODO(Yvain) Bump, roughness, metalness ?
    }
}
