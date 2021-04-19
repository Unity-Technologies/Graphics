using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    abstract class BaseLitGUI
    {
        // Properties for Base Lit material keyword setup
        protected const string kDoubleSidedNormalMode = "_DoubleSidedNormalMode";

        protected const string kDisplacementLockObjectScale = "_DisplacementLockObjectScale";
        protected const string kDisplacementLockTilingScale = "_DisplacementLockTilingScale";

        // Wind
        protected const string kWindEnabled = "_EnableWind";

        // tessellation params
        protected const string kTessellationMode = "_TessellationMode";

        // Decal
        protected const string kEnableGeometricSpecularAA = "_EnableGeometricSpecularAA";

        // SSR
        protected MaterialProperty receivesSSR = null;

        protected virtual void UpdateDisplacement() {}

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

            if (material.HasProperty(kDisplacementMode))
            {
                bool enableDisplacement = (DisplacementMode)material.GetFloat(kDisplacementMode) != DisplacementMode.None;
                bool enableVertexDisplacement = (DisplacementMode)material.GetFloat(kDisplacementMode) == DisplacementMode.Vertex;
                bool enablePixelDisplacement = (DisplacementMode)material.GetFloat(kDisplacementMode) == DisplacementMode.Pixel;
                bool enableTessellationDisplacement = ((DisplacementMode)material.GetFloat(kDisplacementMode) == DisplacementMode.Tessellation) && material.HasProperty(kTessellationMode);

                CoreUtils.SetKeyword(material, "_VERTEX_DISPLACEMENT", enableVertexDisplacement);
                CoreUtils.SetKeyword(material, "_PIXEL_DISPLACEMENT", enablePixelDisplacement);
                // Only set if tessellation exist
                CoreUtils.SetKeyword(material, "_TESSELLATION_DISPLACEMENT", enableTessellationDisplacement);

                bool displacementLockObjectScale = material.GetFloat(kDisplacementLockObjectScale) > 0.0;
                bool displacementLockTilingScale = material.GetFloat(kDisplacementLockTilingScale) > 0.0;
                // Tessellation reuse vertex flag.
                CoreUtils.SetKeyword(material, "_VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE", displacementLockObjectScale && (enableVertexDisplacement || enableTessellationDisplacement));
                CoreUtils.SetKeyword(material, "_PIXEL_DISPLACEMENT_LOCK_OBJECT_SCALE", displacementLockObjectScale && enablePixelDisplacement);
                CoreUtils.SetKeyword(material, "_DISPLACEMENT_LOCK_TILING_SCALE", displacementLockTilingScale && enableDisplacement);

                // Depth offset is only enabled if per pixel displacement is
                bool depthOffsetEnable = (material.GetFloat(kDepthOffsetEnable) > 0.0f) && enablePixelDisplacement;
                CoreUtils.SetKeyword(material, "_DEPTHOFFSET_ON", depthOffsetEnable);
            }

            CoreUtils.SetKeyword(material, "_VERTEX_WIND", false);

            if (material.HasProperty(kTessellationMode))
            {
                TessellationMode tessMode = (TessellationMode)material.GetFloat(kTessellationMode);
                CoreUtils.SetKeyword(material, "_TESSELLATION_PHONG", tessMode == TessellationMode.Phong);
            }

            material.SetupMainTexForAlphaTestGI("_BaseColorMap", "_BaseColor");

            // Use negation so we don't create keyword by default
            CoreUtils.SetKeyword(material, "_DISABLE_DECALS", material.HasProperty(kSupportDecals) && material.GetFloat(kSupportDecals) == 0.0);
            CoreUtils.SetKeyword(material, "_DISABLE_SSR", material.HasProperty(kReceivesSSR) && material.GetFloat(kReceivesSSR) == 0.0);
            CoreUtils.SetKeyword(material, "_DISABLE_SSR_TRANSPARENT", material.HasProperty(kReceivesSSRTransparent) && material.GetFloat(kReceivesSSRTransparent) == 0.0);
            CoreUtils.SetKeyword(material, "_ENABLE_GEOMETRIC_SPECULAR_AA", material.HasProperty(kEnableGeometricSpecularAA) && material.GetFloat(kEnableGeometricSpecularAA) == 1.0);

            if (material.HasProperty(kRefractionModel))
            {
                var refractionModelValue = (ScreenSpaceRefraction.RefractionModel)material.GetFloat(kRefractionModel);
                // We can't have refraction in pre-refraction queue and the material needs to be transparent
                var canHaveRefraction = material.GetSurfaceType() == SurfaceType.Transparent && !HDRenderQueue.k_RenderQueue_PreRefraction.Contains(material.renderQueue);
                CoreUtils.SetKeyword(material, "_REFRACTION_PLANE", (refractionModelValue == ScreenSpaceRefraction.RefractionModel.Box) && canHaveRefraction);
                CoreUtils.SetKeyword(material, "_REFRACTION_SPHERE", (refractionModelValue == ScreenSpaceRefraction.RefractionModel.Sphere) && canHaveRefraction);
                CoreUtils.SetKeyword(material, "_REFRACTION_THIN", (refractionModelValue == ScreenSpaceRefraction.RefractionModel.Thin) && canHaveRefraction);
            }
        }

        static public void SetupStencil(Material material, bool receivesSSR, bool useSplitLighting)
        {
            ComputeStencilProperties(receivesSSR, useSplitLighting, out int stencilRef, out int stencilWriteMask,
                out int stencilRefDepth, out int stencilWriteMaskDepth, out int stencilRefGBuffer, out int stencilWriteMaskGBuffer,
                out int stencilRefMV, out int stencilWriteMaskMV
            );

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

        static public void ComputeStencilProperties(bool receivesSSR, bool useSplitLighting, out int stencilRef, out int stencilWriteMask,
            out int stencilRefDepth, out int stencilWriteMaskDepth, out int stencilRefGBuffer, out int stencilWriteMaskGBuffer,
            out int stencilRefMV, out int stencilWriteMaskMV)
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
        }

        static public void SetupBaseLitMaterialPass(Material material)
        {
            material.SetupBaseUnlitPass();
        }
    }
} // namespace UnityEditor
