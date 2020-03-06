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

#region Unlit
        public static class Unlit
        {
            public static PassDescriptor META = new PassDescriptor()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                useInPreview = false,

                // Port Mask
                pixelPorts = HDPortMasks.Pixel.UnlitDefault,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.Meta,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.Meta,
                pragmas = HDPragmas.Instanced,
                includes = HDIncludes.UnlitMeta,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Unlit"),
            };

            public static PassDescriptor ShadowCaster = new PassDescriptor()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.UnlitDefault,
                pixelPorts = HDPortMasks.Pixel.UnlitOnlyAlpha,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.ShadowCasterUnlit,
                pragmas = HDPragmas.Instanced,
                includes = HDIncludes.UnlitDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Unlit"),
            };

            public static PassDescriptor SceneSelection = new PassDescriptor()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.UnlitDefault,
                pixelPorts = HDPortMasks.Pixel.UnlitOnlyAlpha,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.SceneSelection,
                pragmas = HDPragmas.InstancedEditorSync,
                defines = HDDefines.SceneSelection,
                includes = HDIncludes.UnlitDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Unlit"),
            };

            public static PassDescriptor DepthForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.UnlitDefault,
                pixelPorts = HDPortMasks.Pixel.UnlitOnlyAlpha,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.DepthForwardOnly,
                pragmas = HDPragmas.Instanced,
                keywords = HDKeywords.WriteMsaaDepth,
                includes = HDIncludes.UnlitDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Unlit"),
            };

            public static PassDescriptor MotionVectors = new PassDescriptor()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.UnlitDefault,
                pixelPorts = HDPortMasks.Pixel.UnlitOnlyAlpha,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.PositionRWS,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.UnlitMotionVectors,
                pragmas = HDPragmas.Instanced,
                keywords = HDKeywords.WriteMsaaDepth,
                includes = HDIncludes.UnlitMotionVectors,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Unlit"),
            };

            public static PassDescriptor ForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD_UNLIT",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.UnlitDefault,
                pixelPorts = HDPortMasks.Pixel.UnlitDefault,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.UnlitForward,
                pragmas = HDPragmas.Instanced,
                keywords = HDKeywords.DebugDisplay,
                includes = HDIncludes.UnlitForwardOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Unlit"),
            };
        }
#endregion

#region PBR
        public static class PBR
        {
            public static PassDescriptor GBuffer = new PassDescriptor()
            {
                // Definition
                displayName = "GBuffer",
                referenceName = "SHADERPASS_GBUFFER",
                lightMode = "GBuffer",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.PBRDefault,
                pixelPorts = HDPortMasks.Pixel.PBRDefault,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.LitMinimal,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.PBRGBuffer,
                pragmas = HDPragmas.InstancedRenderingPlayer,
                keywords = HDKeywords.GBuffer,
                includes = HDIncludes.LitGBuffer,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("PBR"),
            };

            public static PassDescriptor META = new PassDescriptor()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                useInPreview = false,

                // Port Mask
                pixelPorts = HDPortMasks.Pixel.PBRDefault,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.Meta,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.Meta,
                pragmas = HDPragmas.InstancedRenderingPlayer,
                keywords = HDKeywords.LodFadeCrossfade,
                includes = HDIncludes.LitMeta,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("PBR"),
            };

            public static PassDescriptor ShadowCaster = new PassDescriptor()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.PBRDefault,
                pixelPorts = HDPortMasks.Pixel.PBROnlyAlpha,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.ShadowCasterPBR,
                pragmas = HDPragmas.InstancedRenderingPlayer,
                keywords = HDKeywords.LodFadeCrossfade,
                includes = HDIncludes.LitDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("PBR"),
            };

            public static PassDescriptor SceneSelection = new PassDescriptor()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.PBRDefault,
                pixelPorts = HDPortMasks.Pixel.PBROnlyAlpha,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.SceneSelection,
                pragmas = HDPragmas.InstancedRenderingPlayerEditorSync,
                defines = HDDefines.SceneSelection,
                keywords = HDKeywords.LodFadeCrossfade,
                includes = HDIncludes.LitDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("PBR"),
            };

            public static PassDescriptor DepthOnly = new PassDescriptor()
            {
                // Definition
                displayName = "DepthOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthOnly",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.PBRDefault,
                pixelPorts = HDPortMasks.Pixel.PBRDepthMotionVectors,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.LitFull,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.DepthOnly,
                pragmas = HDPragmas.InstancedRenderingPlayer,
                defines = HDDefines.ShaderGraphRaytracingHigh,
                keywords = HDKeywords.DepthMotionVectors,
                includes = HDIncludes.LitDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("PBR"),
            };

            public static PassDescriptor MotionVectors = new PassDescriptor()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.PBRDefault,
                pixelPorts = HDPortMasks.Pixel.PBRDepthMotionVectors,

                // Fields
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.PositionRWS,
                fieldDependencies = HDFieldDependencies.Default,

                // Conditional State
                renderStates = HDRenderStates.PBRMotionVectors,
                pragmas = HDPragmas.InstancedRenderingPlayer,
                defines = HDDefines.ShaderGraphRaytracingHigh,
                keywords = HDKeywords.DepthMotionVectors,
                includes = HDIncludes.LitMotionVectors,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("PBR"),
            };

            public static PassDescriptor Forward = new PassDescriptor()
            {
                // Definition
                displayName = "Forward",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "Forward",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.PBRDefault,
                pixelPorts = HDPortMasks.Pixel.PBRDefault,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.LitMinimal,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.PBRForward,
                pragmas = HDPragmas.InstancedRenderingPlayer,
                defines = HDDefines.Forward,
                keywords = HDKeywords.Forward,
                includes = HDIncludes.LitForward,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("PBR"),
            };
        }
