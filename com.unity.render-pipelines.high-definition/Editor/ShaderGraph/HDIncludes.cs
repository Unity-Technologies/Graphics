using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class HDIncludes
    {
#region Unlit
        public static IncludeCollection UnlitMeta = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection UnlitDepthOnly = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection UnlitMotionVectors = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection UnlitForwardOnly = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection UnlitDistortion = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDistortion.hlsl", IncludeLocation.Postgraph },
        };
#endregion

#region Lit
        public static IncludeCollection LitGBuffer = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassGBuffer.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection LitMeta = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection LitDepthOnly = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection LitMotionVectors = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection LitForward = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection LitDistortion = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDistortion.hlsl", IncludeLocation.Postgraph },
        };
#endregion

#region Eye
        public static IncludeCollection EyeMeta = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection EyeDepthOnly = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection EyeMotionVectors = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection EyeForwardOnly = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl", IncludeLocation.Postgraph },
        };
#endregion

#region Fabric
        public static IncludeCollection FabricMeta = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection FabricDepthOnly = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection FabricMotionVectors = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection FabricForwardOnly = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl", IncludeLocation.Postgraph },
        };
#endregion

#region Hair
        public static IncludeCollection HairMeta = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Hair.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection HairDepthOnly = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Hair.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection HairMotionVectors = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Hair.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection HairForwardOnly = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Hair.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl", IncludeLocation.Postgraph },
        };
#endregion

#region StackLit
        public static IncludeCollection StackLitMeta = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SphericalCapPivot/SpecularOcclusionDef.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLitDecalData.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection StackLitDepthOnly = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SphericalCapPivot/SpecularOcclusionDef.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLitDecalData.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection StackLitMotionVectors = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SphericalCapPivot/SpecularOcclusionDef.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLitDecalData.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection StackLitDistortion = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SphericalCapPivot/SpecularOcclusionDef.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLitDecalData.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDistortion.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection StackLitForwardOnly = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SphericalCapPivot/SpecularOcclusionDef.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLitDecalData.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl", IncludeLocation.Postgraph },
        };
#endregion

#region Decal
        public static IncludeCollection Decal = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl", IncludeLocation.Postgraph },
        };
#endregion

#region HDLitRaytracing
        public static IncludeCollection HDLitRaytracingIndirect = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingLightLoop.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingIndirect.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection HDLitRaytracingVisibility = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingVisibility.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection HDLitRaytracingForward = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingLightLoop.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingForward.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection HDLitRaytracingGBuffer = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Deferred/RaytracingIntersectonGBuffer.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StandardLit/StandardLit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderpassRaytracingGBuffer.hlsl", IncludeLocation.Postgraph },
        };
#endregion

#region HDUnlitRaytracing
        public static IncludeCollection HDUnlitRaytracingIndirect = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/UnlitRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingIndirect.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection HDUnlitRaytracingForward = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/UnlitRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingForward.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection HDUnlitRaytracingVisibility = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/UnlitRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingVisibility.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection HDUnlitRaytracingGBuffer = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Deferred/RaytracingIntersectonGBuffer.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StandardLit/StandardLit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/UnlitRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderpassRaytracingGBuffer.hlsl", IncludeLocation.Postgraph },
        };
#endregion

#region FabricRaytracing
        public static IncludeCollection FabricRaytracingIndirect = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/FabricRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingLightLoop.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingIndirect.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection FabricRaytracingForward = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/FabricRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingLightLoop.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingForward.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection FabricRaytracingVisibility = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/FabricRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingVisibility.hlsl", IncludeLocation.Postgraph },
        };

        public static IncludeCollection FabricRaytracingGBuffer = new IncludeCollection
        {
            { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Deferred/RaytracingIntersectonGBuffer.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StandardLit/StandardLit.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/FabricRaytracing.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingCommon.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl", IncludeLocation.Pregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderpassRaytracingGBuffer.hlsl", IncludeLocation.Postgraph },
        };
#endregion
    }
}
