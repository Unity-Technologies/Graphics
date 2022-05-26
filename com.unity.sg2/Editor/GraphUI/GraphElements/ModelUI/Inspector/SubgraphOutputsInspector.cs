using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    // things to do:
    //  - create field visual element (horizontal group: editable label, dropdown)
    //      - expose context creation stuff - pull out of blackboard setup code in stencil
    //      - no constants needed here
    //
    public class SubgraphOutputsInspector : SGFieldsInspector
    {
        List<(string name, int typeIndex)> m_Outputs;
        ListPropertyField<(string name, int typeIndex)> m_ListField;

        public SubgraphOutputsInspector(string name, IModel model, IModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
            m_Outputs = new List<(string name, int typeIndex)>();

            var stencil = (ShaderGraphStencil)((NodeModel)m_Model).GraphModel.Stencil;
            m_ListField = new ListPropertyField<(string name, int typeIndex)>(
                m_OwnerElement.RootView,
                m_Outputs,
                getAddItemOptions: () => ShaderGraphStencil.k_SupportedBlackboardTypes.Select(t => stencil.TypeMetadataResolver.Resolve(t)?.FriendlyName ?? t.Name).ToList(),
                getItemDisplayName: obj => obj.ToString(),
                onAddItemClicked: AddItem,
                onSelectionChanged: _ => { },
                onItemRemoved: () => { },
                makeOptionsUnique: false,
                makeListReorderable: false
            );

            m_ListField.listView.makeItem = () =>
            {
                var ve = new VisualElement();
                GraphElementHelper.LoadTemplateAndStylesheet(ve, "SubgraphOutputRow", "sg-subgraph-output-row");
                ve.Q<DropdownField>().choices = ShaderGraphStencil.k_SupportedBlackboardTypes
                    .Select(t => stencil.TypeMetadataResolver.Resolve(t)?.FriendlyName ?? t.Name)
                    .ToList();

                return ve;
            };

            m_ListField.listView.bindItem = (ve, i) =>
            {
                ve.Q<TextField>().value = m_Outputs[i].name;
                ve.Q<DropdownField>().index = m_Outputs[i].typeIndex;
            };
        }

        private void AddItem(object selectedItemString)
        {
            // TODO: Increment name (ObjectNames.GetUniqueName)
            // TODO: Create context entry with correct type
            m_OwnerElement.RootView.Dispatch(new AddContextEntryCommand((GraphDataContextNodeModel)m_Model, "New", TypeHandle.Float));
        }

        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            yield return new LabelPropertyField("Inputs", m_OwnerElement.RootView);
            yield return m_ListField;
        }

        protected override void UpdatePartFromModel()
        {
            base.UpdatePartFromModel();

            if (m_Model is not GraphDataContextNodeModel contextNodeModel) return;
            if (!contextNodeModel.TryGetNodeReader(out var nodeHandler)) return;

            m_Outputs.Clear();
            foreach (var portReader in nodeHandler.GetPorts())
            {
                if (!portReader.IsHorizontal || !portReader.IsInput)
                    continue;

                m_Outputs.Add((portReader.ID.LocalPath, Array.IndexOf(ShaderGraphStencil.k_SupportedBlackboardTypes, ShaderGraphExampleTypes.GetGraphType(portReader))));
            }

            m_ListField.listView.itemsSource = m_Outputs;
            m_ListField.listView.Rebuild();
        }

        public override bool IsEmpty() => false;
    }
}
