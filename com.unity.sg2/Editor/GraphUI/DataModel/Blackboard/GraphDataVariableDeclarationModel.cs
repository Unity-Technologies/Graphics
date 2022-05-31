using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
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

        public GraphDataVariableDeclarationModel()
        {
        }

        ShaderGraphModel shaderGraphModel => GraphModel as ShaderGraphModel;

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
