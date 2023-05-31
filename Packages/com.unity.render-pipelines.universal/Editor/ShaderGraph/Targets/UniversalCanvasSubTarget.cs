using UnityEditor.ShaderGraph;
using UnityEngine;
using static UnityEditor.Rendering.BuiltIn.ShaderUtils;
using UnityEditor.Rendering.BuiltIn;
using System;
using UnityEngine.UIElements;
using UnityEditor.Rendering.Canvas.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    class UniversalCanvasSubTarget: CanvasSubTarget<UniversalTarget>, IRequiresData<CanvasData>, IHasMetadata
    {
        static readonly GUID kSourceCodeGuid = new GUID("f7075c3a804b49bf86535f6f86615132");  // UniversalCanvasSubTarget.cs

        static readonly string kCanvasPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/CanvasPass.hlsl";
        public override void Setup(ref TargetSetupContext context)
        {
            base.Setup(ref context);
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
        }
        public override bool IsActive() => true;

        // We don't need the save context / update materials for now
        public override object saveContext => null;

        protected override string pipelineTag => UniversalTarget.kPipelineTag;

        protected override IncludeCollection pregraphIncludes => new IncludeCollection
        {
            { CoreIncludes.CorePregraph },
            { kInstancing, IncludeLocation.Pregraph },
            { CoreIncludes.ShaderGraphPregraph },
        };
        protected override IncludeCollection postgraphIncludes => new IncludeCollection
        {
            {kCanvasPass, IncludeLocation.Postgraph},
        };

        public UniversalCanvasSubTarget()
        {
            displayName = "Canvas";
        }

        protected override DefineCollection GetAdditionalDefines()
        {
            var result = new DefineCollection();
            if (canvasData.alphaClip)
                result.Add(CoreKeywordDescriptors.AlphaTestOn, 1);
            return result;
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, canvasData.alphaClip);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);
            if(canvasData.alphaClip)
                context.AddField(UnityEditor.ShaderGraph.Fields.AlphaTest);
        }
        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);
            if (canvasData.alphaClip)
                collector.AddShaderProperty(CanvasProperties.AlphaTest);
        }
    }
}
