using UnityEditor.ShaderGraph;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static partial class HDShaderKernels
    {
        static public KernelDescriptor LineRenderingVertexSetup(bool supportLighting)
        {
            return new KernelDescriptor
            {
                name = "VertexSetup",
                templatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/ShaderGraph/Templates/Kernels/VertexSetup.template",
                passDescriptorReference = HDShaderPasses.GenerateForwardOnlyPass(supportLighting, false, false)
            };
        }
    }

    static partial class HDShaderPasses
    {
        public const string kPassForward = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/LineRendering/ShaderPass/ShaderPassOffscreenForward.hlsl";
        public const string kPassForwardUnlit = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/LineRendering/ShaderPass/ShaderPassOffscreenUnlit.hlsl";

        public static DefineCollection OffscreenShadingLines = new DefineCollection
        {
            { CoreKeywordDescriptors.ForceEnableTransparent, 1 },
            { CoreKeywordDescriptors.LineRenderingOffscreenShading, 1 },
            { CoreKeywordDescriptors.FogOnTransparent, 1 }
        };

        public static PassDescriptor LineRenderingOffscreenShadingPass(bool supportLighting)
        {
            return new PassDescriptor
            {
                // Definition
                displayName = HDShaderPassNames.s_LineRenderingOffscreenShading,
                lightMode =  HDShaderPassNames.s_LineRenderingOffscreenShading,

                // Keep the same reference name as SHADERPASS_FORWARD for compatibility.
                referenceName = supportLighting ? "SHADERPASS_FORWARD" : "SHADERPASS_FORWARD_UNLIT",

                useInPreview = false,

                // Collections
                structs = HDShaderPasses.GenerateStructs(null, false, false),
                // We need motion vector version as Forward pass support transparent motion vector and we can't use ifdef for it
                requiredFields = supportLighting ? CoreRequiredFields.BasicLighting : GenerateRequiredFields(),
                renderStates = CoreRenderStates.LineRendering,
                pragmas = HDShaderPasses.GeneratePragmas(CorePragmas.DotsInstanced, false, false),
                defines = HDShaderPasses.GenerateDefines( new DefineCollection { OffscreenShadingLines, supportLighting ? CoreDefines.Forward : CoreDefines.ForwardUnlit }, false, false),
                includes = GenerateIncludes(),

                virtualTextureFeedback = true,
                customInterpolators = CoreCustomInterpolators.Common
            };

            FieldCollection GenerateRequiredFields()
            {
                var fieldCollection = new FieldCollection();

                fieldCollection.Add(CoreRequiredFields.BasicSurfaceData);
                fieldCollection.Add(CoreRequiredFields.AddWriteNormalBuffer);

                return fieldCollection;
            }

            IncludeCollection GenerateIncludes()
            {
                var includes = new IncludeCollection();

                includes.Add(CoreIncludes.CorePregraph);
                if (supportLighting)
                {
                    includes.Add(CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kLighting, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph);
                }
                includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);
                if (supportLighting)
                    includes.Add(CoreIncludes.kLightLoop, IncludeLocation.Pregraph);
                includes.Add(CoreIncludes.CoreUtility);
                if (supportLighting)
                {
                    includes.Add(CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph);
                }
                includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);

                if (supportLighting)
                    includes.Add(kPassForward, IncludeLocation.Postgraph);
                else
                    includes.Add(kPassForwardUnlit, IncludeLocation.Postgraph);

                return includes;
            }
        }

    }
}
