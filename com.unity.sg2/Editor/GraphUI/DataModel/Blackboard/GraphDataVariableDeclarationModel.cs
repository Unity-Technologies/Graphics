using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class GraphDataVariableDeclarationModel : VariableDeclarationModel
    {
        [SerializeField]
        [HideInInspector]
        string m_ContextNodeName;

        /// <summary>
        /// Name of the context node that owns the entry for this Variable
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
        /// Name of the port on the Context Node that owns the entry for this Variable
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
            get =>
                contextEntry
                    .GetField<ContextEntryEnumTags.DataSource>(ContextEntryEnumTags.kDataSource)
                    .GetData();
            set =>
                contextEntry
                    .GetField<ContextEntryEnumTags.DataSource>(ContextEntryEnumTags.kDataSource)
                    .SetData(value);
        }

        /// <summary>
        /// Returns true if this variable declaration's data type is exposable according to the stencil,
        /// false otherwise.
        /// </summary>
        public bool IsExposable => ((ShaderGraphStencil)shaderGraphModel?.Stencil)?.IsExposable(DataType) ?? false;

        public override bool IsExposed
        {
            get
            {
                if (!IsExposable)
                {
                    return false;
                }

                return contextEntry
                    .GetField<ContextEntryEnumTags.PropertyBlockUsage>(ContextEntryEnumTags.kPropertyBlockUsage)
                    .GetData() == ContextEntryEnumTags.PropertyBlockUsage.Included;
            }
            set
            {
                if (!IsExposable)
                {
                    value = false;
                }

                contextEntry
                    .GetField<ContextEntryEnumTags.PropertyBlockUsage>(ContextEntryEnumTags.kPropertyBlockUsage)
                    .SetData(value ? ContextEntryEnumTags.PropertyBlockUsage.Included : ContextEntryEnumTags.PropertyBlockUsage.Excluded);
            }
        }

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
