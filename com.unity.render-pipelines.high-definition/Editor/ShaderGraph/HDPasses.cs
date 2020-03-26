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

#region Eye
        public static class Eye
        {
            public static PassDescriptor META = new PassDescriptor()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                useInPreview = false,

                // Port Mask
                pixelPorts = HDPortMasks.Pixel.EyeMETA,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.Meta,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.Meta,
                pragmas = HDPragmas.InstancedRenderingLayer,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.EyeMeta,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Eye"),
            };

            public static PassDescriptor ShadowCaster = new PassDescriptor()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.EyeDefault,
                pixelPorts = HDPortMasks.Pixel.EyeAlphaDepth,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDBlendShadowCaster,
                pragmas = HDPragmas.InstancedRenderingLayer,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.EyeDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Eye"),
            };

            public static PassDescriptor SceneSelection = new PassDescriptor()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.EyeDefault,
                pixelPorts = HDPortMasks.Pixel.EyeAlphaDepth,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDSceneSelection,
                pragmas = HDPragmas.InstancedRenderingLayerEditorSync,
                defines = HDDefines.SceneSelection,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.EyeDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Eye"),
            };

            public static PassDescriptor DepthForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.EyeDefault,
                pixelPorts = HDPortMasks.Pixel.EyeDepthMotionVectors,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.LitFull,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDDepthOnly,
                pragmas = HDPragmas.InstancedRenderingLayer,
                defines = HDDefines.DepthMotionVectors,
                keywords = HDKeywords.HDDepthMotionVectorsNoNormal,
                includes = HDIncludes.EyeDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Eye"),
            };

            public static PassDescriptor MotionVectors = new PassDescriptor()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.EyeDefault,
                pixelPorts = HDPortMasks.Pixel.EyeDepthMotionVectors,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.LitFull,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDMotionVectors,
                pragmas = HDPragmas.InstancedRenderingLayer,
                defines = HDDefines.DepthMotionVectors,
                keywords = HDKeywords.HDDepthMotionVectorsNoNormal,
                includes = HDIncludes.EyeMotionVectors,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Eye"),
            };

            public static PassDescriptor ForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.EyeDefault,
                pixelPorts = HDPortMasks.Pixel.EyeForward,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.LitFull,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDForward,
                pragmas = HDPragmas.InstancedRenderingLayer,
                defines = HDDefines.Forward,
                keywords = HDKeywords.HDForward,
                includes = HDIncludes.EyeForwardOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Eye"),
            };
        }
#endregion

#region Fabric
        public static class Fabric
        {
            public static PassDescriptor META = new PassDescriptor()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                useInPreview = false,

                // Port Mask
                pixelPorts = HDPortMasks.Pixel.FabricMETA,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.Meta,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.Meta,
                pragmas = HDPragmas.InstancedRenderingLayer,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.FabricMeta,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Fabric"),
            };

            public static PassDescriptor ShadowCaster = new PassDescriptor()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.FabricDefault,
                pixelPorts = HDPortMasks.Pixel.FabricAlphaDepth,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDBlendShadowCaster,
                pragmas = HDPragmas.InstancedRenderingLayer,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.FabricDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Fabric"),
            };

            public static PassDescriptor SceneSelection = new PassDescriptor()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.FabricDefault,
                pixelPorts = HDPortMasks.Pixel.FabricAlphaDepth,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDShadowCaster,
                pragmas = HDPragmas.InstancedRenderingLayerEditorSync,
                defines = HDDefines.SceneSelection,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.FabricDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Fabric"),
            };

            public static PassDescriptor DepthForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.FabricDefault,
                pixelPorts = HDPortMasks.Pixel.FabricDepthMotionVectors,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.LitFull,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDDepthOnly,
                pragmas = HDPragmas.InstancedRenderingLayer,
                defines = HDDefines.DepthMotionVectors,
                keywords = HDKeywords.HDDepthMotionVectorsNoNormal,
                includes = HDIncludes.FabricDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Fabric"),
            };

            public static PassDescriptor MotionVectors = new PassDescriptor()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.FabricDefault,
                pixelPorts = HDPortMasks.Pixel.FabricDepthMotionVectors,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.LitFull,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDMotionVectors,
                pragmas = HDPragmas.InstancedRenderingLayer,
                defines = HDDefines.DepthMotionVectors,
                keywords = HDKeywords.HDDepthMotionVectorsNoNormal,
                includes = HDIncludes.FabricMotionVectors,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Fabric"),
            };

            public static PassDescriptor FabricForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.FabricDefault,
                pixelPorts = HDPortMasks.Pixel.FabricForward,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.LitFull,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDForward,
                pragmas = HDPragmas.InstancedRenderingLayer,
                defines = HDDefines.Forward,
                keywords = HDKeywords.HDForward,
                includes = HDIncludes.FabricForwardOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Fabric"),
            };
        }