#endregion

#region HDUnlit
        public static class HDUnlit
        {
            public static PassDescriptor META = new PassDescriptor()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                useInPreview = false,

                // Port Mask
                pixelPorts = HDPortMasks.Pixel.HDUnlitDefault,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = new FieldCollection(){ HDRequiredFields.Meta, HDFields.SubShader.Unlit },
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.Meta,
                pragmas = HDPragmas.Instanced,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.UnlitMeta,

                // Custom Template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/HDUnlitPass.template",
            };

            public static PassDescriptor ShadowCaster = new PassDescriptor()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDUnlitDefault,
                pixelPorts = HDPortMasks.Pixel.HDUnlitOnlyAlpha,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit },
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDShadowCaster,
                pragmas = HDPragmas.Instanced,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.UnlitDepthOnly,

                // Custom Template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/HDUnlitPass.template",
            };

            public static PassDescriptor SceneSelection = new PassDescriptor()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDUnlitDefault,
                pixelPorts = HDPortMasks.Pixel.HDUnlitOnlyAlpha,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit },
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDUnlitSceneSelection,
                pragmas = HDPragmas.InstancedEditorSync,
                defines = HDDefines.SceneSelection,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.UnlitDepthOnly,

                // Custom Template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/HDUnlitPass.template",
            };

            public static PassDescriptor DepthForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDUnlitDefault,
                pixelPorts = HDPortMasks.Pixel.HDUnlitOnlyAlpha,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit },
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDDepthForwardOnly,
                pragmas = HDPragmas.Instanced,
                keywords = HDKeywords.HDDepthMotionVectors,
                includes = HDIncludes.UnlitDepthOnly,

                // Custom Template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/HDUnlitPass.template",
            };

            public static PassDescriptor MotionVectors = new PassDescriptor()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDUnlitDefault,
                pixelPorts = HDPortMasks.Pixel.HDUnlitOnlyAlpha,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = new FieldCollection(){ HDRequiredFields.PositionRWS, HDFields.SubShader.Unlit },
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDUnlitMotionVectors,
                pragmas = HDPragmas.Instanced,
                keywords = HDKeywords.HDDepthMotionVectors,
                includes = HDIncludes.UnlitMotionVectors,

                // Custom Template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/HDUnlitPass.template",
            };

            public static PassDescriptor Distortion = new PassDescriptor()
            {
                // Definition
                displayName = "DistortionVectors",
                referenceName = "SHADERPASS_DISTORTION",
                lightMode = "DistortionVectors",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDUnlitDefault,
                pixelPorts = HDPortMasks.Pixel.HDUnlitDistortion,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit },
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDUnlitDistortion,
                pragmas = HDPragmas.Instanced,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.UnlitDistortion,

                // Custom Template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/HDUnlitPass.template",
            };

            public static PassDescriptor ForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD_UNLIT",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDUnlitDefault,
                pixelPorts = HDPortMasks.Pixel.HDUnlitForward,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit },
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDUnlitForward,
                pragmas = HDPragmas.Instanced,
                keywords = HDKeywords.HDUnlitForward,
                includes = HDIncludes.UnlitForwardOnly,

                // Custom Template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Unlit/ShaderGraph/HDUnlitPass.template",
            };
        }
