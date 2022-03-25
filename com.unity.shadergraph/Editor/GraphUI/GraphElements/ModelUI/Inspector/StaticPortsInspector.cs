using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.Registry;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class StaticPortsInspector : FieldsInspector
    {
        public const string inspectorOnlyHint = ".InspectorOnly";

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
            var hints = stencil.GetUIHints(nodeModel.registryKey);

            foreach (var port in nodeReader.GetInputPorts())
            {
                if (!port.GetField("IsStatic", out bool isStatic) || !isStatic) continue;

                var portName = port.GetName();
                if (!hints.ContainsKey(portName + inspectorOnlyHint)) continue;

                var constant = stencil.CreateConstantValue(ShaderGraphExampleTypes.GetGraphType(port));
                if (constant is ICLDSConstant cldsConstant)
                {
                    cldsConstant.Initialize(graphModel.GraphHandler, nodeModel.graphDataName, portName);
                }

                yield return InlineValueEditor.CreateEditorForConstant(m_OwnerElement?.RootView, nodeModel, constant, false, portName);
            }
        }
    }
}
