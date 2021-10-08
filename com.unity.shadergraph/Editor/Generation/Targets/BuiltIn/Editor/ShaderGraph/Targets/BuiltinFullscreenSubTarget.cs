using UnityEditor.ShaderGraph;
using UnityEngine;
using static UnityEditor.Rendering.BuiltIn.ShaderUtils;
using UnityEditor.Rendering.BuiltIn;
using System;
using UnityEditor.Rendering.Fullscreen.ShaderGraph;

namespace UnityEditor.Rendering.BuiltIn.ShaderGraph
{
    class BuiltInFullscreenSubTarget : FullscreenSubTarget<BuiltInTarget>, IRequiresData<FullscreenData>
    {
        static readonly GUID kSourceCodeGuid = new GUID("3107a8a084c35ab4cb765b37e0699ce3");  // BuiltInFullscreenSubTarget.cs // TODO

        // In builtin there is no inverse view projection matrix, so we need to compute it in the vertex shader
        protected override string fullscreenDrawProceduralInclude => "Packages/com.unity.shadergraph/Editor/Generation/Targets/Builtin/Editor/ShaderGraph/Includes/FullscreenDrawProcedural.hlsl";
        protected override string fullscreenBlitInclude => "Packages/com.unity.shadergraph/Editor/Generation/Targets/Builtin/Editor/ShaderGraph/Includes/FullscreenBlit.hlsl";

        public override void Setup(ref TargetSetupContext context)
        {
            base.Setup(ref context);
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
        }

        // We don't need the save context / update materials for now
        public override object saveContext => null;

        protected override string pipelineTag => ""; // Buitin is enabled by having an empty tag

        public override IncludeCollection GetPreGraphIncludes()
        {
            return new IncludeCollection
            {
                { kFullscreenShaderPass, IncludeLocation.Pregraph }, // For VR
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { kSpaceTransforms, IncludeLocation.Pregraph },
            };
        }

        protected override DefineCollection GetPassDefines(FullscreenCompatibility compatibility)
        {
            return CoreDefines.BuiltInTargetAPI;
        }

        public BuiltInFullscreenSubTarget()
        {
            displayName = "Fullscreen";
        }
    }
}