#endregion

#region HDLit
        public static class HDLit
        {
            public static PassDescriptor GBuffer = new PassDescriptor()
            {
                // Definition
                displayName = "GBuffer",
                referenceName = "SHADERPASS_GBUFFER",
                lightMode = "GBuffer",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDLitDefault,
                pixelPorts = HDPortMasks.Pixel.HDLitDefault,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.LitMinimal,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDLitGBuffer,
                pragmas = HDPragmas.DotsInstanced,
                defines = HDDefines.ShaderGraphRaytracingHigh,
                keywords = HDKeywords.HDGBuffer,
                includes = HDIncludes.LitGBuffer,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static PassDescriptor META = new PassDescriptor()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                useInPreview = false,

                // Port Mask
                pixelPorts = HDPortMasks.Pixel.HDLitMeta,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.Meta,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.Meta,
                pragmas = HDPragmas.DotsInstanced,
                defines = HDDefines.ShaderGraphRaytracingHigh,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.LitMeta,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static PassDescriptor ShadowCaster = new PassDescriptor()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDLitDefault,
                pixelPorts = HDPortMasks.Pixel.HDLitShadowCaster,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDBlendShadowCaster,
                pragmas = HDPragmas.DotsInstanced,
                defines = HDDefines.ShaderGraphRaytracingHigh,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.LitDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static PassDescriptor SceneSelection = new PassDescriptor()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDLitDefault,
                pixelPorts = HDPortMasks.Pixel.HDLitSceneSelection,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDSceneSelection,
                pragmas = HDPragmas.DotsInstancedEditorSync,
                defines = HDDefines.SceneSelection,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.LitDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static PassDescriptor DepthOnly = new PassDescriptor()
            {
                // Definition
                displayName = "DepthOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthOnly",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDLitDefault,
                pixelPorts = HDPortMasks.Pixel.HDLitDepthMotionVectors,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.LitFull,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDDepthOnly,
                pragmas = HDPragmas.DotsInstanced,
                defines = HDDefines.ShaderGraphRaytracingHigh,
                keywords = HDKeywords.HDLitDepthMotionVectors,
                includes = HDIncludes.LitDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static PassDescriptor MotionVectors = new PassDescriptor()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDLitDefault,
                pixelPorts = HDPortMasks.Pixel.HDLitDepthMotionVectors,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.LitFull,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDMotionVectors,
                pragmas = HDPragmas.DotsInstanced,
                defines = HDDefines.ShaderGraphRaytracingHigh,
                keywords = HDKeywords.HDLitDepthMotionVectors,
                includes = HDIncludes.LitMotionVectors,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static PassDescriptor DistortionVectors = new PassDescriptor()
            {
                // Definition
                displayName = "DistortionVectors",
                referenceName = "SHADERPASS_DISTORTION",
                lightMode = "DistortionVectors",
                useInPreview = true,

                // Port mask
                vertexPorts = HDPortMasks.Vertex.HDLitDefault,
                pixelPorts = HDPortMasks.Pixel.HDLitDistortion,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDLitDistortion,
                pragmas = HDPragmas.DotsInstanced,
                defines = HDDefines.ShaderGraphRaytracingHigh,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.LitDistortion,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static PassDescriptor TransparentDepthPrepass = new PassDescriptor()
            {
                // Definition
                displayName = "TransparentDepthPrepass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPrepass",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDLitDefault,
                pixelPorts = HDPortMasks.Pixel.HDLitTransparentDepthPrepass,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDTransparentDepthPrePostPass,
                pragmas = HDPragmas.DotsInstanced,
                defines = HDDefines.TransparentDepthPrepass,
                keywords = HDKeywords.TransparentDepthPrepass,
                includes = HDIncludes.LitDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static PassDescriptor TransparentBackface = new PassDescriptor()
            {
                // Definition
                displayName = "TransparentBackface",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "TransparentBackface",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDLitDefault,
                pixelPorts = HDPortMasks.Pixel.HDLitTransparentBackface,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDTransparentBackface,
                pragmas = HDPragmas.DotsInstanced,
                defines = HDDefines.Forward,
                keywords = HDKeywords.HDForward,
                includes = HDIncludes.LitForward,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static PassDescriptor Forward = new PassDescriptor()
            {
                // Definition
                displayName = "Forward",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "Forward",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDLitDefault,
                pixelPorts = HDPortMasks.Pixel.HDLitDefault,

                // Collections
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.LitMinimal,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDForwardColorMask,
                pragmas = HDPragmas.DotsInstanced,
                defines = HDDefines.Forward,
                keywords = HDKeywords.HDForward,
                includes = HDIncludes.LitForward,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static PassDescriptor TransparentDepthPostpass = new PassDescriptor()
            {
                // Definition
                displayName = "TransparentDepthPostpass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPostpass",
                useInPreview = true,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDLitDefault,
                pixelPorts = HDPortMasks.Pixel.HDLitTransparentDepthPostpass,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.HDTransparentDepthPrePostPass,
                pragmas = HDPragmas.DotsInstanced,
                defines = HDDefines.TransparentDepthPostpass,
                keywords = HDKeywords.TransparentDepthPostpass,
                includes = HDIncludes.LitDepthOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };
        }
#endregion

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
                pragmas = HDPragmas.InstancedRenderingPlayer,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
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
                pragmas = HDPragmas.InstancedRenderingPlayerEditorSync,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
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
                pragmas = HDPragmas.InstancedRenderingPlayerEditorSync,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
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
                pragmas = HDPragmas.InstancedRenderingPlayerEditorSync,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
                defines = HDDefines.TransparentDepthPrepass,
                keywords = HDKeywords.TransparentDepthPrepass,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
                defines = HDDefines.TransparentDepthPostpass,
                keywords = HDKeywords.TransparentDepthPostpass,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
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
                pragmas = HDPragmas.InstancedRenderingPlayerEditorSync,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
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
                pragmas = HDPragmas.InstancedRenderingPlayer,
                defines = HDDefines.Forward,
                keywords = HDKeywords.HDForward,
                includes = HDIncludes.StackLitForwardOnly,

                // Custom Template
                passTemplatePath = GetPassTemplatePath("StackLit"),
            };
        }
#endregion

#region Decal
        public static class Decal
        {
            // CAUTION: c# code relies on the order in which the passes are declared, any change will need to be reflected in Decalsystem.cs - s_MaterialDecalNames and s_MaterialDecalSGNames array
            // and DecalSet.InitializeMaterialValues()
            public static PassDescriptor Projector3RT = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferProjector3RT],
                referenceName = "SHADERPASS_DBUFFER_PROJECTOR",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferProjector3RT],
                useInPreview = false,

                // Port mask
                pixelPorts = HDPortMasks.Pixel.DecalDefault,

                //Fields
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                renderStates = HDRenderStates.DecalProjector3RT,
                pragmas = HDPragmas.Instanced,
                defines = HDDefines.Decals3RT,
                includes = HDIncludes.Decal,
            };

            public static PassDescriptor Projector4RT = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferProjector4RT],
                referenceName = "SHADERPASS_DBUFFER_PROJECTOR",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferProjector4RT],
                useInPreview = false,

                // Port mask
                pixelPorts = HDPortMasks.Pixel.DecalDefault,

                //Fields
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,

                // Conditional State
                renderStates = HDRenderStates.DecalProjector4RT,
                pragmas = HDPragmas.Instanced,
                defines = HDDefines.Decals4RT,
                includes = HDIncludes.Decal,
            };

            public static PassDescriptor ProjectorEmissive = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_ProjectorEmissive],
                referenceName = "SHADERPASS_FORWARD_EMISSIVE_PROJECTOR",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_ProjectorEmissive],
                useInPreview = false,

                // Port mask
                pixelPorts = HDPortMasks.Pixel.DecalEmissive,

                //Fields
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,

                // Conditional State
                renderStates = HDRenderStates.DecalProjectorEmissive,
                pragmas = HDPragmas.Instanced,
                includes = HDIncludes.Decal,
            };

            public static PassDescriptor Mesh3RT = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh3RT],
                referenceName = "SHADERPASS_DBUFFER_MESH",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh3RT],
                useInPreview = false,

                // Port mask
                pixelPorts = HDPortMasks.Pixel.DecalDefault,

                //Fields
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.DecalMesh,
                fieldDependencies = HDFieldDependencies.Default,

                // Conditional State
                renderStates = HDRenderStates.DecalMesh3RT,
                pragmas = HDPragmas.Instanced,
                defines = HDDefines.Decals3RT,
                includes = HDIncludes.Decal,
            };

            public static PassDescriptor Mesh4RT = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh4RT],
                referenceName = "SHADERPASS_DBUFFER_MESH",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh4RT],
                useInPreview = false,

                // Port mask
                pixelPorts = HDPortMasks.Pixel.DecalDefault,

                //Fields
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.DecalMesh,
                fieldDependencies = HDFieldDependencies.Default,

                // Conditional State
                renderStates = HDRenderStates.DecalMesh4RT,
                pragmas = HDPragmas.Instanced,
                defines = HDDefines.Decals4RT,
                includes = HDIncludes.Decal,
            };

            public static PassDescriptor MeshEmissive = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_MeshEmissive],
                referenceName = "SHADERPASS_FORWARD_EMISSIVE_MESH",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_MeshEmissive],
                useInPreview = false,

                // Port mask
                pixelPorts = HDPortMasks.Pixel.DecalMeshEmissive,

                //Fields
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.DecalMesh,
                fieldDependencies = HDFieldDependencies.Default,

                // Conditional State
                renderStates = HDRenderStates.DecalMeshEmissive,
                pragmas = HDPragmas.Instanced,
                includes = HDIncludes.Decal,
            };

            public static PassDescriptor Preview = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD_PREVIEW",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Port mask
                pixelPorts = HDPortMasks.Pixel.DecalMeshEmissive,

                //Fields
                structs = HDStructCollections.Default,
                requiredFields = HDRequiredFields.DecalMesh,
                fieldDependencies = HDFieldDependencies.Default,

                // Render state overrides
                renderStates = HDRenderStates.DecalPreview,
                pragmas = HDPragmas.Instanced,
                includes = HDIncludes.Decal,
            };
        }
