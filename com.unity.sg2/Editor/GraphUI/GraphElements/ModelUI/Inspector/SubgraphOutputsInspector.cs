using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
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
            // Used so we can remove the corresponding rows from the graph handler before the list deletes them.
            beforeItemsRemoved?.Invoke(indices);
            base.RemoveItems(indices);
        }
    }

    public class SubgraphOutputsInspector : SGFieldsInspector
    {
        class SubgraphOutputRow
        {
            public string Name;
            public int TypeIndex;
        }

        GraphDataContextNodeModel m_ContextNodeModel;
        TypeHandle[] m_AvailableTypes;
        ShaderGraphStencil m_Stencil;
        List<SubgraphOutputRow> m_OutputRows;
        ListPropertyField<SubgraphOutputRow> m_OutputRowListField;

        public SubgraphOutputsInspector(string name, IModel model, IModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
            m_OutputRows = new List<SubgraphOutputRow>();
            m_AvailableTypes = ShaderGraphStencil.k_SupportedBlackboardTypes;

            if (m_Model is not GraphDataContextNodeModel contextNodeModel) return;
            m_ContextNodeModel = contextNodeModel;
            m_Stencil = (ShaderGraphStencil)contextNodeModel.GraphModel.Stencil;

            var typeNames = m_AvailableTypes.Select(GetTypeDisplayName).ToList();
            m_OutputRowListField = new ListPropertyField<SubgraphOutputRow>(
                m_OwnerElement.RootView,
                m_OutputRows,
                // NOTE: OnAddMenuItemSelected will cast the objects in the collection below back to TypeHandles
                // TODO (Joe): SG1 doesn't show a menu when pressing the + button
                getAddItemData: () => m_AvailableTypes.Cast<object>().ToList(),
                getAddItemMenuString: obj => GetTypeDisplayName((TypeHandle)obj),
                onAddItemClicked: OnAddMenuItemSelected,
                onSelectionChanged: _ => { }, // Not used
                onItemRemoved: () => { }, // Not used, see SubgraphOutputListViewController
                makeOptionsUnique: false,
                makeListReorderable: false
            );

            m_OutputRowListField.listView.name = "sg-subgraph-output-list";

            var controller = new SubgraphOutputListViewController();
            controller.beforeItemsRemoved += indices =>
            {
                foreach (var i in indices)
                {
                    m_OwnerElement.RootView.Dispatch(new RemoveContextEntryCommand((GraphDataContextNodeModel)model, m_OutputRows[i].Name));
                }
            };

            m_OutputRowListField.listView.SetViewController(controller);

            m_OutputRowListField.listView.makeItem = () =>
            {
                var ve = new VisualElement();
                // TODO (Joe): SG1's fields use the style of labels, with no field background.
                GraphElementHelper.LoadTemplateAndStylesheet(ve, "SubgraphOutputRow", "sg-subgraph-output-row");
                ve.Q<DropdownField>().choices = typeNames;
                return ve;
            };

            m_OutputRowListField.listView.bindItem = (ve, i) =>
            {
                var textField = ve.Q<TextField>();
                textField.SetValueWithoutNotify(m_OutputRows[i].Name);
                textField.isDelayed = true;
                textField.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue == evt.previousValue) return;
                    var uniqueName = GetUniqueOutputName(evt.newValue);
                    m_OwnerElement.RootView.Dispatch(new RenameContextEntryCommand(contextNodeModel, m_OutputRows[i].Name, uniqueName));
                });

                var dropdownField = ve.Q<DropdownField>();
                dropdownField.SetValueWithoutNotify(dropdownField.choices[m_OutputRows[i].TypeIndex]);
                dropdownField.RegisterValueChangedCallback(_ => // Event gives us a string, not the index
                {
                    m_OwnerElement.RootView.Dispatch(new ChangeContextEntryTypeCommand(contextNodeModel, m_OutputRows[i].Name, m_AvailableTypes[dropdownField.index]));
                });
            };
        }

        string GetTypeDisplayName(TypeHandle typeHandle)
        {
            return m_Stencil.TypeMetadataResolver.Resolve(typeHandle)?.FriendlyName ?? typeHandle.Name;
        }

        string GetUniqueOutputName(string proposedName)
        {
            // Make sure this name can't be interpreted as a path
            // TODO (Joe): It'd be better to separate the display and data names
            proposedName = proposedName.Replace('.', '_');

            if (!m_ContextNodeModel.TryGetNodeReader(out var nodeHandler)) return proposedName;

            var existingNames = nodeHandler.GetPorts().Select(p => p.ID.LocalPath);
            return ObjectNames.GetUniqueName(existingNames.ToArray(), proposedName);
        }

        void OnAddMenuItemSelected(object selectedTypeData)
        {
            var entryName = GetUniqueOutputName("New");
            m_OwnerElement.RootView.Dispatch(new AddContextEntryCommand(m_ContextNodeModel, entryName, (TypeHandle)selectedTypeData));
        }

        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            yield return new LabelPropertyField("Inputs", m_OwnerElement.RootView);
            yield return m_OutputRowListField;
        }

        protected override void UpdatePartFromModel()
        {
            base.UpdatePartFromModel();

            if (!m_ContextNodeModel.TryGetNodeReader(out var nodeHandler)) return;

            m_OutputRows.Clear();
            foreach (var portHandler in nodeHandler.GetPorts())
            {
                if (!portHandler.IsHorizontal || !portHandler.IsInput)
                {
                    continue;
                }

                m_OutputRows.Add(new SubgraphOutputRow {Name = portHandler.ID.LocalPath, TypeIndex = Array.IndexOf(m_AvailableTypes, ShaderGraphExampleTypes.GetGraphType(portHandler))});
            }

            m_OutputRowListField.listView.itemsSource = m_OutputRows;
            m_OutputRowListField.listView.Rebuild();
        }

        public override bool IsEmpty() => false;
    }
}
