using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine;
using Unity.GraphToolsFoundation;
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

    class SubgraphOutputsInspector : SGFieldsInspector
    {
        // TODO GTF UPGRADE: support edition of multiple models.

        class SubgraphOutputRow
        {
            public string Name;
            public int TypeIndex;
        }

        GraphDataContextNodeModel m_ContextNodeModel;
        TypeHandle[] m_AvailableTypes;
        ShaderGraphStencil m_Stencil;
        List<SubgraphOutputRow> m_OutputRows;
        ListPropertyField m_OutputRowListField;

        public SubgraphOutputsInspector(string name, IEnumerable<Model> models, RootView rootView, string parentClassName)
            : base(name, models, rootView, parentClassName)
        {
            m_OutputRows = new List<SubgraphOutputRow>();
            m_AvailableTypes = ShaderGraphExampleTypes.SubgraphOutputTypes.ToArray();

            var contextNodeModels = m_Models.OfType<GraphDataContextNodeModel>();
            if (!contextNodeModels.Any()) return;
            m_ContextNodeModel = contextNodeModels.First();
            m_Stencil = (ShaderGraphStencil)m_ContextNodeModel.GraphModel.Stencil;

            var typeNames = m_AvailableTypes.Select(GetTypeDisplayName).ToList();
            var controller = new SubgraphOutputListViewController();
            controller.beforeItemsRemoved += indices =>
            {
                Debug.Assert(indices.Count == 1, "UNIMPLEMENTED: Remove multiple subgraph outputs");
                RootView.Dispatch(new RemoveContextEntryCommand(m_ContextNodeModel, m_OutputRows[indices[0]].Name));
            };

            m_OutputRowListField = new ListPropertyField(
                RootView,
                m_OutputRows,
                controller: controller,
                makeItem: () =>
                {
                    var ve = new VisualElement();
                    GraphElementHelper.LoadTemplateAndStylesheet(ve, "SubgraphOutputRow", "sg-subgraph-output-row");
                    ve.Q<DropdownField>().choices = typeNames;
                    return ve;
                },
                bindItem: (ve, i) =>
                {
                    var textField = ve.Q<TextField>();
                    textField.SetValueWithoutNotify(m_OutputRows[i].Name);
                    textField.isDelayed = true;
                    textField.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.newValue == evt.previousValue) return;
                        var uniqueName = GetUniqueOutputName(evt.newValue);
                        RootView.Dispatch(new RenameContextEntryCommand(m_ContextNodeModel, m_OutputRows[i].Name, uniqueName));
                    });

                    var dropdownField = ve.Q<DropdownField>();
                    dropdownField.SetValueWithoutNotify(dropdownField.choices[m_OutputRows[i].TypeIndex]);
                    dropdownField.RegisterValueChangedCallback(_ => // Event gives us a string, not the index
                    {
                        RootView.Dispatch(new ChangeContextEntryTypeCommand(m_ContextNodeModel, m_OutputRows[i].Name, m_AvailableTypes[dropdownField.index]));
                    });
                }
            );

            m_OutputRowListField.listView.itemsAdded += _ => // SGListViewController doesn't supply indices
            {
                var entryName = GetUniqueOutputName("New");
                RootView.Dispatch(new AddContextEntryCommand(m_ContextNodeModel, entryName, TypeHandle.Float));
            };

            m_OutputRowListField.listView.name = "sg-subgraph-output-list";
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

            if (!m_ContextNodeModel.TryGetNodeHandler(out var nodeHandler)) return proposedName;

            var existingNames = nodeHandler.GetPorts().Select(p => p.ID.LocalPath);
            return ObjectNames.GetUniqueName(existingNames.ToArray(), proposedName);
        }

        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            yield return new LabelPropertyField("Inputs", RootView);
            yield return m_OutputRowListField;
        }

        protected override void UpdatePartFromModel()
        {
            base.UpdatePartFromModel();

            if (!m_ContextNodeModel.TryGetNodeHandler(out var nodeHandler)) return;

            m_OutputRows.Clear();
            foreach (var portHandler in nodeHandler.GetPorts())
            {
                if (!portHandler.IsHorizontal || !portHandler.IsInput)
                {
                    continue;
                }

                m_OutputRows.Add(new SubgraphOutputRow
                {
                    Name = portHandler.ID.LocalPath,
                    TypeIndex = Array.IndexOf(m_AvailableTypes, ShaderGraphExampleTypes.GetGraphType(portHandler)),
                });
            }

            m_OutputRowListField.listView.itemsSource = m_OutputRows;
            m_OutputRowListField.listView.Rebuild();
        }

        public override bool IsEmpty() => false;
    }
}
