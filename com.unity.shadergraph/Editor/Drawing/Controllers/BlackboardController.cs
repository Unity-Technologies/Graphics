using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Views;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Assertions;
using GraphDataStore = UnityEditor.ShaderGraph.DataStore<UnityEditor.ShaderGraph.GraphData>;
using BlackboardItem = UnityEditor.ShaderGraph.Internal.ShaderInput;

namespace UnityEditor.ShaderGraph.Drawing
{
    class AddShaderInputAction : IGraphDataAction
    {
        public enum AddActionSource
        {
            Default,
            AddMenu
        }


        void AddShaderInput(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out AddShaderInputAction");
            // If type property is valid, create instance of that type
            if (blackboardItemType != null && blackboardItemType.IsSubclassOf(typeof(BlackboardItem)))
                shaderInputReference = (BlackboardItem)Activator.CreateInstance(blackboardItemType, true);
            // If type is null a direct override object must have been provided or else we are in an error-state
            else if (shaderInputReference == null)
            {
                Debug.Log("ERROR: BlackboardController: Unable to complete Add Shader Input action.");
                return;
            }

            shaderInputReference.generatePropertyBlock = shaderInputReference.isExposable;

            graphData.owner.RegisterCompleteObjectUndo("Add Shader Input");
            graphData.AddGraphInput(shaderInputReference);
        }

        public Action<GraphData> modifyGraphDataAction => AddShaderInput;

        // If this is a subclass of ShaderInput and is not null, then an object of this type is created to add to blackboard
        public Type blackboardItemType { get; set; }

        // If the type field above is null and this is provided, then it is directly used as the item to add to blackboard
        public BlackboardItem shaderInputReference { get; set; }

        public AddActionSource addInputActionType { get; set; }
    }

    class ChangeGraphPathAction : IGraphDataAction
    {
        void ChangeGraphPath(GraphData graphData)
        {
            if (graphData != null)
            {
                graphData.path = NewGraphPath;
            }
        }

        public Action<GraphData> modifyGraphDataAction => ChangeGraphPath;

        public string NewGraphPath { get; set; }
    }

    class CopyShaderInputAction : IGraphDataAction
    {
        void CopyShaderInput(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out CopyShaderInputAction");
            Assert.IsNotNull(shaderInputToCopy, "ShaderInputToCopy is null while carrying out CopyShaderInputAction");
            switch (shaderInputToCopy)
            {
                case AbstractShaderProperty property:
                    var copiedProperty = (AbstractShaderProperty)graphData.AddCopyOfShaderInput(property, insertIndex);
                    if (copiedProperty != null) // some property types cannot be duplicated (unknown types)
                    {
                        // Update the property nodes that depends on the copied node
                        foreach (var node in dependentNodeList)
                        {
                            if (node is PropertyNode propertyNode)
                            {
                                propertyNode.owner = graphData;
                                propertyNode.property = copiedProperty;
                            }
                        }
                    }

                    copiedShaderInput = copiedProperty;
                    break;

                case ShaderKeyword shaderKeyword:
                    // Don't duplicate built-in keywords within the same graph
                    if (shaderKeyword.isBuiltIn && graphData.keywords.Any(p => p.referenceName == shaderInputToCopy.referenceName))
                        return;

                    var copiedKeyword = (ShaderKeyword)graphData.AddCopyOfShaderInput(shaderKeyword, insertIndex);

                    // Update the keyword nodes that depends on the copied node
                    foreach (var node in dependentNodeList)
                    {
                        if (node is KeywordNode propertyNode)
                        {
                            propertyNode.owner = graphData;
                            propertyNode.keyword = copiedKeyword;
                        }
                    }

                    copiedShaderInput = copiedKeyword;
                    break;

                default:
                    Debug.Log("ERROR: BlackboardController: Unable to complete Copy Shader Input action.");
                    return;
            }
        }

        public Action<GraphData> modifyGraphDataAction => CopyShaderInput;

