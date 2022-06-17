using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class GraphDataVariableDeclarationModel : VariableDeclarationModel
    {
        [SerializeField]
        [HideInInspector]
        string m_ContextNodeName;

        /// <summary>
        // Name of the context node that owns the entry for this Variable
        /// </summary>
        public string contextNodeName
        {
            get => m_ContextNodeName;
            set => m_ContextNodeName = value;
        }

        [SerializeField]
        [HideInInspector]
        string m_GraphDataName;

        /// <summary>
        // Name of the port on the Context Node that owns the entry for this Variable
        /// </summary>
        public string graphDataName
        {
            get => m_GraphDataName;
            set => m_GraphDataName = value;
        }

        ShaderGraphModel shaderGraphModel => GraphModel as ShaderGraphModel;

        PortHandler contextEntry => shaderGraphModel.GraphHandler
            .GetNode(contextNodeName)
            .GetPort(graphDataName);

        public ContextEntryEnumTags.DataSource ShaderDeclaration
        {
            // TODO: Actual data
            get => ContextEntryEnumTags.DataSource.PerMaterial;
            set { }
        }

        public override bool IsExposed
        {
            // TODO: Maybe this is a bit too direct. This seems to get used during initialization, which breaks things.
            get
            {
                try
                {
                    return contextEntry
                        .GetField<ContextEntryEnumTags.PropertyBlockUsage>(ContextEntryEnumTags.kPropertyBlockUsage)
                        .GetData() == ContextEntryEnumTags.PropertyBlockUsage.Included;
                }
                catch (NullReferenceException)
                {
                    return true;
                }
            }
            set
            {
                try
                {
                    contextEntry
                        .GetField<ContextEntryEnumTags.PropertyBlockUsage>(ContextEntryEnumTags.kPropertyBlockUsage)
                        .SetData(value ? ContextEntryEnumTags.PropertyBlockUsage.Included : ContextEntryEnumTags.PropertyBlockUsage.Excluded);
                }
                catch (NullReferenceException)
                {
                    // no-op
                }
            }
        }

        public GraphDataVariableDeclarationModel() { }

        public override void CreateInitializationValue()
        {
            if (string.IsNullOrEmpty(contextNodeName) || string.IsNullOrEmpty(graphDataName))
            {
                return;
            }

            if (GraphModel?.Stencil?.GetConstantType(DataType) != null)
            {
                InitializationModel = GraphModel.Stencil.CreateConstantValue(DataType);
                if (InitializationModel is BaseShaderGraphConstant cldsConstant)
                {
                    cldsConstant.Initialize(shaderGraphModel, contextNodeName, graphDataName);
                }
            }
        }
    }
}