#endregion

#region HDLitRaytracing
        public static class HDLitRaytracing
        {
            public static PassDescriptor Indirect = new PassDescriptor()
            {
                // Definition
                displayName = "IndirectDXR",
                referenceName = "SHADERPASS_RAYTRACING_INDIRECT",
                lightMode = "IndirectDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDLitDefault,
                pixelPorts = HDPortMasks.Pixel.HDLitDefault,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                pragmas = HDPragmas.RaytracingInstanced,
                defines = HDDefines.HDLitRaytracingForwardIndirect,
                keywords = HDKeywords.RaytracingIndirect,
                includes = HDIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Lit, HDFields.ShaderPass.RaytracingIndirect },

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static PassDescriptor Visibility = new PassDescriptor()
            {
                // Definition
                displayName = "VisibilityDXR",
                referenceName = "SHADERPASS_RAYTRACING_VISIBILITY",
                lightMode = "VisibilityDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDLitDefault,
                pixelPorts = HDPortMasks.Pixel.HDLitDefault,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                pragmas = HDPragmas.RaytracingInstanced,
                defines = HDDefines.HDLitRaytracingVisibility,
                includes = HDIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Lit, HDFields.ShaderPass.RaytracingVisibility },

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static PassDescriptor Forward = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardDXR",
                referenceName = "SHADERPASS_RAYTRACING_FORWARD",
                lightMode = "ForwardDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDLitDefault,
                pixelPorts = HDPortMasks.Pixel.HDLitDefault,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                pragmas = HDPragmas.RaytracingInstanced,
                defines = HDDefines.HDLitRaytracingForwardIndirect,
                keywords = HDKeywords.RaytracingGBufferForward,
                includes = HDIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Lit, HDFields.ShaderPass.RaytracingForward },

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };

            public static PassDescriptor GBuffer = new PassDescriptor()
            {
                // Definition
                displayName = "GBufferDXR",
                referenceName = "SHADERPASS_RAYTRACING_GBUFFER",
                lightMode = "GBufferDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDLitDefault,
                pixelPorts = HDPortMasks.Pixel.HDLitDefault,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                pragmas = HDPragmas.RaytracingInstanced,
                defines = HDDefines.HDLitRaytracingGBuffer,
                keywords = HDKeywords.RaytracingGBufferForward,
                includes = HDIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Lit, HDFields.ShaderPass.RayTracingGBuffer },

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };
            public static PassDescriptor PathTracing = new PassDescriptor()
            {
                //Definition
                displayName = "PathTracingDXR",
                referenceName = "SHADERPASS_PATH_TRACING",
                lightMode = "PathTracingDXR",
                useInPreview = false,

                //Port mask
                vertexPorts = HDPortMasks.Vertex.HDLitDefault,
                pixelPorts = HDPortMasks.Pixel.HDLitDefault,

                //Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                pragmas = HDPragmas.RaytracingInstanced,
                defines = HDDefines.HDLitRaytracingPathTracing,
                includes = HDIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Lit, HDFields.ShaderPass.RaytracingPathTracing },

                //Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };
            public static PassDescriptor SubSurface = new PassDescriptor()
            {
                //Definition
                displayName = "SubSurfaceDXR",
                referenceName = "SHADERPASS_RAYTRACING_SUB_SURFACE",
                lightMode = "SubSurfaceDXR",
                useInPreview = false,

                //Port mask
                vertexPorts = HDPortMasks.Vertex.HDLitDefault,
                pixelPorts = HDPortMasks.Pixel.HDLitDefault,

                //Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                pragmas = HDPragmas.RaytracingInstanced,
                defines = HDDefines.HDLitRaytracingGBuffer,
                keywords = HDKeywords.RaytracingGBufferForward,
                includes = HDIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Lit, HDFields.ShaderPass.RaytracingSubSurface },

                //Custom Template
                passTemplatePath = GetPassTemplatePath("Lit"),
            };
        }
