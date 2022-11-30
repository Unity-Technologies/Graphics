using System;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    enum LegacyTargetType
    {
        Blank,
        URPLit,
        URPUnlit
    }

    class ShaderGraphTemplate : GraphTemplate
    {
        readonly bool m_IsSubgraph;
        readonly LegacyTargetType m_TargetType;

        public override Type StencilType => typeof(ShaderGraphStencil);

        public override string DefaultAssetName => ShaderGraphStencil.DefaultGraphAssetName;

        public override string GraphFileExtension => ShaderGraphStencil.GraphExtension;

        public override string GraphTypeName { get; }

        Target m_Target;

        public Action GraphHandlerInitializationCallback;

        public override void InitBasicGraph(GraphModel graphModel)
        {
            base.InitBasicGraph(graphModel);

            if (graphModel.Asset is ShaderGraphAsset shaderGraphAsset)
                shaderGraphAsset.InitializeGraphData(m_TargetType);

            if (graphModel is ShaderGraphModel shaderGraphModel)
            {
                m_Target = m_TargetType switch
                {
                    LegacyTargetType.Blank => null,
                    LegacyTargetType.URPLit => URPTargetUtils.ConfigureURPLit(shaderGraphModel.GraphHandler),
                    LegacyTargetType.URPUnlit => URPTargetUtils.ConfigureURPUnlit(shaderGraphModel.GraphHandler),
                    _ => throw new ArgumentOutOfRangeException("ShaderGraphTemplate.m_TargetType")
                };

                shaderGraphModel.Init(m_IsSubgraph, m_Target);
            }
        }

        public static ShaderGraphAsset CreateInMemoryGraphFromTemplate(ShaderGraphTemplate graphTemplate)
        {
            var graphAsset = ScriptableObject.CreateInstance<ShaderGraphAsset>();

            if (graphAsset != null)
            {
                graphAsset.Name = graphTemplate.DefaultAssetName;
                graphAsset.CreateGraph(graphTemplate.StencilType);
                graphTemplate?.InitBasicGraph(graphAsset.GraphModel);
                graphAsset = (ShaderGraphAsset)graphAsset.Import();
            }

            return graphAsset;
        }

        internal ShaderGraphTemplate(bool isSubgraph, LegacyTargetType targetType)
        {
            m_IsSubgraph = isSubgraph;
            m_TargetType = targetType;
        }
    }
}
