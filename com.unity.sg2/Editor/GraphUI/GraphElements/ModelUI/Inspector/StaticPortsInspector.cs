using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class StaticPortsInspector : SGFieldsInspector
    {
        public StaticPortsInspector(string name, IModel model, IModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
        }

        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            if (m_Model is not GraphDataNodeModel nodeModel) yield break;
            if (!nodeModel.TryGetNodeReader(out var nodeReader)) yield break;

            var graphModel = (ShaderGraphModel)nodeModel.GraphModel;
            var stencil = (ShaderGraphStencil)graphModel.Stencil;
            var nodeUIDescriptor = stencil.GetUIHints(nodeModel.registryKey);

            foreach (var port in nodeReader.GetPorts())
            {
                var staticField = port.GetTypeField().GetSubField<bool>("IsStatic");
                var isStatic = staticField?.GetData() ?? false;
                if (!isStatic) continue;

                var portName = port.ID.LocalPath;
                var parameterUIDescriptor = nodeUIDescriptor.GetParameterInfo(portName);

                if (!parameterUIDescriptor.InspectorOnly) continue;

                var constant = stencil.CreateConstantValue(ShaderGraphExampleTypes.GetGraphType(port));
                if (constant is BaseShaderGraphConstant cldsConstant)
                {
                    cldsConstant.Initialize(graphModel.GraphHandler, nodeModel.graphDataName, portName);
                }

                // TODO: Last argument is label text, should come from UI strings
                yield return InlineValueEditor.CreateEditorForConstant(m_OwnerElement?.RootView, nodeModel, constant, false, portName);
            }
        }

        public override bool IsEmpty()
        {
            return m_Fields.Count != 0;
        }
    }
}
