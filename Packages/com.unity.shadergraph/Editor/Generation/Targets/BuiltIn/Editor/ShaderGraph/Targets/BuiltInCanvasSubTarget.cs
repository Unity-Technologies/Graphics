using System;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using UnityEditor.Rendering.Canvas.ShaderGraph;

namespace UnityEditor.Rendering.BuiltIn.ShaderGraph
{
    class BuiltInCanvasSubTarget : CanvasSubTarget<BuiltInTarget>, IRequiresData<CanvasData>, IHasMetadata
    {
        static readonly GUID kSourceCodeGuid = new GUID("5a0372ef872c4103b70866297bd45e38"); // BuiltInCanvasSubTarget.cs

        static readonly string kCanvasPass = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/BuiltInCanvasPass.hlsl";

        public BuiltInCanvasSubTarget()
        {
            displayName = "Canvas";
        }

        public override object saveContext => null;
        protected override string pipelineTag { get; }
        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            context.AddSubShader(GenerateDefaultSubshader( false ));
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha, target.alphaClip);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, target.alphaClip);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            if(target.alphaClip)
                context.AddField(UnityEditor.ShaderGraph.Fields.AlphaTest);
            base.GetFields(ref context);
        }

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
        protected override DefineCollection GetAdditionalDefines()
        {
            var result = new DefineCollection() { CoreDefines.BuiltInTargetAPI };
            if (target.alphaClip)
                result.Add(CoreKeywordDescriptors.AlphaTestOn, 1);
            return result;
        }
        protected override KeywordCollection GetAdditionalKeywords() => new KeywordCollection {};

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<string> registerUndo)
        {
            var builtInTarget = (target as BuiltInTarget);
            context.AddProperty("Alpha Clipping", new Toggle() { value = builtInTarget.alphaClip }, (evt) =>
            {
                if (Equals(builtInTarget.alphaClip, evt.newValue))
                    return;

                registerUndo("Change Alpha Clip");
                builtInTarget.alphaClip = evt.newValue;
                onChange();
            });
        }
    }
}
