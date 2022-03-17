using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A view to display the model inspector.
    /// </summary>
    public class ModelInspectorView : RootView
    {
        /// <summary>
        /// Determines if a field should be displayed in the basic settings section of the inspector.
        /// </summary>
        /// <param name="f">The field to inspect.</param>
        /// <returns>True if a field should be displayed in the basic settings section of the inspector. False otherwise.</returns>
        public static bool BasicSettingsFilter(FieldInfo f)
        {
            return SerializedFieldsInspector.CanBeInspected(f) && f.CustomAttributes.Any(a => a.AttributeType == typeof(ModelSettingAttribute));
        }

        /// <summary>
        /// Determines if a field should be displayed in the advanced settings section of the inspector.
        /// </summary>
        /// <param name="f">The field to inspect.</param>
        /// <returns>True if a field should be displayed in the advanced settings section of the inspector. False otherwise.</returns>
        public static bool AdvancedSettingsFilter(FieldInfo f)
        {
            return SerializedFieldsInspector.CanBeInspected(f) && f.CustomAttributes.All(a => a.AttributeType != typeof(ModelSettingAttribute));
        }

        static readonly List<ModelView> k_UpdateAllUIs = new List<ModelView>();

        public new static readonly string ussClassName = "model-inspector-view";
        public static readonly string titleUssClassName = ussClassName.WithUssElement("title");
        public static readonly string containerUssClassName = ussClassName.WithUssElement("container");

        public static readonly string firstChildUssClassName = "first-child";

        Label m_Title;
        VisualElement m_InspectorContainer;

        ModelViewUpdater m_UpdateObserver;
        InspectorSelectionObserver m_SelectionObserver;

        public ModelInspectorViewModel ModelInspectorViewModel => (ModelInspectorViewModel)Model;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelInspectorView"/> class.
        /// </summary>
        /// <param name="window">The <see cref="GraphViewEditorWindow"/> associated with this view.</param>
        public ModelInspectorView(GraphViewEditorWindow window)
        : base(window, window.GraphTool)
        {
            if (window.GraphView != null)
            {
                Model = new ModelInspectorViewModel(window.GraphView);

                m_UpdateObserver = new ModelViewUpdater(this, ModelInspectorViewModel.ModelInspectorState, ModelInspectorViewModel.GraphModelState);

                GraphTool.State.OnStateComponentListModified += UpdateSelectionObserver;

                Dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SetInspectedGraphModelFieldCommand>(SetInspectedGraphModelFieldCommand.DefaultCommandHandler, GraphTool.UndoStateComponent, ModelInspectorViewModel.GraphModelState);

                Dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, SetInspectedGraphElementModelFieldCommand>(SetInspectedGraphElementModelFieldCommand.DefaultCommandHandler, GraphTool.UndoStateComponent, ModelInspectorViewModel.GraphModelState);

                Dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, UpdateConstantValueCommand>(UpdateConstantValueCommand.DefaultCommandHandler, GraphTool.UndoStateComponent, ModelInspectorViewModel.GraphModelState);

                Dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, ExposeVariableCommand>(ExposeVariableCommand.DefaultCommandHandler, GraphTool.UndoStateComponent, ModelInspectorViewModel.GraphModelState);
                Dispatcher.RegisterCommandHandler<UndoStateComponent, GraphModelStateComponent, UpdateTooltipCommand>(UpdateTooltipCommand.DefaultCommandHandler, GraphTool.UndoStateComponent, ModelInspectorViewModel.GraphModelState);

                Dispatcher.RegisterCommandHandler<ModelInspectorStateComponent, CollapseInspectorSectionCommand>(
                    CollapseInspectorSectionCommand.DefaultCommandHandler, ModelInspectorViewModel.ModelInspectorState);
            }

            this.AddStylesheet("ModelInspector.uss");
            AddToClassList(ussClassName);

            m_Title = new Label();
            m_Title.AddToClassList(titleUssClassName);
            Add(m_Title);

            m_InspectorContainer = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            m_InspectorContainer.AddToClassList(containerUssClassName);
            Add(m_InspectorContainer);
        }

        void UpdateSelectionObserver(IState state, IStateComponent stateComponent)
        {
            if (stateComponent is SelectionStateComponent)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_SelectionObserver);
                m_SelectionObserver = null;

                BuildSelectionObserver();
            }
        }

        void BuildSelectionObserver()
        {
            if (GraphTool != null)
            {
                var selectionStates = GraphTool.State.AllStateComponents.OfType<SelectionStateComponent>();
                m_SelectionObserver = new InspectorSelectionObserver(GraphTool.ToolState, ModelInspectorViewModel.GraphModelState,
                    selectionStates.ToList(), ModelInspectorViewModel.ModelInspectorState);

                GraphTool.ObserverManager?.RegisterObserver(m_SelectionObserver);
            }
        }

        /// <inheritdoc />
        protected override void RegisterObservers()
        {
            GraphTool?.ObserverManager?.RegisterObserver(m_UpdateObserver);
            BuildSelectionObserver();
        }

        /// <inheritdoc />
        protected override void UnregisterObservers()
        {
            GraphTool?.ObserverManager?.UnregisterObserver(m_UpdateObserver);

            GraphTool?.ObserverManager?.UnregisterObserver(m_SelectionObserver);
            m_SelectionObserver = null;
        }

        void RemoveAllUI()
        {
            var elements = m_InspectorContainer.Query<ModelView>().ToList();
            foreach (var element in elements)
            {
                element.RemoveFromRootView();
            }

            m_InspectorContainer.Clear();
        }

        /// <inheritdoc />
        public override void BuildUI()
        {
            RemoveAllUI();

            if (ModelInspectorViewModel == null)
                return;

            var inspectorModel = ModelInspectorViewModel.ModelInspectorState.GetInspectorModel();
            if (inspectorModel != null)
            {
                m_Title.text = inspectorModel.Title;

                bool isFirst = true;
                ModelView sectionUI = null;
                foreach (var section in inspectorModel.Sections)
                {
                    var context = new InspectorSectionContext(section);

                    // Create the inspector, with all the field editors.
                    var sectionInspector = ModelViewFactory.CreateUI<ModelView>(this,
                        ModelInspectorViewModel.ModelInspectorState.InspectedModels.FirstOrDefault(), context);

                    // If the sectionInspector is empty, do not show it and do not create a section for it.
                    if (sectionInspector == null || (sectionInspector is ModelInspector modelInspector && modelInspector.IsEmpty()))
                        continue;

                    // Create a section to wrap the sectionInspector. This could be a collapsible section, for example.
                    sectionUI = ModelViewFactory.CreateUI<ModelView>(this, section);
                    if (sectionUI != null)
                    {
                        if (isFirst)
                        {
                            // The firstChild class is useful to style the first element differently,
                            // for example to avoid a double border or a missing border between sibling elements.
                            sectionUI.AddToClassList(firstChildUssClassName);
                            isFirst = false;
                        }

                        // Add the section to the view, in the side panel, and add the sectionInspector to the section.
                        sectionUI.AddToRootView(this);
                        m_InspectorContainer.Add(sectionUI);
                        sectionUI.Add(sectionInspector);
                    }
                    else
                    {
                        // If there was no section created, let's put the sectionInspector directly in the side panel.
                        m_InspectorContainer.Add(sectionInspector);
                    }

                    sectionInspector.AddToRootView(this);
                }

                sectionUI?.AddToClassList("last-child");
            }
            else if (ModelInspectorViewModel.ModelInspectorState.InspectedModels.Count > 1)
            {
                m_Title.text = "Multiple selection";
            }
        }

        public override void UpdateFromModel()
        {
            if (panel == null)
                return;

            using (var modelStateObservation = m_UpdateObserver.ObserveState(ModelInspectorViewModel.GraphModelState))
            using (var inspectorStateObservation = m_UpdateObserver.ObserveState(ModelInspectorViewModel.ModelInspectorState))
            {
                var rebuildType = inspectorStateObservation.UpdateType.Combine(modelStateObservation.UpdateType);

                if (rebuildType == UpdateType.Complete)
                {
                    BuildUI();
                }
                else
                {
                    if (modelStateObservation.UpdateType == UpdateType.Partial)
                    {
                        var changeset = ModelInspectorViewModel.GraphModelState.GetAggregatedChangeset(modelStateObservation.LastObservedVersion);
                        var inspectedModel = ModelInspectorViewModel.ModelInspectorState.InspectedModels.FirstOrDefault();

                        var inspectedModelChanged = false;
                        foreach (var changedModel in changeset.ChangedModels)
                        {
                            switch (changedModel)
                            {
                                case INodeModel _:
                                    inspectedModelChanged = ReferenceEquals(changedModel, inspectedModel);
                                    break;
                                case IPortModel portModel:
                                    inspectedModelChanged = ReferenceEquals(portModel.NodeModel, inspectedModel);
                                    break;
                                case IVariableDeclarationModel variableDeclarationModel:
                                    inspectedModelChanged = ReferenceEquals(variableDeclarationModel, inspectedModel);
                                    break;
                            }

                            if (inspectedModelChanged)
                                break;
                        }

                        if (inspectedModelChanged)
                        {
                            var inspectorModel = ModelInspectorViewModel.ModelInspectorState.GetInspectorModel();
                            if (inspectorModel != null)
                            {
                                m_Title.text = inspectorModel.Title;
                            }

                            inspectedModel.GetAllViews(this, null, k_UpdateAllUIs);
                            foreach (var ui in k_UpdateAllUIs)
                            {
                                ui.UpdateFromModel();
                            }
                        }

                        if (!k_UpdateAllUIs.Any() && (changeset.NewModels.Any() || changeset.DeletedModels.Any()))
                        {
                            inspectedModel.GetAllViews(this, null, k_UpdateAllUIs);

                            foreach (var ui in k_UpdateAllUIs)
                            {
                                ui.UpdateFromModel();
                            }
                        }

                        if (k_UpdateAllUIs.Any())
                            k_UpdateAllUIs.Clear();
                    }

                    if (inspectorStateObservation.UpdateType == UpdateType.Partial)
                    {
                        var inspectorModel = ModelInspectorViewModel.ModelInspectorState.GetInspectorModel();
                        if (inspectorModel != null)
                        {
                            m_Title.text = inspectorModel.Title;

                            var changeSet = ModelInspectorViewModel.ModelInspectorState.GetAggregatedChangeset(inspectorStateObservation.LastObservedVersion);
                            if (changeSet != null)
                            {
                                foreach (var sectionModel in changeSet.ChangedModels)
                                {
                                    sectionModel.GetAllViews(this, null, k_UpdateAllUIs);
                                    foreach (var ui in k_UpdateAllUIs)
                                    {
                                        ui.UpdateFromModel();
                                    }

                                    k_UpdateAllUIs.Clear();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
