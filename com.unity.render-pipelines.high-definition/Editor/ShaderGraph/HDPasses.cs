using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class HDPasses
    {
        static string GetPassTemplatePath(string materialName)
        {
            return $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/{materialName}/ShaderGraph/{materialName}Pass.template";
        }

        static class HDStructCollections
        {
            public static StructCollection Default = new StructCollection
            {
                { HDStructs.AttributesMesh },
                { HDStructs.VaryingsMeshToPS },
                { Structs.SurfaceDescriptionInputs },
                { Structs.VertexDescriptionInputs },
            };
        }

#region StackLit
        public static class StackLit
        {
            public static PassDescriptor META = new PassDescriptor()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                useInPreview = false,

                // Port Mask
                pixelPorts = HDPortMasks.Pixel.StackLitMETA,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.Meta,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.Meta,
                pragmas = HDPragmas.DotsInstancedInV2OnlyRenderingLayer,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.StackLitMeta,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("StackLit"),
            };

            public static PassDescriptor ShadowCaster = new PassDescriptor()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.StackLitPosition,
                pixelPorts = HDPortMasks.Pixel.StackLitAlphaDepth,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.StackLitShadowCaster,
                pragmas = HDPragmas.DotsInstancedInV2OnlyRenderingLayer,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.StackLitDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("StackLit"),
            };

            public static PassDescriptor SceneSelection = new PassDescriptor()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.StackLitDefault,
                pixelPorts = HDPortMasks.Pixel.StackLitAlphaDepth,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDSceneSelection,
                pragmas = HDPragmas.DotsInstancedInV2OnlyRenderingLayerEditorSync,
                defines = HDDefines.SceneSelection,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.StackLitDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("StackLit"),
            };

            public static PassDescriptor DepthForwardOnly = new PassDescriptor()
            {
                // // Code path for WRITE_NORMAL_BUFFER
                // See StackLit.hlsl:ConvertSurfaceDataToNormalData()
                // which ShaderPassDepthOnly uses: we need to add proper interpolators dependencies depending on WRITE_NORMAL_BUFFER.
                // In our case WRITE_NORMAL_BUFFER is always enabled here.
                // Also, we need to add PixelShaderSlots dependencies for everything potentially used there.
                // See AddPixelShaderSlotsForWriteNormalBufferPasses()

                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.StackLitDefault,
                pixelPorts = HDPortMasks.Pixel.StackLitDepthMotionVectors,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.LitFull,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDDepthOnly,
                pragmas = HDPragmas.DotsInstancedInV2OnlyRenderingLayer,
                defines = HDDefines.DepthMotionVectors,
                keywords = HDKeywords.HDDepthMotionVectorsNoNormal,
                includes = HDIncludes.StackLitDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("StackLit"),
            };

            public static PassDescriptor MotionVectors = new PassDescriptor()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.StackLitDefault,
                pixelPorts = HDPortMasks.Pixel.StackLitDepthMotionVectors,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.LitFull,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDMotionVectors,
                pragmas = HDPragmas.DotsInstancedInV2OnlyRenderingLayer,
                defines = HDDefines.DepthMotionVectors,
                keywords = HDKeywords.HDDepthMotionVectorsNoNormal,
                includes = HDIncludes.StackLitMotionVectors,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("StackLit"),
            };

            public static PassDescriptor Distortion = new PassDescriptor()
            {
                // Definition
                displayName = "DistortionVectors",
                referenceName = "SHADERPASS_DISTORTION",
                lightMode = "DistortionVectors",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.StackLitDefault,
                pixelPorts = HDPortMasks.Pixel.StackLitDistortion,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.StackLitDistortion,
                pragmas = HDPragmas.DotsInstancedInV2OnlyRenderingLayer,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.StackLitDistortion,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("StackLit"),
            };

            public static PassDescriptor ForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.StackLitDefault,
                pixelPorts = HDPortMasks.Pixel.StackLitForward,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.LitFull,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDForward,
                pragmas = HDPragmas.DotsInstancedInV2OnlyRenderingLayer,
                defines = HDDefines.Forward,
                keywords = HDKeywords.HDForward,
                includes = HDIncludes.StackLitForwardOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("StackLit"),
            };
        }
#endregion
    }
}