        public IEnumerable<AbstractMaterialNode> dependentNodeList { get; set; } = new List<AbstractMaterialNode>();

        public BlackboardItem shaderInputToCopy { get; set; }

        public BlackboardItem copiedShaderInput { get; set; }

        public int insertIndex { get; set; } = -1;
    }

    class BlackboardController : SGViewController<GraphData, BlackboardViewModel>
    {
        // Type changes (adds/removes of Types) only happen after a full assmebly reload so its safe to make this stuff static (I think, confirm this assumption)
        static IList<Type> m_shaderInputTypes;

        static BlackboardController()
        {
            var shaderInputTypes = TypeCache.GetTypesWithAttribute<BlackboardInputInfo>().ToList();
            // Sort the ShaderInput by priority using the BlackboardInputInfo attribute
            shaderInputTypes.Sort((s1, s2) => {
                var info1 = Attribute.GetCustomAttribute(s1, typeof(BlackboardInputInfo)) as BlackboardInputInfo;
                var info2 = Attribute.GetCustomAttribute(s2, typeof(BlackboardInputInfo)) as BlackboardInputInfo;

                if (info1.priority == info2.priority)
                    return (info1.name ?? s1.Name).CompareTo(info2.name ?? s2.Name);
                else
                    return info1.priority.CompareTo(info2.priority);
            });

            m_shaderInputTypes = shaderInputTypes.ToList();
        }

        internal int k_PropertySectionIndex = 0;
        internal int k_KeywordSectionIndex = 1;

        BlackboardSectionController m_PropertySectionController;
        BlackboardSectionController m_KeywordSectionController;
        IList<BlackboardSectionController> m_BlackboardSectionControllers = new List<BlackboardSectionController>();

        SGBlackboard m_Blackboard;

        internal SGBlackboard blackboard
        {
            get => m_Blackboard;
            private set => m_Blackboard = value;
        }

        void InitializeViewModel()
        {
            // Clear the view model
            ViewModel.Reset();

            ViewModel.subtitle = BlackboardUtils.FormatPath(Model.path);

            // TODO: Could all this below data be static in the view model as well? Can't really see it ever changing at runtime
            // All of this stuff seems static and driven by attributes so it would seem to be compile-time defined, could definitely be an option
            // Only issue is checking for conflicts with user made keywords, could leave just that bit here and make everything else static

            // Property data first
            foreach (var shaderInputType in m_shaderInputTypes)
            {
                if (shaderInputType.IsAbstract)
                    continue;

                var info = Attribute.GetCustomAttribute(shaderInputType, typeof(BlackboardInputInfo)) as BlackboardInputInfo;
                string name = info?.name ?? ObjectNames.NicifyVariableName(shaderInputType.Name.Replace("ShaderProperty", ""));

                // QUICK FIX TO DEAL WITH DEPRECATED COLOR PROPERTY
                if (name.Equals("Color", StringComparison.InvariantCultureIgnoreCase) && ShaderGraphPreferences.allowDeprecatedBehaviors)
                    ViewModel.propertyNameToAddActionMap.Add("Color (Deprecated)", new AddShaderInputAction() { shaderInputReference = new ColorShaderProperty(ColorShaderProperty.deprecatedVersion), addInputActionType = AddShaderInputAction.AddActionSource.AddMenu});
                else
                    ViewModel.propertyNameToAddActionMap.Add(name, new AddShaderInputAction() { blackboardItemType = shaderInputType, addInputActionType = AddShaderInputAction.AddActionSource.AddMenu });
            }

            // Default Keywords next
            ViewModel.defaultKeywordNameToAddActionMap.Add("Boolean",  new AddShaderInputAction() { shaderInputReference = new ShaderKeyword(KeywordType.Boolean), addInputActionType = AddShaderInputAction.AddActionSource.AddMenu });
            ViewModel.defaultKeywordNameToAddActionMap.Add("Enum",  new AddShaderInputAction() { shaderInputReference = new ShaderKeyword(KeywordType.Enum), addInputActionType = AddShaderInputAction.AddActionSource.AddMenu });

            // Built-In Keywords last
            foreach (var builtinKeywordDescriptor in KeywordUtil.GetBuiltinKeywordDescriptors())
            {
                var keyword = ShaderKeyword.CreateBuiltInKeyword(builtinKeywordDescriptor);
                // Do not allow user to add built-in keywords that conflict with user-made keywords that have the same reference name
                if (Model.keywords.Any(x => x.referenceName == keyword.referenceName))
                {
                    ViewModel.disabledKeywordNameList.Add(keyword.displayName);
                }
                else
                {
                    ViewModel.builtInKeywordNameToAddActionMap.Add(keyword.displayName,  new AddShaderInputAction() { shaderInputReference = keyword.Copy(), addInputActionType = AddShaderInputAction.AddActionSource.AddMenu });
                }
            }

            ViewModel.requestModelChangeAction = this.RequestModelChange;

            ViewModel.categoryInfoList = DataStore.State.categories.ToList();

            // If no user-made categories exist, then create the default categories and add all inputs that exist to them
            if (ViewModel.categoryInfoList.Count == 0)
            {
                var propertyGUIDs = new List<Guid>();
                foreach (var property in DataStore.State.properties)
                    propertyGUIDs.Add(property.guid);
                var defaultPropertyCategory = new CategoryData("Properties", propertyGUIDs);
                ViewModel.categoryInfoList.Add(defaultPropertyCategory);

                var keywordGUIDs = new List<Guid>();
                foreach (var keyword in DataStore.State.keywords)
                    keywordGUIDs.Add(keyword.guid);
                var defaultKeywordCategory = new CategoryData("Keywords", keywordGUIDs);
                ViewModel.categoryInfoList.Add(defaultKeywordCategory);
            }
        }

