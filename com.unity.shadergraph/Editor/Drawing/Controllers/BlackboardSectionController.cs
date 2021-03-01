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
            Assert.IsNotNull(graphData, "GraphData is null while carrying out MoveShaderInputAction");
            Assert.IsNotNull(ShaderInputReference, "ShaderInputReference is null while carrying out MoveShaderInputAction");
            graphData.owner.RegisterCompleteObjectUndo("Move Graph Input");
            switch (ShaderInputReference)
            {
                case AbstractShaderProperty property:
                    graphData.MoveProperty(property, NewIndexValue);
                    break;
                case ShaderKeyword keyword:
                    graphData.MoveKeyword(keyword, NewIndexValue);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Action<GraphData> modifyGraphDataAction =>  MoveShaderInput;

        // Reference to the shader input being modified
        internal ShaderInput ShaderInputReference { get; set; }

        internal int NewIndexValue { get; set; }
    }

    class BlackboardSectionController : SGViewController<GraphData, BlackboardSectionViewModel>
    {
        internal SGBlackboardSection BlackboardSectionView => m_BlackboardSectionView;

        SGBlackboardSection m_BlackboardSectionView;

        IList<BlackboardItemController> m_BlackboardItemControllers = new List<BlackboardItemController>();

        internal bool controlsDefaultSection { get; set; }

        SGBlackboard blackboard { get; set; }

        internal BlackboardSectionController(GraphData graphData, BlackboardSectionViewModel sectionViewModel, GraphDataStore dataStore)
            : base(graphData, sectionViewModel, dataStore)
        {
            m_BlackboardSectionView = new SGBlackboardSection(sectionViewModel);

            blackboard = sectionViewModel.parentView as SGBlackboard;

            foreach (var shaderInput in DataStore.State.properties)
            {
                if (IsInputInSection(shaderInput))
                    AddBlackboardRow(shaderInput);
            }

            foreach (var shaderInput in DataStore.State.keywords)
            {
                if (IsInputInSection(shaderInput))
                    AddBlackboardRow(shaderInput);
            }
        }

        protected override void RequestModelChange(IGraphDataAction changeAction)
        {
            DataStore.Dispatch(changeAction);
        }

        // Called by GraphDataStore.Subscribe after the model has been changed
        protected override void ModelChanged(GraphData graphData, IGraphDataAction changeAction)
        {
            if (changeAction is AddShaderInputAction addBlackboardItemAction)
            {
                if (IsInputInSection(addBlackboardItemAction.shaderInputReference))
                {
                    var blackboardRow = AddBlackboardRow(addBlackboardItemAction.shaderInputReference);
                    // Rows should auto-expand when an input is first added
                    // blackboardRow.expanded = true;

                    var propertyView = blackboardRow.Q<BlackboardPropertyView>();
                    if(addBlackboardItemAction.addInputActionType == AddShaderInputAction.AddActionSource.AddMenu)
                        propertyView.OpenTextEditor();
                }
            }
            else if (changeAction is DeleteShaderInputAction deleteShaderInputAction)
            {
                foreach (var shaderInput in deleteShaderInputAction.shaderInputsToDelete)
                {
                    if (IsInputInSection(shaderInput))
                        RemoveBlackboardRow(shaderInput);
                }
            }
            else if (changeAction is HandleUndoRedoAction handleUndoRedoAction)
            {
                foreach (var shaderInput in graphData.removedInputs)
                    if (IsInputInSection(shaderInput))
                        RemoveBlackboardRow(shaderInput);

                foreach (var shaderInput in graphData.addedInputs)
                    if (IsInputInSection(shaderInput))
                        AddBlackboardRow(shaderInput);
            }
            else if (changeAction is CopyShaderInputAction copyShaderInputAction)
            {
                if (IsInputInSection(copyShaderInputAction.copiedShaderInput))
                    InsertBlackboardRow(copyShaderInputAction.copiedShaderInput, copyShaderInputAction.insertIndex);
            }
        }

        internal bool IsInputInSection(ShaderInput shaderInput)
        {
            // Go through categories in Data Store
            foreach (var categoryData in DataStore.State.categories)
            {
                // If category can be found with matching guid for this section
                // And that category contains this input
                if (categoryData.categoryGuid == ViewModel.associatedCategoryGuid
                    && categoryData.childItemIDList.Contains(shaderInput.guid))
                    return true;
            }

            return false;
        }

        // Creates controller, view and view model for a blackboard item and adds the view to the end of the section
        internal SGBlackboardRow AddBlackboardRow(BlackboardItem shaderInput)
        {
            var shaderInputViewModel = new ShaderInputViewModel()
            {
                Model = shaderInput,
                parentView = BlackboardSectionView,
                updateSelectionStateAction = ViewModel.updateSelectionStateAction
            };

            var blackboardItemController = new BlackboardItemController(shaderInput, shaderInputViewModel, DataStore);
            m_BlackboardItemControllers.Add(blackboardItemController);

            BlackboardSectionView.Add(blackboardItemController.BlackboardItemView);

            return blackboardItemController.BlackboardItemView;
        }

        // Creates controller, view and view model for a blackboard item and adds the view to the specified index in the section
        // By default adds it to the end of the list if no insertionIndex specified
        internal SGBlackboardRow InsertBlackboardRow(BlackboardItem shaderInput, int insertionIndex = -1)
        {
            // If no index specified, add to end of section
            if (insertionIndex == -1)
                return AddBlackboardRow(shaderInput);

            var shaderInputViewModel = new ShaderInputViewModel()
            {
                Model = shaderInput,
                parentView = BlackboardSectionView,
                updateSelectionStateAction = ViewModel.updateSelectionStateAction
            };
            var blackboardItemController = new BlackboardItemController(shaderInput, shaderInputViewModel, DataStore);
            m_BlackboardItemControllers.Insert(insertionIndex, blackboardItemController);

            BlackboardSectionView.Insert(insertionIndex, blackboardItemController.BlackboardItemView);

            return blackboardItemController.BlackboardItemView;
        }

        internal void RemoveBlackboardRow(BlackboardItem shaderInput)
        {
            BlackboardItemController associatedBlackboardItemController = null;
            foreach (var blackboardItemController in m_BlackboardItemControllers)
            {
                if (blackboardItemController.Model == shaderInput)
                    associatedBlackboardItemController = blackboardItemController;
            }

            try
            {
                Assert.IsNotNull(associatedBlackboardItemController);
            }
            catch (NullReferenceException e)
            {
                Debug.Log("ERROR: Failed to find associated blackboard item controller for shader input that was just deleted. Cannot clean up view associated with input. " + e);
                return;
            }

            BlackboardSectionView.Remove(associatedBlackboardItemController.BlackboardItemView);

            m_BlackboardItemControllers.Remove(associatedBlackboardItemController);
        }
    }
}
