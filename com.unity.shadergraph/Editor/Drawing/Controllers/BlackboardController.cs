using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Views;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Serialization;
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
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out AddShaderInputAction");

            // If type property is valid, create instance of that type
            if (blackboardItemType != null && blackboardItemType.IsSubclassOf(typeof(BlackboardItem)))
                shaderInputReference = (BlackboardItem)Activator.CreateInstance(blackboardItemType, true);
            // If type is null a direct override object must have been provided or else we are in an error-state
            else if (shaderInputReference == null)
            {
                AssertHelpers.Fail("BlackboardController: Unable to complete Add Shader Input action.");
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
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out ChangeGraphPathAction");
            graphData.path = NewGraphPath;
        }

        public Action<GraphData> modifyGraphDataAction => ChangeGraphPath;

        public string NewGraphPath { get; set; }
    }

    class CopyShaderInputAction : IGraphDataAction
    {
        void CopyShaderInput(GraphData graphData)
        {
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out CopyShaderInputAction");
            AssertHelpers.IsNotNull(shaderInputToCopy, "ShaderInputToCopy is null while carrying out CopyShaderInputAction");
            // Don't handle undo here as there are different contexts in which this action is used, that define the undo action namea
            // TODO: Perhaps a sign that each of those need to be made their own actions instead of conflating intent into a single action
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
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Action<GraphData> modifyGraphDataAction => CopyShaderInput;

        public IEnumerable<AbstractMaterialNode> dependentNodeList { get; set; } = new List<AbstractMaterialNode>();

        public BlackboardItem shaderInputToCopy { get; set; }

        public BlackboardItem copiedShaderInput { get; set; }

        public int insertIndex { get; set; } = -1;
    }

    class AddCategoryAction : IGraphDataAction
    {
        public enum AddActionSource
        {
            Default,
            AddMenu
        }

        void AddCategory(GraphData graphData)
        {
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out AddCategoryAction");
            graphData.owner.RegisterCompleteObjectUndo("Add Category");
            // If categoryDataReference is not null, directly add it to graphData
            graphData.AddCategory(categoryDataReference ?? new CategoryData(categoryName, childItems));
        }

        public Action<GraphData> modifyGraphDataAction => AddCategory;

        // Direct reference to the categoryData to use if it is specified
        public CategoryData categoryDataReference { get; set; }

        public AddActionSource addInputActionType { get; set; }

        public string categoryName { get; set; } = String.Empty;

        public Guid categoryGUID { get; set; }

        public List<ShaderInput> childItems { get; set; }
    }

    // TODO: These are stub classes, feel free to change them
    class AddItemToCategoryAction : IGraphDataAction
    {
        void AddItemsToCategory(GraphData graphData)
        {
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out AddItemToCategoryAction");
            graphData.owner.RegisterCompleteObjectUndo("Add Item to Category");
            graphData.AddItemToCategory(category, itemToAdd);
        }

        public Action<GraphData> modifyGraphDataAction => AddItemsToCategory;

        public CategoryData category { get; set; }

        public ShaderInput itemToAdd{ get; set; }
    }

    // TODO: These are stub classes, feel free to change them
    class RemoveItemsFromCategoryAction : IGraphDataAction
    {
        void RemoveItemsFromCategory(GraphData graphData)
        {
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out RemoveItemsFromCategoryAction");
            graphData.owner.RegisterCompleteObjectUndo("Remove Item from Category");
            graphData.RemoveItemFromCategory(category, itemToRemove);
        }

        public Action<GraphData> modifyGraphDataAction => RemoveItemsFromCategory;

        public CategoryData category { get; set; }

        public ShaderInput itemToRemove{ get; set; }
    }

    class BlackboardController : SGViewController<GraphData, BlackboardViewModel>
    {
        // Type changes (adds/removes of Types) only happen after a full assembly reload so its safe to make this static
        static IList<Type> s_ShaderInputTypes;

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

            s_ShaderInputTypes = shaderInputTypes.ToList();
        }

        internal int propertyCategoryIndex = 0;
        internal int keywordCategoryIndex = 1;

        BlackboardCategoryController m_DefaultCategoryController;
        CategoryData m_DefaultCategoryDataReference;
        IList<BlackboardCategoryController> m_BlackboardCategoryControllers = new List<BlackboardCategoryController>();

        SGBlackboard m_Blackboard;

        internal SGBlackboard blackboard
        {
            get => m_Blackboard;
            private set => m_Blackboard = value;
        }

        void InitializeViewModel()
        {
            // Clear the view model
            ViewModel.ResetViewModelData();

            ViewModel.subtitle = BlackboardUtils.FormatPath(Model.path);

            // Property data first
            foreach (var shaderInputType in s_ShaderInputTypes)
            {
                if (shaderInputType.IsAbstract)
                    continue;

                var info = Attribute.GetCustomAttribute(shaderInputType, typeof(BlackboardInputInfo)) as BlackboardInputInfo;
                string name = info?.name ?? ObjectNames.NicifyVariableName(shaderInputType.Name.Replace("ShaderProperty", ""));

                // QUICK FIX TO DEAL WITH DEPRECATED COLOR PROPERTY
                if (name.Equals("Color", StringComparison.InvariantCultureIgnoreCase) && ShaderGraphPreferences.allowDeprecatedBehaviors)
                {
                    ViewModel.propertyNameToAddActionMap.Add("Color (Deprecated)", new AddShaderInputAction() { shaderInputReference = new ColorShaderProperty(ColorShaderProperty.deprecatedVersion), addInputActionType = AddShaderInputAction.AddActionSource.AddMenu});
                    ViewModel.propertyNameToAddActionMap.Add(name, new AddShaderInputAction() { blackboardItemType = shaderInputType, addInputActionType = AddShaderInputAction.AddActionSource.AddMenu });
                }
                else
                    ViewModel.propertyNameToAddActionMap.Add(name, new AddShaderInputAction() { blackboardItemType = shaderInputType, addInputActionType = AddShaderInputAction.AddActionSource.AddMenu });
            }

            // Default Keywords next
            ViewModel.defaultKeywordNameToAddActionMap.Add("Boolean",  new AddShaderInputAction() { shaderInputReference = new ShaderKeyword(KeywordType.Boolean), addInputActionType = AddShaderInputAction.AddActionSource.AddMenu });
            ViewModel.defaultKeywordNameToAddActionMap.Add("Enum",  new AddShaderInputAction() { shaderInputReference = new ShaderKeyword(KeywordType.Enum), addInputActionType = AddShaderInputAction.AddActionSource.AddMenu });

            // Built-In Keywords after that
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

            // Category data last
            var defaultCategoryReference = new CategoryData("Category", new List<ShaderInput>());
            ViewModel.addCategoryAction = new AddCategoryAction() { categoryDataReference = defaultCategoryReference };

            ViewModel.requestModelChangeAction = this.RequestModelChange;

            // Create the default categories and add all inputs that exist outside of any category, to them
            var defaultItems = new List<ShaderInput>();
            foreach (var property in DataStore.State.properties)
                defaultItems.Add(property);

            foreach (var keyword in DataStore.State.keywords)
                defaultItems.Add(keyword);

            m_DefaultCategoryDataReference = new CategoryData("Uncategorized", defaultItems);
            ViewModel.categoryInfoList.Add(m_DefaultCategoryDataReference);

            ViewModel.categoryInfoList.AddRange(DataStore.State.categories.ToList());
        }

        internal BlackboardController(GraphData model, BlackboardViewModel inViewModel, GraphDataStore graphDataStore)
            : base(model, inViewModel, graphDataStore)
        {
            InitializeViewModel();

            blackboard = new SGBlackboard(ViewModel);
            blackboard.controller = this;

            // The Blackboard Controller is responsible for handling the default categories
            AddBlackboardCategory(Model, DataStore, m_DefaultCategoryDataReference);

            // Including owning a reference to the controller of the default category
            m_DefaultCategoryController = m_BlackboardCategoryControllers.First();

            foreach (var shaderProperty in DataStore.State.properties)
                if (IsInputInDefaultCategory(shaderProperty))
                    AddInputToDefaultCategory(shaderProperty);

            foreach (var shaderKeyword in DataStore.State.keywords)
                if (IsInputInDefaultCategory(shaderKeyword))
                    AddInputToDefaultCategory(shaderKeyword);

            blackboard.contentContainer.Add(m_DefaultCategoryController.blackboardCategoryView);

            // Followed by adding the user made categories (which have their own controllers)
            // Skipping the first, which we already have handled and defined as the default category
            foreach (var categoryInfo in ViewModel.categoryInfoList.Skip(1))
                AddBlackboardCategory(model, graphDataStore, categoryInfo);

        }

        BlackboardCategoryController AddBlackboardCategory(GraphData model, GraphDataStore graphDataStore, CategoryData categoryInfo)
        {
            var blackboardCategoryViewModel = new BlackboardCategoryViewModel();
            blackboardCategoryViewModel.parentView = blackboard;
            blackboardCategoryViewModel.requestModelChangeAction = ViewModel.requestModelChangeAction;
            blackboardCategoryViewModel.name = categoryInfo.name;
            blackboardCategoryViewModel.associatedCategoryID = categoryInfo.objectId;
            var blackboardCategoryController = new BlackboardCategoryController(model, blackboardCategoryViewModel, graphDataStore);
            m_BlackboardCategoryControllers.Add(blackboardCategoryController);
            return blackboardCategoryController;
        }

        public void UpdateBlackboardTitle(string newTitle)
        {
            ViewModel.title = newTitle;
            blackboard.title = ViewModel.title;
        }

        protected override void RequestModelChange(IGraphDataAction changeAction)
        {
            DataStore.Dispatch(changeAction);
        }

        // Called by GraphDataStore.Subscribe after the model has been changed
        protected override void ModelChanged(GraphData graphData, IGraphDataAction changeAction)
        {
            // Reconstruct view-model first
            InitializeViewModel();

            switch (changeAction)
            {
                // If newly added input doesn't belong to any of the user-made categories, add it to the appropriate default category
                case AddShaderInputAction addBlackboardItemAction:
                    if (IsInputInDefaultCategory(addBlackboardItemAction.shaderInputReference))
                    {
                        var blackboardRow = AddInputToDefaultCategory(addBlackboardItemAction.shaderInputReference);
                        var propertyView = blackboardRow.Q<BlackboardPropertyView>();
                        if (addBlackboardItemAction.addInputActionType == AddShaderInputAction.AddActionSource.AddMenu)
                            propertyView.OpenTextEditor();
                    }

                    break;

                case DeleteShaderInputAction deleteShaderInputAction:
                    foreach (var shaderInput in deleteShaderInputAction.shaderInputsToDelete)
                        if (IsInputInDefaultCategory(shaderInput))
                            RemoveInputFromDefaultCategory(shaderInput);
                    break;

                case HandleUndoRedoAction handleUndoRedoAction:
                    foreach (var shaderInput in graphData.removedInputs)
                        if (IsInputInDefaultCategory(shaderInput))
                            RemoveInputFromDefaultCategory(shaderInput);

                    foreach (var shaderInput in graphData.addedInputs)
                        if (IsInputInDefaultCategory(shaderInput))
                            AddInputToDefaultCategory(shaderInput);
                    break;

                case CopyShaderInputAction copyShaderInputAction:
                    if (IsInputInDefaultCategory(copyShaderInputAction.copiedShaderInput))
                    {
                        var blackboardRow = InsertInputInDefaultCategory(copyShaderInputAction.copiedShaderInput, copyShaderInputAction.insertIndex);

                        // This selects the newly created property value without over-riding the undo stack in case user wants to undo
                        var graphView = ViewModel.parentView as MaterialGraphView;
                        graphView?.ClearSelectionNoUndoRecord();
                        var propertyView = blackboardRow.Q<BlackboardPropertyView>();
                        graphView?.AddToSelectionNoUndoRecord(propertyView);
                    }

                    break;
                case ConvertToPropertyAction convertToPropertyAction:
                    foreach (var convertedProperty in convertToPropertyAction.convertedPropertyReferences)
                        if (IsInputInDefaultCategory(convertedProperty))
                            AddInputToDefaultCategory(convertedProperty);
                    break;

                case AddCategoryAction addCategoryAction:
                    var blackboardCategoryController = AddBlackboardCategory(Model, DataStore, addCategoryAction.categoryDataReference);
                    // Iterate through anything that is selected currently
                    foreach (var selectedElement in blackboard.selection.ToList())
                    {
                        if (selectedElement is BlackboardPropertyView { userData: ShaderInput shaderInput })
                        {
                            // If a blackboard item is selected, add it to the newly created category
                            var controllerForCategoryContainingElement = GetCategoryController(shaderInput);
                            controllerForCategoryContainingElement?.RemoveBlackboardRow(shaderInput);

                            var addItemToCategoryAction = new AddItemToCategoryAction();
                            addItemToCategoryAction.category = addCategoryAction.categoryDataReference;
                            addItemToCategoryAction.itemToAdd= shaderInput;
                            DataStore.Dispatch(addItemToCategoryAction);
                        }
                    }
                    break;
            }

            // Lets all event handlers this controller owns/manages know that the model has changed
            // Usually this is to update views and make them reconstruct themself from updated view-model
            //NotifyChange(changeAction);

            // Let child controllers know about changes to this controller so they may update themselves in turn
            //ApplyChanges();
        }

        SGBlackboardRow AddInputToDefaultCategory(ShaderInput shaderInput)
        {
            return m_DefaultCategoryController.InsertBlackboardRow(shaderInput);
        }

        SGBlackboardRow InsertInputInDefaultCategory(ShaderInput shaderInput, int insertionIndex)
        {
            return m_DefaultCategoryController.InsertBlackboardRow(shaderInput, insertionIndex);
        }

        void RemoveInputFromDefaultCategory(ShaderInput shaderInput)
        {
            m_DefaultCategoryController.RemoveBlackboardRow(shaderInput);
        }

        bool IsInputInDefaultCategory(ShaderInput shaderInput)
        {
            foreach (var categoryController in m_BlackboardCategoryControllers)
            {
                if (categoryController.IsInputInCategory(shaderInput))
                    return false;
            }

            return true;
        }

        public SGBlackboardRow GetBlackboardRow(ShaderInput blackboardItem)
        {
            foreach (var categoryController in m_BlackboardCategoryControllers)
            {
                var blackboardRow = categoryController.FindBlackboardRow(blackboardItem);
                if (blackboardRow != null)
                    return blackboardRow;
            }

            return null;
        }

        BlackboardCategoryController GetCategoryController(ShaderInput blackboardItem)
        {
            foreach (var categoryController in m_BlackboardCategoryControllers)
            {
                var blackboardRow = categoryController.FindBlackboardRow(blackboardItem);
                if (blackboardRow != null)
                    return categoryController;
            }

            return null;
        }

        int numberOfCategories => m_BlackboardCategoryControllers.Count;

        // Gets the index after the currently selected shader input per row.
        internal List<int> GetIndicesOfSelectedItems()
        {
            List<int> indexPerCategory = new List<int>();

            for (int x = 0; x < numberOfCategories; x++)
                indexPerCategory.Add(-1);

            if (blackboard?.selection == null || blackboard.selection.Count == 0)
                return indexPerCategory;

            foreach (ISelectable selection in blackboard.selection)
            {
                if (selection is BlackboardPropertyView blackboardPropertyView)
                {
                    SGBlackboardRow row = blackboardPropertyView.GetFirstAncestorOfType<SGBlackboardRow>();
                    SGBlackboardCategory category = blackboardPropertyView.GetFirstAncestorOfType<SGBlackboardCategory>();
                    if (row == null || category == null)
                        continue;
                    VisualElement categoryContainer = category.parent;

                    int categoryIndex = categoryContainer.IndexOf(category);
                    if (categoryIndex > numberOfCategories)
                        continue;

                    int rowAfterIndex = category.IndexOf(row) + 1;
                    if (rowAfterIndex  > indexPerCategory[categoryIndex])
                    {
                        indexPerCategory[categoryIndex] = rowAfterIndex;
                    }
                }
            }

            return indexPerCategory;
        }
    }
}
