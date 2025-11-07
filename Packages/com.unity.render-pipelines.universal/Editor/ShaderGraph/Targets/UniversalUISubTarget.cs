using UnityEditor.ShaderGraph;
using System;
using System.Collections.Generic;
using UnityEditor.Rendering.UITK.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    class UniversalUISubTarget: UISubTarget<UniversalTarget>
    {
        static readonly GUID kSourceCodeGuid = new GUID("b1197b10aa62577498d67cffe1d3bd43");  // UniversalUISubTarget.cs

        static readonly string kUITKPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UITKPass.hlsl";
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
            { kUIShim, IncludeLocation.Pregraph }
        };
        protected override IncludeCollection postgraphIncludes => new IncludeCollection
        {
            {kUITKPass, IncludeLocation.Postgraph},
        };

        public UniversalUISubTarget()
        {
            displayName = "UI";
        }

        HashSet<Type> m_UnsupportedNodes;

        public override void GetFields(ref TargetFieldContext context)
        {
            context.AddField(Fields.GraphVertex);
            context.AddField(Fields.GraphPixel);

            base.GetFields(ref context);

            switch (target.alphaMode)
            {
                case AlphaMode.Premultiply:
                    context.AddField(UniversalFields.BlendPremultiply);
                    break;
                case AlphaMode.Additive:
                    context.AddField(UniversalFields.BlendAdd);
                    break;
                case AlphaMode.Multiply:
                    context.AddField(UniversalFields.BlendMultiply);
                    break;
                default:
                    context.AddField(Fields.BlendAlpha);
                    break;
            }

        }

        public override PassDescriptor GenerateUIPassDescriptor(bool isSRP)
        {
            PassDescriptor passDescriptor = base.GenerateUIPassDescriptor(isSRP);

            var result = new RenderStateCollection();

            result.Add(RenderState.Cull(Cull.Off));
            result.Add(RenderState.ZWrite(ZWrite.Off));

            switch (target.alphaMode)
            {
                case AlphaMode.Alpha:
                    result.Add(RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha));
                    break;
                case AlphaMode.Premultiply:
                    result.Add(RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha));
                    break;
                case AlphaMode.Additive:
                    result.Add(RenderState.Blend(Blend.SrcAlpha, Blend.One, Blend.One, Blend.OneMinusSrcAlpha));
                    break;
                case AlphaMode.Multiply:
                    result.Add(RenderState.Blend(Blend.DstColor, Blend.Zero, Blend.Zero, Blend.One));
                    break;
            }

            passDescriptor.renderStates = result;

            return passDescriptor;
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            context.AddHelpBox(MessageType.Info, "This shader is intended for use with Unity's UI Toolkit. It has special requirements.\n\nStart with the UI > Render Type Branch node and connect its outputs to the Fragment node.");
            context.AddProperty("Blending Mode", new UnityEngine.UIElements.EnumField(AlphaMode.Alpha) { value = target.alphaMode }, (evt) =>
            {
                if (Equals(target.alphaMode, evt.newValue))
                    return;

                registerUndo("Change Blend");
                target.alphaMode = (AlphaMode)evt.newValue;
                onChange();
            });
        }
    }
}
