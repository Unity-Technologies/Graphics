using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class StaticPortsInspector : SGFieldsInspector
    {
        // TODO GTF UPGRADE: support edition of multiple models.

        public StaticPortsInspector(string name, IEnumerable<Model> models, RootView rootView, string parentClassName)
            : base(name, models, rootView, parentClassName)
        {
        }

        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            var models = m_Models.OfType<SGNodeModel>();
            if (!models.Any()) yield break;

            var nodeModel = models.First();
            if (!nodeModel.TryGetNodeHandler(out var nodeReader)) yield break;

            var graphModel = (SGGraphModel)nodeModel.GraphModel;
            var stencil = (ShaderGraphStencil)graphModel.Stencil;
            var nodeUIDescriptor = stencil.GetUIHints(nodeModel.registryKey, nodeReader);

            foreach (var port in nodeReader.GetPorts())
            {
                var staticField = port.GetTypeField()?.GetSubField<bool>("IsStatic");
                var isStatic = staticField?.GetData() ?? false;
                if (!isStatic) continue;

                var portName = port.ID.LocalPath;
                var parameterUIDescriptor = nodeUIDescriptor.GetParameterInfo(portName);

                if (!parameterUIDescriptor.InspectorOnly) continue;

                var constant = stencil.CreateConstantValue(ShaderGraphExampleTypes.GetGraphType(port));
                if (constant is BaseShaderGraphConstant cldsConstant)
                {
                    cldsConstant.BindTo(nodeModel.graphDataName, portName);
                }

                // TODO: Last argument is label text, should come from UI strings
                yield return InlineValueEditor.CreateEditorForConstants(RootView, models, new [] { constant }, false, portName);
            }
        }

        public override bool IsEmpty()
        {
            return m_Fields.Count != 0;
        }
    }
}
