using System;
using System.Collections.Generic;
using UnityEditor.Rendering.BuiltIn;
using UnityEditor.Rendering.Canvas.ShaderGraph;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class HDCanvasSubTarget : CanvasSubTarget<HDTarget>, IRequiresData<HDCanvasData>, IHasMetadata
    {
        // Constants
        const string kAssetGuid = "799e4883420742bfa629d3d3d9b674d6"; // CanvasTarget.cs

        static readonly string kHDCanvasPass = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Canvas/HDCanvasPass.hlsl";

        public override void Setup(ref TargetSetupContext context)
        {
            base.Setup(ref context);
            context.AddAssetDependency(new GUID(kAssetGuid), AssetCollection.Flags.SourceDependency);
        }

        protected override string pipelineTag => HDRenderPipeline.k_ShaderTagName;

        protected override IncludeCollection pregraphIncludes => new IncludeCollection
        {
            { CoreIncludes.MinimalCorePregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassSampling.hlsl", IncludeLocation.Pregraph},
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph}, // Need this to make the scene color/depth nodes work
            { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph }
        };
        protected override IncludeCollection postgraphIncludes => new IncludeCollection
        {
            {kHDCanvasPass, IncludeLocation.Postgraph},
        };

        HDCanvasData m_CanvasData;

        HDCanvasData IRequiresData<HDCanvasData>.data
        {
            get => m_CanvasData;
            set => m_CanvasData = value;
        }

        public HDCanvasData CanvasData
        {
            get => m_CanvasData;
            set => m_CanvasData = value;
        }


        // We don't need the save context / update materials for now
        public override object saveContext => null;

        public HDCanvasSubTarget()
        {
            displayName = "Canvas";
        }
        protected override DefineCollection GetAdditionalDefines()
        {
            var result = new DefineCollection();
            if (canvasData.alphaClip)
                result.Add(CoreKeywordDescriptors.AlphaTest, 1);

            result.Add(base.GetAdditionalDefines());
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

        protected override DefineCollection GetPassDefines()
        {
            var defines = base.GetPassDefines();

            if (m_CanvasData.supportsMotionVectors)
            {
                defines.Add(new KeywordDescriptor
                {
                    referenceName = "UI_MOTION_VECTORS",
                    displayName = "UI_MOTION_VECTORS",
                    type = KeywordType.Boolean,
                    definition = KeywordDefinition.ShaderFeature,
                    scope = KeywordScope.Global,
                    value = 1,
                }, 1);
            }

            return defines;
        }

        public override PassDescriptor GenerateUIPassDescriptor(bool isSRP)
        {
            var pass = base.GenerateUIPassDescriptor(isSRP);

            pass.requiredFields.Add(StructFields.Varyings.texCoord2); // Motion vectors support

            // Patch render state to support motion vectors.
            pass.renderStates = new RenderStateCollection
            {
                {RenderState.Cull(Cull.Off)},
                {RenderState.ZWrite(ZWrite.On)},
                {RenderState.ZTest(CanvasUniforms.ZTest)},
                {RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha)},
                {RenderState.ColorMask(CanvasUniforms.ColorMask)},
                {RenderState.Stencil(new StencilDescriptor()
                    {
                        Ref = CanvasUniforms.Ref,
                        Comp = CanvasUniforms.Comp,
                        Pass = CanvasUniforms.Pass,
                        ReadMask = CanvasUniforms.ReadMask,
                        WriteMask = CanvasUniforms.WriteMask,
                    })
                }
            };

            return pass;
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<string> registerUndo)
        {
            base.GetPropertiesGUI(ref context, onChange, registerUndo);

            context.AddProperty("Supports Motion vectors", new Toggle() { value = m_CanvasData.supportsMotionVectors }, (evt) =>
            {
                if (Equals(m_CanvasData.supportsMotionVectors, evt.newValue))
                    return;

                registerUndo("Supports Motion vectors");
                m_CanvasData.supportsMotionVectors = evt.newValue;
                onChange();
            });
        }

        public override bool IsNodeAllowedBySubTarget(Type nodeType)
        {
            base.IsNodeAllowedBySubTarget(nodeType);

            bool allowed = true;
            if (nodeType == typeof(BuiltinCorneaIOR))
                return false;
            if (nodeType == typeof(BuiltinIrisPlaneOffset))
                return false;
            if (nodeType == typeof(BuiltinIrisRadius))
                return false;
            if (nodeType == typeof(BlendNormal_Water))
                return false;
            if (nodeType == typeof(ComputeVertexData_Water))
                return false;
            if (nodeType == typeof(EvaluateFoamData_Water))
                return false;
            if (nodeType == typeof(EvaluateRefractionData_Water))
                return false;
            if (nodeType == typeof(EvaluateScatteringColor_Water))
                return false;
            if (nodeType == typeof(EvaluateSimulationAdditionalData_Water))
                return false;
            if (nodeType == typeof(EvaluateSimulationCaustics_Water))
                return false;
            if (nodeType == typeof(EvaluateTipThickness_Water))
                return false;
            if (nodeType == typeof(BlendNormal_Water))
                return false;

            return allowed;
        }
    }
}