        internal BlackboardController(GraphData model, BlackboardViewModel inViewModel, GraphDataStore graphDataStore)
            : base(model, inViewModel, graphDataStore)
        {
            InitializeViewModel();

            blackboard = new SGBlackboard(ViewModel);
            blackboard.controller = this;

            foreach (var categoryInfo in ViewModel.categoryInfoList)
            {
                var blackboardSectionViewModel = new BlackboardSectionViewModel();
                blackboardSectionViewModel.parentView = blackboard;
                blackboardSectionViewModel.requestModelChangeAction = ViewModel.requestModelChangeAction;
                blackboardSectionViewModel.updateSelectionStateAction = UpdateSelectionAfterUndoRedo;
                blackboardSectionViewModel.name = categoryInfo.name;
                blackboardSectionViewModel.associatedCategoryGuid = categoryInfo.categoryGuid;
                var blackboardSectionController = new BlackboardSectionController(model, blackboardSectionViewModel, graphDataStore);
                m_BlackboardSectionControllers.Add(blackboardSectionController);
            }

            m_PropertySectionController = m_BlackboardSectionControllers[0];
            m_KeywordSectionController = m_BlackboardSectionControllers[1];

            // The Blackboard Controller is responsible for handling the default categories/sections
            foreach (var shaderProperty in DataStore.State.properties)
                if (IsInputInDefaultCategory(shaderProperty))
                    AddInputToDefaultSection(shaderProperty);

            foreach (var shaderKeyword in DataStore.State.keywords)
                if (IsInputInDefaultCategory(shaderKeyword))
                    AddInputToDefaultSection(shaderKeyword);

            if (ViewModel.parentView is MaterialGraphView graphView)
            {
                graphView.blackboardItemRemovedDelegate += (removedInput) =>
                {
                    if (IsInputInDefaultCategory(removedInput))
                        RemoveInputFromDefaultSection(removedInput);
                };

                // Selection persistence
                graphView.OnSelectionChange += StoreSelection;
            }

            blackboard.PropertySection = m_PropertySectionController.BlackboardSectionView;
            blackboard.KeywordSection = m_KeywordSectionController.BlackboardSectionView;
            blackboard.contentContainer.Add(blackboard.PropertySection);
            blackboard.contentContainer.Add(blackboard.KeywordSection);
        }