#endregion

#region Hair
        public static class Hair
        {
            public static PassDescriptor META = new PassDescriptor()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                useInPreview = false,

                // Port Mask
                pixelPorts = HDPortMasks.Pixel.HairMETA,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.Meta,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.Meta,
                pragmas = HDPragmas.InstancedRenderingLayer,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.HairMeta,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Hair"),
            };

            public static PassDescriptor ShadowCaster = new PassDescriptor()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HairDefault,
                pixelPorts = HDPortMasks.Pixel.HairShadowCaster,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDBlendShadowCaster,
                pragmas = HDPragmas.InstancedRenderingLayer,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.HairDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Hair"),
            };

            public static PassDescriptor SceneSelection = new PassDescriptor()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HairDefault,
                pixelPorts = HDPortMasks.Pixel.HairAlphaDepth,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDSceneSelection,
                pragmas = HDPragmas.InstancedRenderingLayerEditorSync,
                defines = HDDefines.SceneSelection,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.HairDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Hair"),
            };

            public static PassDescriptor DepthForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HairDefault,
                pixelPorts = HDPortMasks.Pixel.HairDepthMotionVectors,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.LitFull,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HairDepthOnly,
                pragmas = HDPragmas.InstancedRenderingLayer,
                defines = HDDefines.DepthMotionVectors,
                keywords = HDKeywords.HDDepthMotionVectorsNoNormal,
                includes = HDIncludes.HairDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Hair"),
            };

            public static PassDescriptor MotionVectors = new PassDescriptor()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HairDefault,
                pixelPorts = HDPortMasks.Pixel.HairDepthMotionVectors,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.LitFull,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HairMotionVectors,
                pragmas = HDPragmas.InstancedRenderingLayer,
                defines = HDDefines.DepthMotionVectors,
                keywords = HDKeywords.HDDepthMotionVectorsNoNormal,
                includes = HDIncludes.HairMotionVectors,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Hair"),
            };

            public static PassDescriptor TransparentDepthPrepass = new PassDescriptor()
            {
                // Definition
                displayName = "TransparentDepthPrepass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPrepass",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HairDefault,
                pixelPorts = HDPortMasks.Pixel.HairTransparentDepthPrepass,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDTransparentDepthPrePostPass,
                pragmas = HDPragmas.InstancedRenderingLayer,
                defines = HDDefines.TransparentDepthPrepass,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.HairDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Hair"),
            };

            public static PassDescriptor TransparentBackface = new PassDescriptor()
            {
                // Definition
                displayName = "TransparentBackface",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "TransparentBackface",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HairDefault,
                pixelPorts = HDPortMasks.Pixel.HairTransparentBackface,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.LitMinimal,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDTransparentBackface,
                pragmas = HDPragmas.InstancedRenderingLayer,
                defines = HDDefines.Forward,
                keywords = HDKeywords.HDForward,
                includes = HDIncludes.HairForwardOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Hair"),
            };

            public static PassDescriptor ForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HairDefault,
                pixelPorts = HDPortMasks.Pixel.HairForward,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.LitFull,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDForwardColorMask,
                pragmas = HDPragmas.InstancedRenderingLayer,
                defines = HDDefines.Forward,
                keywords = HDKeywords.HDForward,
                includes = HDIncludes.HairForwardOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Hair"),
            };

            public static PassDescriptor TransparentDepthPostpass = new PassDescriptor()
            {
                // Definition
                displayName = "TransparentDepthPostpass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPostpass",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HairDefault,
                pixelPorts = HDPortMasks.Pixel.HairTransparentDepthPostpass,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDTransparentDepthPrePostPass,
                pragmas = HDPragmas.InstancedRenderingLayer,
                defines = HDDefines.ShaderGraphRaytracingHigh,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.HairDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Hair"),
            };
        }
