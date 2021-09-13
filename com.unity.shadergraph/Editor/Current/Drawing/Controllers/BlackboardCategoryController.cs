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
            graphData.MoveItemInCategory(shaderInputReference, newIndexValue, associatedCategoryGuid);
        }

        public Action<GraphData> modifyGraphDataAction => MoveShaderInput;

        internal string associatedCategoryGuid { get; set; }

        // Reference to the shader input being modified
        internal ShaderInput shaderInputReference { get; set; }

        internal int newIndexValue { get; set; }
    }

    class DeleteCategoryAction : IGraphDataAction
    {
        void RemoveCategory(GraphData graphData)
        {
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out DeleteCategoryAction");
            AssertHelpers.IsNotNull(categoriesToRemoveGuids, "CategoryToRemove is null while carrying out DeleteCategoryAction");

            // This is called by MaterialGraphView currently, no need to repeat it here, though ideally it would live here
            //graphData.owner.RegisterCompleteObjectUndo("Delete Category");

            foreach (var categoryGUID in categoriesToRemoveGuids)
            {
                graphData.RemoveCategory(categoryGUID);
            }
        }

        public Action<GraphData> modifyGraphDataAction => RemoveCategory;

        // Reference to the guid(s) of categories being deleted
        public HashSet<string> categoriesToRemoveGuids { get; set; } = new HashSet<string>();
    }

    class ChangeCategoryIsExpandedAction : IGraphDataAction
    {
        internal const string kEditorPrefKey = ".isCategoryExpanded";

        void ChangeIsExpanded(GraphData graphData)
        {
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out ChangeIsExpanded on Category");
            foreach (var catid in categoryGuids)
            {
                var key = $"{editorPrefsBaseKey}.{catid}.{kEditorPrefKey}";
                var currentValue = EditorPrefs.GetBool(key, true);

                if (currentValue != isExpanded)
                {
                    EditorPrefs.SetBool(key, isExpanded);
                }
            }
        }

        public string editorPrefsBaseKey;
        public List<string> categoryGuids { get; set; }
        public bool isExpanded { get; set; }

        public Action<GraphData> modifyGraphDataAction => ChangeIsExpanded;
    }

    class ChangeCategoryNameAction : IGraphDataAction
    {
        void ChangeCategoryName(GraphData graphData)
        {
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out ChangeCategoryNameAction");
            graphData.owner.RegisterCompleteObjectUndo("Change Category Name");
            graphData.ChangeCategoryName(categoryGuid, newCategoryNameValue);
        }

        public Action<GraphData> modifyGraphDataAction => ChangeCategoryName;

        // Guid of the category being modified
        public string categoryGuid { get; set; }

        internal string newCategoryNameValue { get; set; }
    }

    class BlackboardCategoryController : SGViewController<CategoryData, BlackboardCategoryViewModel>
    {
        internal SGBlackboardCategory blackboardCategoryView => m_BlackboardCategoryView;
        SGBlackboardCategory m_BlackboardCategoryView;
        Dictionary<string, BlackboardItemController> m_BlackboardItemControllers = new Dictionary<string, ShaderInputViewController>();
        SGBlackboard blackboard { get; set; }

        internal BlackboardCategoryController(CategoryData categoryData, BlackboardCategoryViewModel categoryViewModel, GraphDataStore dataStore)
            : base(categoryData, categoryViewModel, dataStore)
        {
            m_BlackboardCategoryView = new SGBlackboardCategory(categoryViewModel, this);
            blackboard = categoryViewModel.parentView as SGBlackboard;
            if (blackboard == null)
                return;

            blackboard.Add(m_BlackboardCategoryView);
            // These make sure that the drag indicators are disabled whenever a drag action is cancelled without completing a drop
            blackboard.RegisterCallback<MouseUpEvent>(evt =>
            {
                m_BlackboardCategoryView.OnDragActionCanceled();
            });
            blackboard.hideDragIndicatorAction += m_BlackboardCategoryView.OnDragActionCanceled;

            foreach (var categoryItem in categoryData.Children)
            {
                if (categoryItem == null)
                {
                    AssertHelpers.Fail("Failed to insert blackboard row into category due to shader input being null.");
                    continue;
                }
                InsertBlackboardRow(categoryItem);
            }
        }

        protected override void RequestModelChange(IGraphDataAction changeAction)
        {
            DataStore.Dispatch(changeAction);
        }

        // Called by GraphDataStore.Subscribe after the model has been changed
        protected override void ModelChanged(GraphData graphData, IGraphDataAction changeAction)
        {
            // If categoryData associated with this controller is removed by an operation, destroy controller and views associated
            if (graphData.ContainsCategory(Model) == false)
            {
                this.Destroy();
                return;
            }

            switch (changeAction)
            {
                case AddShaderInputAction addBlackboardItemAction:
                    if (addBlackboardItemAction.shaderInputReference != null && IsInputInCategory(addBlackboardItemAction.shaderInputReference))
                    {
                        var blackboardRow = FindBlackboardRow(addBlackboardItemAction.shaderInputReference);
                        if (blackboardRow == null)
                            blackboardRow = InsertBlackboardRow(addBlackboardItemAction.shaderInputReference);
                        // Rows should auto-expand when an input is first added
                        // blackboardRow.expanded = true;
                        var propertyView = blackboardRow.Q<SGBlackboardField>();
                        if (addBlackboardItemAction.addInputActionType == AddShaderInputAction.AddActionSource.AddMenu)
                            propertyView.OpenTextEditor();
                    }
                    break;

                case CopyShaderInputAction copyShaderInputAction:
                    // In the specific case of only-one keywords like Material Quality and Raytracing, they can get copied, but because only one can exist, the output copied value is null
                    if (copyShaderInputAction.copiedShaderInput != null && IsInputInCategory(copyShaderInputAction.copiedShaderInput))
                    {
                        var blackboardRow = InsertBlackboardRow(copyShaderInputAction.copiedShaderInput, copyShaderInputAction.insertIndex);
                        if (blackboardRow != null)
                        {
                            var graphView = ViewModel.parentView.GetFirstAncestorOfType<MaterialGraphView>();
                            var propertyView = blackboardRow.Q<SGBlackboardField>();
                            graphView?.AddToSelectionNoUndoRecord(propertyView);
                        }
                    }
                    break;

                case AddItemToCategoryAction addItemToCategoryAction:
                    // If item was added to category that this controller manages, then add blackboard row to represent that item
                    if (addItemToCategoryAction.itemToAdd != null && addItemToCategoryAction.categoryGuid == ViewModel.associatedCategoryGuid)
                    {
                        InsertBlackboardRow(addItemToCategoryAction.itemToAdd, addItemToCategoryAction.indexToAddItemAt);
                    }
                    else
                    {
                        // If the added input has been added to a category other than this one, and it used to belong to this category,
                        // Then cleanup the controller and view that used to represent that input
                        foreach (var key in m_BlackboardItemControllers.Keys)
                        {
                            var blackboardItemController = m_BlackboardItemControllers[key];
                            if (blackboardItemController.Model == addItemToCategoryAction.itemToAdd)
                            {
                                RemoveBlackboardRow(addItemToCategoryAction.itemToAdd);
                                break;
                            }
                        }
                    }
                    break;

                case DeleteCategoryAction deleteCategoryAction:
                    if (deleteCategoryAction.categoriesToRemoveGuids.Contains(ViewModel.associatedCategoryGuid))
                    {
                        this.Destroy();
                        return;
                    }

                    break;

                case ChangeCategoryIsExpandedAction changeIsExpandedAction:
                    if (changeIsExpandedAction.categoryGuids.Contains(ViewModel.associatedCategoryGuid))
                    {
                        ViewModel.isExpanded = changeIsExpandedAction.isExpanded;
                        m_BlackboardCategoryView.TryDoFoldout(changeIsExpandedAction.isExpanded);
                    }
                    break;

                case ChangeCategoryNameAction changeCategoryNameAction:
                    if (changeCategoryNameAction.categoryGuid == ViewModel.associatedCategoryGuid)
                    {
                        ViewModel.name = Model.name;
                        m_BlackboardCategoryView.title = ViewModel.name;
                    }
                    break;
            }
        }

        internal bool IsInputInCategory(ShaderInput shaderInput)
        {
            return Model != null && Model.IsItemInCategory(shaderInput);
        }

        internal SGBlackboardRow FindBlackboardRow(ShaderInput shaderInput)
        {
            m_BlackboardItemControllers.TryGetValue(shaderInput.objectId, out var associatedController);
            return associatedController?.BlackboardItemView;
        }

        // Creates controller, view and view model for a blackboard item and adds the view to the specified index in the category
        // By default adds it to the end of the list if no insertionIndex specified
        internal SGBlackboardRow InsertBlackboardRow(BlackboardItem shaderInput, int insertionIndex = -1)
        {
            var shaderInputViewModel = new ShaderInputViewModel()
            {
                model = shaderInput,
                parentView = blackboardCategoryView,
            };
            var blackboardItemController = new BlackboardItemController(shaderInput, shaderInputViewModel, DataStore);

            m_BlackboardItemControllers.TryGetValue(shaderInput.objectId, out var existingItemController);
            if (existingItemController == null)
            {
                m_BlackboardItemControllers.Add(shaderInput.objectId, blackboardItemController);
                // If no index specified, add to end of category
                if (insertionIndex == -1)
                    blackboardCategoryView.Add(blackboardItemController.BlackboardItemView);
                else
                    blackboardCategoryView.Insert(insertionIndex, blackboardItemController.BlackboardItemView);

                blackboardCategoryView.MarkDirtyRepaint();

                return blackboardItemController.BlackboardItemView;
            }
            else
            {
                AssertHelpers.Fail("Tried to add blackboard item that already exists to category.");
                return null;
            }
        }

        internal void RemoveBlackboardRow(BlackboardItem shaderInput)
        {
            m_BlackboardItemControllers.TryGetValue(shaderInput.objectId, out var associatedBlackboardItemController);
            if (associatedBlackboardItemController != null)
            {
                associatedBlackboardItemController.Destroy();
                m_BlackboardItemControllers.Remove(shaderInput.objectId);
            }
            else
                AssertHelpers.Fail("Failed to find associated blackboard item controller for shader input that was just deleted. Cannot clean up view associated with input.");
        }

        void ClearBlackboardRows()
        {
            foreach (var shaderInputViewController in m_BlackboardItemControllers.Values)
                shaderInputViewController.Destroy();

            m_BlackboardItemControllers.Clear();
        }

        public override void Destroy()
        {
            Cleanup();
            m_BlackboardCategoryView?.RemoveFromHierarchy();
            ClearBlackboardRows();
        }
    }
}
