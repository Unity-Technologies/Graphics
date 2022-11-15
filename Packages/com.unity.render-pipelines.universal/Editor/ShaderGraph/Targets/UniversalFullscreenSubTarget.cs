using UnityEditor.ShaderGraph;
using UnityEngine;
using static UnityEditor.Rendering.BuiltIn.ShaderUtils;
using UnityEditor.Rendering.BuiltIn;
using System;
using UnityEditor.Rendering.Fullscreen.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    class UniversalFullscreenSubTarget : FullscreenSubTarget<UniversalTarget>, IRequiresData<FullscreenData>, IHasMetadata
    {
        static readonly GUID kSourceCodeGuid = new GUID("48080a5025a54a84087e882e2f988642");  // UniversalFullscreenSubTarget.cs // TODO

        public override void Setup(ref TargetSetupContext context)
        {
            base.Setup(ref context);
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
        }

        // We don't need the save context / update materials for now
        public override object saveContext => null;

        protected override string pipelineTag => UniversalTarget.kPipelineTag;

        const string kURPInput = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl";

        protected override IncludeCollection pregraphIncludes => new IncludeCollection
        {
            { kURPInput, IncludeLocation.Pregraph }, // Include before kInstancing
            { kInstancing, IncludeLocation.Pregraph }, // For VR
            { CoreIncludes.CorePregraph },
            { CoreIncludes.ShaderGraphPregraph },
        };

        public UniversalFullscreenSubTarget()
        {
            displayName = "Fullscreen";
        }
    }
}
