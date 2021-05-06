using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using GraphDataStore = UnityEditor.ShaderGraph.DataStore<UnityEditor.ShaderGraph.GraphData>;
using BlackboardItem = UnityEditor.ShaderGraph.Internal.ShaderInput;
using BlackboardItemController = UnityEditor.ShaderGraph.Drawing.ShaderInputViewController;

namespace UnityEditor.ShaderGraph.Drawing
{
    class MoveShaderInputAction : IGraphDataAction
    {
        void MoveShaderInput(GraphData graphData)
        {
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out MoveShaderInputAction");
            AssertHelpers.IsNotNull(shaderInputReference, "ShaderInputReference is null while carrying out MoveShaderInputAction");
            graphData.owner.RegisterCompleteObjectUndo("Move Graph Input");
            switch (shaderInputReference)
            {
                case AbstractShaderProperty property:
                    graphData.MoveProperty(property, newIndexValue);
                    break;
                case ShaderKeyword keyword:
                    graphData.MoveKeyword(keyword, newIndexValue);
                    break;
                case ShaderDropdown dropdown:
                    graphData.MoveDropdown(dropdown, newIndexValue);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Action<GraphData> modifyGraphDataAction =>  MoveShaderInput;

        // Reference to the shader input being modified
        internal ShaderInput shaderInputReference { get; set; }

        internal int newIndexValue { get; set; }
    }

    class BlackboardSectionController : SGViewController<GraphData, BlackboardSectionViewModel>
    {
        internal SGBlackboardSection BlackboardSectionView => m_BlackboardSectionView;

        SGBlackboardSection m_BlackboardSectionView;

        // Reference to the category data this controller is responsible for representing
        CategoryData m_CategoryDataReference = null;

        Dictionary<Guid, BlackboardItemController> m_BlackboardItemControllers = new Dictionary<Guid, ShaderInputViewController>();

        SGBlackboard blackboard { get; set; }


        internal BlackboardSectionController(GraphData graphData, BlackboardSectionViewModel sectionViewModel, GraphDataStore dataStore)
            : base(graphData, sectionViewModel, dataStore)
        {
            m_BlackboardSectionView = new SGBlackboardSection(sectionViewModel);

            blackboard = sectionViewModel.parentView as SGBlackboard;
            if (blackboard == null)
                return;

            blackboard.Add(m_BlackboardSectionView);
            // These make sure that the drag indicators are disabled whenever a drag action is cancelled without completing a drop
            blackboard.RegisterCallback<MouseUpEvent>(evt =>
            {
                m_BlackboardSectionView.OnDragActionCanceled();
            });
            blackboard.hideDragIndicatorAction += m_BlackboardSectionView.OnDragActionCanceled;

            // Go through categories in Data Store
            foreach (var categoryData in graphData.categories)
            {
                // If category can be found with matching guid for this section
                // And that category contains this input
                if (categoryData.categoryGuid == ViewModel.associatedCategoryGuid)
                {
                    m_CategoryDataReference = categoryData;
                    break;
                }
            }

            foreach (var shaderInput in graphData.properties)
            {
                if (IsInputInSection(shaderInput))
                    InsertBlackboardRow(shaderInput);
            }

            foreach (var shaderInput in graphData.keywords)
            {
                if (IsInputInSection(shaderInput))
                    InsertBlackboardRow(shaderInput);
            }

            foreach (var shaderInput in graphData.dropdowns)
            {
                if (IsInputInSection(shaderInput))
                    InsertBlackboardRow(shaderInput);
            }
        }

        protected override void RequestModelChange(IGraphDataAction changeAction)
        {
            DataStore.Dispatch(changeAction);
        }

        // Called by GraphDataStore.Subscribe after the model has been changed
        protected override void ModelChanged(GraphData graphData, IGraphDataAction changeAction)
        {
            switch (changeAction)
            {
                // If newly added input doesn't belong to any of the sections, add it to the appropriate default section
                case AddShaderInputAction addBlackboardItemAction:
                    if (IsInputInSection(addBlackboardItemAction.shaderInputReference))
                    {
                        var blackboardRow = InsertBlackboardRow(addBlackboardItemAction.shaderInputReference);

                        // Rows should auto-expand when an input is first added
                        // blackboardRow.expanded = true;

                        var propertyView = blackboardRow.Q<BlackboardPropertyView>();
                        if (addBlackboardItemAction.addInputActionType == AddShaderInputAction.AddActionSource.AddMenu)
                            propertyView.OpenTextEditor();
                    }
                    break;
                case DeleteShaderInputAction deleteShaderInputAction:
                    foreach (var shaderInput in deleteShaderInputAction.shaderInputsToDelete)
                    {
                        if (IsInputInSection(shaderInput))
                            RemoveBlackboardRow(shaderInput);
                    }
                    break;
                case HandleUndoRedoAction handleUndoRedoAction:
                    foreach (var shaderInput in graphData.removedInputs)
                        if (IsInputInSection(shaderInput))
                            RemoveBlackboardRow(shaderInput);

                    foreach (var shaderInput in graphData.addedInputs)
                        if (IsInputInSection(shaderInput))
                            InsertBlackboardRow(shaderInput);
                    break;
                case CopyShaderInputAction copyShaderInputAction:
                    if (IsInputInSection(copyShaderInputAction.copiedShaderInput))
                        InsertBlackboardRow(copyShaderInputAction.copiedShaderInput, copyShaderInputAction.insertIndex);
                    break;
            }
        }

        internal bool IsInputInSection(ShaderInput shaderInput)
        {
            return m_CategoryDataReference != null && m_CategoryDataReference.childItemIDSet.Contains(shaderInput.guid);
        }

        internal SGBlackboardRow FindBlackboardRow(ShaderInput shaderInput)
        {
            m_BlackboardItemControllers.TryGetValue(shaderInput.guid, out var associatedController);
            return associatedController?.BlackboardItemView;
        }

        // Creates controller, view and view model for a blackboard item and adds the view to the specified index in the section
        // By default adds it to the end of the list if no insertionIndex specified
        internal SGBlackboardRow InsertBlackboardRow(BlackboardItem shaderInput, int insertionIndex = -1)
        {
            // If no index specified, add to end of section
            if (insertionIndex == -1)
                insertionIndex = m_BlackboardItemControllers.Count;

            var shaderInputViewModel = new ShaderInputViewModel()
            {
                model = shaderInput,
                parentView = BlackboardSectionView,
            };
            var blackboardItemController = new BlackboardItemController(shaderInput, shaderInputViewModel, DataStore);
            m_BlackboardItemControllers.Add(shaderInput.guid, blackboardItemController);

            BlackboardSectionView.Insert(insertionIndex, blackboardItemController.BlackboardItemView);

            return blackboardItemController.BlackboardItemView;
        }

        internal void RemoveBlackboardRow(BlackboardItem shaderInput)
        {
            BlackboardItemController associatedBlackboardItemController = null;
            m_BlackboardItemControllers.TryGetValue(shaderInput.guid, out associatedBlackboardItemController);

            if (associatedBlackboardItemController != null)
            {
                associatedBlackboardItemController.BlackboardItemView.RemoveFromHierarchy();
                m_BlackboardItemControllers.Remove(shaderInput.guid);
            }
            else
            {
                Debug.Log("ERROR: Failed to find associated blackboard item controller for shader input that was just deleted. Cannot clean up view associated with input.");
            }
        }
    }
}