#endregion

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

#region FabricRaytracing
        public static class FabricRaytracing
        {
            public static PassDescriptor Indirect = new PassDescriptor()
            {
                // Definition
                displayName = "IndirectDXR",
                referenceName = "SHADERPASS_RAYTRACING_INDIRECT",
                lightMode = "IndirectDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.FabricDefault,
                pixelPorts = HDPortMasks.Pixel.FabricForward,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                pragmas = HDPragmas.RaytracingBasic,
                defines = HDDefines.FabricRaytracingForwardIndirect,
                keywords = HDKeywords.RaytracingIndirect,
                includes = HDIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Fabric, HDFields.ShaderPass.RaytracingIndirect },

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Fabric"),
            };

            public static PassDescriptor Visibility = new PassDescriptor()
            {
                // Definition
                displayName = "VisibilityDXR",
                referenceName = "SHADERPASS_RAYTRACING_VISIBILITY",
                lightMode = "VisibilityDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.FabricDefault,
                pixelPorts = HDPortMasks.Pixel.FabricForward,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                pragmas = HDPragmas.RaytracingBasic,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Fabric, HDFields.ShaderPass.RaytracingVisibility },

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Fabric"),
            };

            public static PassDescriptor Forward = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardDXR",
                referenceName = "SHADERPASS_RAYTRACING_FORWARD",
                lightMode = "ForwardDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.FabricDefault,
                pixelPorts = HDPortMasks.Pixel.FabricForward,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                pragmas = HDPragmas.RaytracingBasic,
                defines = HDDefines.FabricRaytracingForwardIndirect,
                keywords = HDKeywords.RaytracingGBufferForward,
                includes = HDIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Fabric, HDFields.ShaderPass.RaytracingForward },

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Fabric"),
            };

            public static PassDescriptor GBuffer = new PassDescriptor()
            {
                // Definition
                displayName = "GBufferDXR",
                referenceName = "SHADERPASS_RAYTRACING_GBUFFER",
                lightMode = "GBufferDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.FabricDefault,
                pixelPorts = HDPortMasks.Pixel.FabricForward,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                pragmas = HDPragmas.RaytracingBasic,
                defines = HDDefines.FabricRaytracingGBuffer,
                keywords = HDKeywords.RaytracingGBufferForward,
                includes = HDIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Fabric, HDFields.ShaderPass.RayTracingGBuffer },

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Fabric"),
            };
            public static PassDescriptor SubSurface = new PassDescriptor()
            {
                //Definition
                displayName = "SubSurfaceDXR",
                referenceName = "SHADERPASS_RAYTRACING_SUB_SURFACE",
                lightMode = "SubSurfaceDXR",
                useInPreview = false,

                //Port mask
                vertexPorts = HDPortMasks.Vertex.FabricDefault,
                pixelPorts = HDPortMasks.Pixel.FabricForward,

                //Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                pragmas = HDPragmas.RaytracingBasic,
                defines = HDDefines.FabricRaytracingGBuffer,
                keywords = HDKeywords.RaytracingGBufferForward,
                includes = HDIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Fabric, HDFields.ShaderPass.RaytracingSubSurface },

                //Custom Template
                passTemplatePath = GetPassTemplatePath("Fabric"),
            };
        }
#endregion
    }
}
