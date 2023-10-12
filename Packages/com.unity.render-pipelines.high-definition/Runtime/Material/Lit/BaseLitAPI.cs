using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Rendering.HighDefinition;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEngine.Rendering.HighDefinition
{
    abstract class BaseLitAPI
    {
        // Wind
        protected const string kWindEnabled = "_EnableWind";

        public static DisplacementMode GetFilteredDisplacementMode(Material material)
        {
            return material.GetFilteredDisplacementMode((DisplacementMode)material.GetFloat(kDisplacementMode));
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupBaseLitKeywords(Material material)
        {
            material.SetupBaseUnlitKeywords();

            bool doubleSidedEnable = material.HasProperty(kDoubleSidedEnable) ? material.GetFloat(kDoubleSidedEnable) > 0.0f : false;
            if (doubleSidedEnable)
            {
                DoubleSidedNormalMode doubleSidedNormalMode = (DoubleSidedNormalMode)material.GetFloat(kDoubleSidedNormalMode);
                switch (doubleSidedNormalMode)
                {
                    case DoubleSidedNormalMode.Mirror: // Mirror mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(1.0f, 1.0f, -1.0f, 0.0f));
                        break;

                    case DoubleSidedNormalMode.Flip: // Flip mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(-1.0f, -1.0f, -1.0f, 0.0f));
                        break;

                    case DoubleSidedNormalMode.None: // None mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
                        break;
                }
            }

            // Displacement Mapping
            {
                bool enableVertexDisplacement = false;
                bool enablePixelDisplacement = false;
                bool enableTessellationDisplacement = false;
                bool displacementLockObjectScale = false;
                bool displacementLockTilingScale = false;
                bool enableDepthOffset = false;
                bool conservativeDepthOffset = false;

                bool enableDisplacement = material.HasProperty(kDisplacementMode) && (GetFilteredDisplacementMode(material) != DisplacementMode.None);

                if (enableDisplacement)
                {
                    var displacementMode = GetFilteredDisplacementMode(material);

                    enableVertexDisplacement = displacementMode == DisplacementMode.Vertex;
                    enablePixelDisplacement = displacementMode == DisplacementMode.Pixel;
                    enableTessellationDisplacement = displacementMode == DisplacementMode.Tessellation;

                    displacementLockObjectScale = material.GetFloat(kDisplacementLockObjectScale) > 0.0f;
                    displacementLockTilingScale = material.GetFloat(kDisplacementLockTilingScale) > 0.0f;
                }

                // Depth Offset may be used without Displacement Mapping as well (a Shader Graph feature).
                if (enablePixelDisplacement || (!material.HasProperty(kDisplacementMode) && material.HasProperty(kDepthOffsetEnable)))
                {
                    enableDepthOffset = material.GetFloat(kDepthOffsetEnable) > 0.0f;
                }

                if (enableDepthOffset && material.HasProperty(kConservativeDepthOffsetEnable))
                {
                    conservativeDepthOffset = material.GetFloat(kConservativeDepthOffsetEnable) > 0.0f;
                }

                CoreUtils.SetKeyword(material, "_VERTEX_DISPLACEMENT", enableVertexDisplacement);
                CoreUtils.SetKeyword(material, "_PIXEL_DISPLACEMENT", enablePixelDisplacement);
                CoreUtils.SetKeyword(material, "_TESSELLATION_DISPLACEMENT", enableTessellationDisplacement);

                // Tessellation reuse vertex flag.
                CoreUtils.SetKeyword(material, "_VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE", displacementLockObjectScale && (enableVertexDisplacement || enableTessellationDisplacement));
                CoreUtils.SetKeyword(material, "_PIXEL_DISPLACEMENT_LOCK_OBJECT_SCALE", displacementLockObjectScale && enablePixelDisplacement);
                CoreUtils.SetKeyword(material, "_DISPLACEMENT_LOCK_TILING_SCALE", displacementLockTilingScale && enableDisplacement);

                CoreUtils.SetKeyword(material, "_DEPTHOFFSET_ON", enableDepthOffset);
                CoreUtils.SetKeyword(material, "_CONSERVATIVE_DEPTH_OFFSET", conservativeDepthOffset);
            }

            CoreUtils.SetKeyword(material, "_VERTEX_WIND", false);

            material.SetupMainTexForAlphaTestGI("_BaseColorMap", "_BaseColor");

            // Use negation so we don't create keyword by default
            CoreUtils.SetKeyword(material, "_DISABLE_DECALS", material.HasProperty(kSupportDecals) && material.GetFloat(kSupportDecals) == 0.0f);
            CoreUtils.SetKeyword(material, "_DISABLE_SSR", material.HasProperty(kReceivesSSR) && material.GetFloat(kReceivesSSR) == 0.0f);
            CoreUtils.SetKeyword(material, "_DISABLE_SSR_TRANSPARENT", material.HasProperty(kReceivesSSRTransparent) && material.GetFloat(kReceivesSSRTransparent) == 0.0f);
            CoreUtils.SetKeyword(material, "_ENABLE_GEOMETRIC_SPECULAR_AA", material.HasProperty(kEnableGeometricSpecularAA) && material.GetFloat(kEnableGeometricSpecularAA) == 1.0f);

            if (material.HasProperty(kRefractionModel))
            {
                var refractionModel = material.GetRefractionModel();
                CoreUtils.SetKeyword(material, "_REFRACTION_PLANE", refractionModel == ScreenSpaceRefraction.RefractionModel.Planar);
                CoreUtils.SetKeyword(material, "_REFRACTION_SPHERE", refractionModel == ScreenSpaceRefraction.RefractionModel.Sphere);
                CoreUtils.SetKeyword(material, "_REFRACTION_THIN", refractionModel == ScreenSpaceRefraction.RefractionModel.Thin);
            }
        }

        static public bool CompatibleWithExcludeFromTUAndAA(SurfaceType surfaceType, int renderQueue)
        {
            return surfaceType == SurfaceType.Transparent && HDRenderQueue.k_RenderQueue_Transparent.Contains(renderQueue);
        }

        static public bool CompatibleWithExcludeFromTUAndAA(Material material)
        {
            return CompatibleWithExcludeFromTUAndAA(material.GetSurfaceType(), material.renderQueue) && material.HasProperty(kExcludeFromTUAndAA);
        }

        static public void SetupStencil(Material material, bool receivesLighting, bool receivesSSR, bool useSplitLighting, bool excludeFromTUAndAA = false)
        {
            // To determine if the shader is forward only, we can't rely on the presence of GBuffer pass because that depends on the active subshader, which
            // depends on the active render pipeline, giving an inconsistent result. The properties of a shader are always the same so it's ok to check them
            bool forwardOnly = material.shader.FindPropertyIndex(kZTestGBuffer) == -1;
            bool hasRefraction = material.GetRefractionModel() != ScreenSpaceRefraction.RefractionModel.None;

            ComputeStencilProperties(
                receivesLighting,
                forwardOnly,
                receivesSSR,
                useSplitLighting,
                hasRefraction,
                excludeFromTUAndAA,
                out int stencilRef,
                out int stencilWriteMask,
                out int stencilRefDepth,
                out int stencilWriteMaskDepth,
                out int stencilRefGBuffer,
                out int stencilWriteMaskGBuffer,
                out int stencilRefMV,
                out int stencilWriteMaskMV);

            // As we tag both during motion vector pass and Gbuffer pass we need a separate state and we need to use the write mask
            if (material.HasProperty(kStencilRef))
            {
                material.SetInt(kStencilRef, stencilRef);
                material.SetInt(kStencilWriteMask, stencilWriteMask);
            }
            if (material.HasProperty(kStencilRefDepth))
            {
                material.SetInt(kStencilRefDepth, stencilRefDepth);
                material.SetInt(kStencilWriteMaskDepth, stencilWriteMaskDepth);
            }
            if (material.HasProperty(kStencilRefGBuffer))
            {
                material.SetInt(kStencilRefGBuffer, stencilRefGBuffer);
                material.SetInt(kStencilWriteMaskGBuffer, stencilWriteMaskGBuffer);
            }
            if (material.HasProperty(kStencilRefDistortionVec))
            {
                material.SetInt(kStencilRefDistortionVec, (int)StencilUsage.DistortionVectors);
                material.SetInt(kStencilWriteMaskDistortionVec, (int)StencilUsage.DistortionVectors);
            }
            if (material.HasProperty(kStencilRefMV))
            {
                material.SetInt(kStencilRefMV, stencilRefMV);
                material.SetInt(kStencilWriteMaskMV, stencilWriteMaskMV);
            }
        }

        static public void ComputeStencilProperties(
            bool receivesLighting,
            bool forwardOnly,
            bool receivesSSR,
            bool useSplitLighting,
            bool hasRefraction,
            bool excludeFromTUAndAA,
            out int stencilRef,
            out int stencilWriteMask,
            out int stencilRefDepth,
            out int stencilWriteMaskDepth,
            out int stencilRefGBuffer,
            out int stencilWriteMaskGBuffer,
            out int stencilRefMV,
            out int stencilWriteMaskMV)
        {
            // Stencil usage rules:
            // TraceReflectionRay need to be tagged during depth prepass
            // RequiresDeferredLighting need to be tagged during GBuffer
            // SubsurfaceScattering need to be tagged during either GBuffer or Forward pass
            // ObjectMotionVectors need to be tagged in velocity pass.
            // As motion vectors pass can be use as a replacement of depth prepass it also need to have TraceReflectionRay
            // As GBuffer pass can have no depth prepass, it also need to have TraceReflectionRay
            // Object motion vectors is always render after a full depth buffer (if there is no depth prepass for GBuffer all object motion vectors are render after GBuffer)
            // so we have a guarantee than when we write object motion vectors no other object will be draw on top (and so would have require to overwrite motion vectors).
            // Final combination is:
            // Prepass: TraceReflectionRay
            // Motion vectors: TraceReflectionRay, ObjectVelocity
            // GBuffer: LightingMask, ObjectVelocity
            // Forward: LightingMask

            stencilRef = (int)StencilUsage.Clear; // Forward case
            stencilWriteMask = (int)StencilUsage.RequiresDeferredLighting | (int)StencilUsage.SubsurfaceScattering;
            stencilRefDepth = 0;
            stencilWriteMaskDepth = 0;
            stencilRefGBuffer = (int)StencilUsage.RequiresDeferredLighting;
            stencilWriteMaskGBuffer = (int)StencilUsage.RequiresDeferredLighting | (int)StencilUsage.SubsurfaceScattering;
            stencilRefMV = (int)StencilUsage.ObjectMotionVector;
            stencilWriteMaskMV = (int)StencilUsage.ObjectMotionVector;

            // ForwardOnly materials with motion vectors are rendered after GBuffer, so we need to clear the deferred bit in the stencil
            if (forwardOnly)
                stencilWriteMaskMV |= (int)StencilUsage.RequiresDeferredLighting;

            if (useSplitLighting)
            {
                stencilRefGBuffer |= (int)StencilUsage.SubsurfaceScattering;
                stencilRef |= (int)StencilUsage.SubsurfaceScattering;
            }

            if (receivesSSR)
            {
                stencilRefDepth |= (int)StencilUsage.TraceReflectionRay;
                stencilRefGBuffer |= (int)StencilUsage.TraceReflectionRay;
                stencilRefMV |= (int)StencilUsage.TraceReflectionRay;
            }

            stencilWriteMaskDepth |= (int)StencilUsage.TraceReflectionRay;
            stencilWriteMaskGBuffer |= (int)StencilUsage.TraceReflectionRay;
            stencilWriteMaskMV |= (int)StencilUsage.TraceReflectionRay;

            if (hasRefraction)
            {
                stencilRefDepth |= (int)StencilUsage.Refractive;
                stencilWriteMaskDepth |= (int)StencilUsage.Refractive;
            }

            if (!receivesLighting)
            {
                stencilRefDepth |= (int)StencilUsage.IsUnlit;
                stencilWriteMaskDepth |= (int)StencilUsage.IsUnlit;
                stencilRefMV |= (int)StencilUsage.IsUnlit;
            }

            if (excludeFromTUAndAA)
            {
                stencilRefDepth |= (int)StencilUsage.ExcludeFromTUAndAA;
                stencilRef |= (int)StencilUsage.ExcludeFromTUAndAA;
                stencilWriteMask |= (int)StencilUsage.ExcludeFromTUAndAA;
                stencilWriteMaskDepth |= (int)StencilUsage.ExcludeFromTUAndAA;
            }

            stencilWriteMaskDepth |= (int)StencilUsage.IsUnlit;
            stencilWriteMaskGBuffer |= (int)StencilUsage.IsUnlit;
            stencilWriteMaskMV |= (int)StencilUsage.IsUnlit;
        }

        static public void SetupBaseLitMaterialPass(Material material)
        {
            material.SetupBaseUnlitPass();
        }

        static public void SetupDisplacement(Material material, int layerCount = 1)
        {
            DisplacementMode displacementMode = GetFilteredDisplacementMode(material);
            for (int i = 0; i < layerCount; i++)
            {
                var heightAmplitude = layerCount > 1 ? kHeightAmplitude + i : kHeightAmplitude;
                var heightCenter = layerCount > 1 ? kHeightCenter + i : kHeightCenter;
                if (material.HasProperty(heightAmplitude) && material.HasProperty(heightCenter))
                {
                    var heightPoMAmplitude = layerCount > 1 ? kHeightPoMAmplitude + i : kHeightPoMAmplitude;
                    var heightParametrization = layerCount > 1 ? kHeightParametrization + i : kHeightParametrization;
                    var heightTessAmplitude = layerCount > 1 ? kHeightTessAmplitude + i : kHeightTessAmplitude;
                    var heightTessCenter = layerCount > 1 ? kHeightTessCenter + i : kHeightTessCenter;
                    var heightOffset = layerCount > 1 ? kHeightOffset + i : kHeightOffset;
                    var heightMin = layerCount > 1 ? kHeightMin + i : kHeightMin;
                    var heightMax = layerCount > 1 ? kHeightMax + i : kHeightMax;

                    if (displacementMode == DisplacementMode.Pixel)
                    {
                        material.SetFloat(heightAmplitude, material.GetFloat(heightPoMAmplitude) * 0.01f); // Convert centimeters to meters.
                        material.SetFloat(heightCenter, 1.0f); // PoM is always inward so base (0 height) is mapped to 1 in the texture
                    }
                    else
                    {
                        var parametrization = (HeightmapParametrization)material.GetFloat(heightParametrization);
                        if (parametrization == HeightmapParametrization.MinMax)
                        {
                            float offset = material.GetFloat(heightOffset);
                            float minHeight = material.GetFloat(heightMin);
                            float amplitude = material.GetFloat(heightMax) - minHeight;

                            material.SetFloat(heightAmplitude, amplitude * 0.01f); // Convert centimeters to meters.
                            material.SetFloat(heightCenter, -(minHeight + offset) / Mathf.Max(1e-6f, amplitude));
                        }
                        else
                        {
                            float offset = material.GetFloat(heightOffset);
                            float center = material.GetFloat(heightTessCenter);
                            float amplitude = material.GetFloat(heightTessAmplitude);

                            material.SetFloat(heightAmplitude, amplitude * 0.01f); // Convert centimeters to meters.
                            material.SetFloat(heightCenter, -offset / Mathf.Max(1e-6f, amplitude) + center);
                        }
                    }
                }
            }
        }
    }
} // namespace UnityEditor
