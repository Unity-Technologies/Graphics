using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class SubgraphOutputListViewController : SGListViewController
    {
        public event Action<List<int>> beforeItemsRemoved;

        public override void RemoveItems(List<int> indices)
        {
            beforeItemsRemoved?.Invoke(indices);
            base.RemoveItems(indices);
        }
    }

    public class SubgraphOutputsInspector : SGFieldsInspector
    {
        List<(string name, int typeIndex)> m_Outputs;
        ListPropertyField<(string name, int typeIndex)> m_ListField;

        public SubgraphOutputsInspector(string name, IModel model, IModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
            m_Outputs = new List<(string name, int typeIndex)>();
            if (m_Model is not GraphDataContextNodeModel contextNodeModel) return;

            var stencil = (ShaderGraphStencil)contextNodeModel.GraphModel.Stencil;
            m_ListField = new ListPropertyField<(string name, int typeIndex)>(
                m_OwnerElement.RootView,
                m_Outputs,
                getAddItemOptions: () => ShaderGraphStencil.k_SupportedBlackboardTypes.Select(t => stencil.TypeMetadataResolver.Resolve(t)?.FriendlyName ?? t.Name).ToList(),
                getItemDisplayName: obj => obj.ToString(),
                onAddItemClicked: OnItemsAdded,
                onSelectionChanged: _ => { },
                onItemRemoved: () => { },
                makeOptionsUnique: false,
                makeListReorderable: false
            );

            var controller = new SubgraphOutputListViewController();
            controller.beforeItemsRemoved += ints =>
            {
                m_OwnerElement.RootView.Dispatch(new RemoveContextEntryCommand((GraphDataContextNodeModel)model, m_Outputs[ints[0]].name));
            };
            m_ListField.listView.SetViewController(controller);

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
                var textField = ve.Q<TextField>();
                textField.value = m_Outputs[i].name;
                textField.isDelayed = true;

                textField.RegisterValueChangedCallback(evt =>
                {
                    m_OwnerElement.RootView.Dispatch(new RenameContextEntryCommand(contextNodeModel, evt.previousValue, evt.newValue));
                });

                var dropdownField = ve.Q<DropdownField>();
                dropdownField.index = m_Outputs[i].typeIndex;
                dropdownField.RegisterValueChangedCallback(evt =>
                {
                    m_OwnerElement.RootView.Dispatch(new ChangeContextEntryTypeCommand(contextNodeModel, m_Outputs[i].name, ShaderGraphStencil.k_SupportedBlackboardTypes[dropdownField.index]));
                });
            };
        }

        void OnItemsAdded(object selectedItemString)
        {
            if (m_Model is not GraphDataContextNodeModel contextNodeModel) return;
            if (!contextNodeModel.TryGetNodeReader(out var nodeHandler)) return;

            var stencil = (ShaderGraphStencil)contextNodeModel.GraphModel.Stencil;

            var existingNames = nodeHandler.GetPorts().Select(p => p.ID.LocalPath);
            var entryName = ObjectNames.GetUniqueName(existingNames.ToArray(), "New");
            var type = ShaderGraphStencil.k_SupportedBlackboardTypes.Select(t => stencil.TypeMetadataResolver.Resolve(t)?.FriendlyName ?? t.Name).ToList().IndexOf(selectedItemString.ToString());

            m_OwnerElement.RootView.Dispatch(new AddContextEntryCommand(contextNodeModel, entryName, ShaderGraphStencil.k_SupportedBlackboardTypes[type]));
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