        protected override void RequestModelChange(IGraphDataAction changeAction)
        {
            DataStore.Dispatch(changeAction);
        }

        // Called by GraphDataStore.Subscribe after the model has been changed
        protected override void ModelChanged(GraphData graphData, IGraphDataAction changeAction)
        {
            // Reconstruct view-model first
            // TODO: (would be cool to have some scoping here to see if the action was one that changed OUR UI or not, could avoid reconstructing the ViewModel based on that)
            InitializeViewModel();

            // If newly added input doesn't belong to any of the sections, add it to the appropriate default section
            if (changeAction is AddShaderInputAction addBlackboardItemAction)
            {
                var shaderInput = addBlackboardItemAction.shaderInputReference;

                if (IsInputInDefaultCategory(shaderInput))
                {
                    var blackboardRow = AddInputToDefaultSection(shaderInput);
                    // This selects the newly created property value without over-riding the undo stack in case user wants to undo
                    var graphView = ViewModel.parentView as MaterialGraphView;
                    graphView?.ClearSelectionNoUndoRecord();
                    var propertyView = blackboardRow.Q<BlackboardPropertyView>();
                    graphView?.AddToSelectionNoUndoRecord(propertyView);

                    if(addBlackboardItemAction.addInputActionType == AddShaderInputAction.AddActionSource.AddMenu)
                        propertyView.OpenTextEditor();
                }
            }
            else if (changeAction is DeleteShaderInputAction deleteShaderInputAction)
            {
                foreach (var shaderInput in deleteShaderInputAction.shaderInputsToDelete)
                {
                    if (IsInputInDefaultCategory(shaderInput))
                        RemoveInputFromDefaultSection(shaderInput);
                }
            }
            else if (changeAction is HandleUndoRedoAction handleUndoRedoAction)
            {
                // HandleUndo/Redo action is inherently unwieldy, should decompose to make it a series of add/remove operations
                foreach (var shaderInput in graphData.removedInputs)
                    if (IsInputInDefaultCategory(shaderInput))
                        RemoveInputFromDefaultSection(shaderInput);

                foreach (var shaderInput in graphData.addedInputs)
                    if (IsInputInDefaultCategory(shaderInput))
                        AddInputToDefaultSection(shaderInput);
            }
            else if (changeAction is CopyShaderInputAction copyShaderInputAction)
            {
                if (IsInputInDefaultCategory(copyShaderInputAction.copiedShaderInput))
                {
                    var blackboardRow = InsertInputInDefaultSection(copyShaderInputAction.copiedShaderInput, copyShaderInputAction.insertIndex);
                    // This selects the newly created property value without over-riding the undo stack in case user wants to undo
                    var graphView = ViewModel.parentView as MaterialGraphView;
                    graphView?.ClearSelectionNoUndoRecord();
                    var propertyView = blackboardRow.Q<BlackboardPropertyView>();
                    graphView?.AddToSelectionNoUndoRecord(propertyView);
                }
            }

            // Lets all event handlers this controller owns/manages know that the model has changed
            // Usually this is to update views and make them reconstruct themself from updated view-model
            NotifyChange(changeAction);

            // Let child controllers know about changes to this controller so they may update themselves in turn
            ApplyChanges();
        }

        SGBlackboardRow AddInputToDefaultSection(ShaderInput shaderInput)
        {
            switch (shaderInput)
            {
                case AbstractShaderProperty property:
                    return m_PropertySectionController.AddBlackboardRow(property);
                case ShaderKeyword keyword:
                    return m_KeywordSectionController.AddBlackboardRow(keyword);
            }

            return null;
        }

        SGBlackboardRow InsertInputInDefaultSection(ShaderInput shaderInput, int insertionIndex)
        {
            switch (shaderInput)
            {
                case AbstractShaderProperty property:
                    return m_PropertySectionController.InsertBlackboardRow(property, insertionIndex);
                case ShaderKeyword keyword:
                    return m_KeywordSectionController.InsertBlackboardRow(keyword, insertionIndex);
            }

            return null;
        }