#endregion

#region HDUnlitRaytracing
        public static class HDUnlitRaytracing
        {
            public static PassDescriptor Indirect = new PassDescriptor()
            {
                // Definition
                displayName = "IndirectDXR",
                referenceName = "SHADERPASS_RAYTRACING_INDIRECT",
                lightMode = "IndirectDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDUnlitDefault,
                pixelPorts = HDPortMasks.Pixel.HDUnlitDefault,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                pragmas = HDPragmas.RaytracingBasic,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit, HDFields.ShaderPass.RaytracingIndirect },

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Unlit"),
            };

            public static PassDescriptor Visibility = new PassDescriptor()
            {
                // Definition
                displayName = "VisibilityDXR",
                referenceName = "SHADERPASS_RAYTRACING_VISIBILITY",
                lightMode = "VisibilityDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDUnlitDefault,
                pixelPorts = HDPortMasks.Pixel.HDUnlitDefault,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                pragmas = HDPragmas.RaytracingBasic,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit, HDFields.ShaderPass.RaytracingVisibility },

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Unlit"),
            };

            public static PassDescriptor Forward = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardDXR",
                referenceName = "SHADERPASS_RAYTRACING_FORWARD",
                lightMode = "ForwardDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDUnlitDefault,
                pixelPorts = HDPortMasks.Pixel.HDUnlitDefault,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                pragmas = HDPragmas.RaytracingBasic,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit, HDFields.ShaderPass.RaytracingForward },

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Unlit"),
            };

            public static PassDescriptor GBuffer = new PassDescriptor()
            {
                // Definition
                displayName = "GBufferDXR",
                referenceName = "SHADERPASS_RAYTRACING_GBUFFER",
                lightMode = "GBufferDXR",
                useInPreview = false,

                // Port Mask
                vertexPorts = HDPortMasks.Vertex.HDUnlitDefault,
                pixelPorts = HDPortMasks.Pixel.HDUnlitDefault,

                // Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                pragmas = HDPragmas.RaytracingBasic,
                keywords = HDKeywords.HDBase,
                includes = HDIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.Unlit, HDFields.ShaderPass.RayTracingGBuffer },

                // Custom Template
                passTemplatePath = GetPassTemplatePath("Unlit"),
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
                pragmas = HDPragmas.RaytracingInstanced,
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
                pragmas = HDPragmas.RaytracingInstanced,
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
                pragmas = HDPragmas.RaytracingInstanced,
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
                pragmas = HDPragmas.RaytracingInstanced,
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
                vertexPorts = HDPortMasks.Vertex.HDLitDefault,
                pixelPorts = HDPortMasks.Pixel.HDLitDefault,

                //Collections
                structs = HDStructCollections.Default,
                fieldDependencies = HDFieldDependencies.Default,
                pragmas = HDPragmas.RaytracingInstanced,
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
