using System;
using System.Collections.Generic;
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
        protected virtual void CollectPassKeywords(ref PassDescriptor pass) { }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);
            if (canvasData.alphaClip)
                collector.AddShaderProperty(CanvasProperties.AlphaTest);
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