        void RemoveInputFromDefaultSection(ShaderInput shaderInput)
        {
            switch (shaderInput)
            {
                case AbstractShaderProperty property:
                    m_PropertySectionController.RemoveBlackboardRow(property);
                    break;
                case ShaderKeyword keyword:
                    m_KeywordSectionController.RemoveBlackboardRow(keyword);
                    break;
            }
        }

        bool IsInputInDefaultCategory(ShaderInput shaderInput)
        {
            bool addToDefaultSection = true;
            foreach (var sectionController in m_BlackboardSectionControllers)
            {
                if (sectionController.IsInputInSection(shaderInput))
                    addToDefaultSection = false;
            }

            return addToDefaultSection;
        }

        public BlackboardRow GetBlackboardRow(ShaderInput blackboardItem)
        {
            return new BlackboardRow(new VisualElement(), null);
        }

        public void HandleGraphChanges(bool wasUndoRedoPerformed)
        {
        }

        internal int numberOfSections => m_BlackboardSectionControllers.Count;

        // Gets the index after the currently selected shader input per row.
        internal List<int> GetIndicesOfSelectedItems()
        {
            List<int> indexPerSection = new List<int>();

            for (int x = 0; x < numberOfSections; x++)
                indexPerSection.Add(-1);

            if (blackboard?.selection == null || blackboard.selection.Count == 0)
                return indexPerSection;

            foreach (ISelectable selection in blackboard.selection)
            {
                if (selection is BlackboardPropertyView blackboardPropertyView)
                {
                    SGBlackboardRow row = blackboardPropertyView.GetFirstAncestorOfType<SGBlackboardRow>();
                    SGBlackboardSection section = blackboardPropertyView.GetFirstAncestorOfType<SGBlackboardSection>();
                    if (row == null || section == null)
                        continue;
                    VisualElement sectionContainer = section.parent;

                    int sectionIndex = sectionContainer.IndexOf(section);
                    if (sectionIndex > numberOfSections)
                        continue;

                    int rowAfterIndex = section.IndexOf(row) + 1;
                    if (rowAfterIndex  > indexPerSection[sectionIndex])
                    {
                        indexPerSection[sectionIndex] = rowAfterIndex;
                    }
                }
            }

            return indexPerSection;
        }

        // A map from shaderInput reference names to the viewDataKey of the BlackboardPropertyView that is used to represent them
        // This data is used to re-select the shaderInputs in the blackboard after an undo/redo is performed
        Dictionary<string, string> oldSelectionPersistenceData { get; set; } = new Dictionary<string, string>();

        void UpdateSelectionAfterUndoRedo(AttachToPanelEvent evt)
        {
            var propertyView = evt.target as BlackboardPropertyView;
            // If this field view represents a value that was previously selected
            var refName = propertyView?.shaderInput?.referenceName;
            if (refName != null && oldSelectionPersistenceData.TryGetValue(refName, out var oldViewDataKey))
            {
                var graphView = ViewModel.parentView as MaterialGraphView;
                // This re-selects the property view if it existed, was deleted, and then an undo action added it back
                graphView?.AddToSelectionNoUndoRecord(propertyView);
            }
        }

        void StoreSelection(IList<ISelectable> newSelection)
        {
            if (newSelection.Count == 0)
            {
                oldSelectionPersistenceData.Clear();
                return;
            }

            // This tries to maintain the selection the user had before the undo/redo was performed
            foreach (var item in newSelection)
            {
                if (item is BlackboardPropertyView blackboardPropertyView)
                {
                    var guid = blackboardPropertyView.shaderInput.referenceName;
                    if(oldSelectionPersistenceData.TryGetValue(guid, out var viewDataKey) == false)
                        oldSelectionPersistenceData.Add(guid, blackboardPropertyView.viewDataKey);
                }
            }
        }
    }
}
