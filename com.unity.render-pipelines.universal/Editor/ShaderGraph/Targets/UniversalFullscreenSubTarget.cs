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

        protected override IncludeCollection pregraphIncludes => new IncludeCollection
        {
            { CoreIncludes.CorePregraph },
            { CoreIncludes.ShaderGraphPregraph },
        };

        public UniversalFullscreenSubTarget()
        {
            displayName = "Fullscreen";
        }

        protected override KeywordCollection GetPassKeywords(FullscreenCompatibility compatibility)
        {
            return new KeywordCollection
            {
                useDrawProcedural
            };
        }

        // For GLES 2 support
        static KeywordDescriptor useDrawProcedural = new KeywordDescriptor
        {
            displayName = "Use Draw Procedural",
            referenceName = "_USE_DRAW_PROCEDURAL",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Vertex,
        };
    }
}
