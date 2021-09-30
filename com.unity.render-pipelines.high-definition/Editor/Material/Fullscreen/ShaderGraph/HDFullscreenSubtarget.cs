using UnityEditor.ShaderGraph;
using UnityEngine;
using static UnityEditor.Rendering.BuiltIn.ShaderUtils;
using UnityEditor.Rendering.BuiltIn;
using System;
using UnityEditor.Rendering.Fullscreen.ShaderGraph;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class HDFullscreenSubTarget : FullscreenSubTarget<HDTarget>, IHasMetadata
    {
        static readonly GUID kSourceCodeGuid = new GUID("657f6eb2bee4e2f4985ec1ac58eb04cb");  // HDFullscreenSubTarget.cs // TODO

        public override void Setup(ref TargetSetupContext context)
        {
            base.Setup(ref context);
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
        }

        protected override string pipelineTag => HDRenderPipeline.k_ShaderTagName;

        protected override IncludeCollection pregraphIncludes => new IncludeCollection
        {
            { CoreIncludes.MinimalCorePregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassSampling.hlsl", IncludeLocation.Pregraph},
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph}, // Need this to make the scene color/depth nodes work
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fullscreen/HDFullscreenFunctions.hlsl", IncludeLocation.Pregraph},
            { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph }
        };

        // We don't need the save context / update materials for now
        public override object saveContext => null;

        public HDFullscreenSubTarget()
        {
            displayName = "Fullscreen";
        }
    }
}
