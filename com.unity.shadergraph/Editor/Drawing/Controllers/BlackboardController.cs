using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using GraphDataStore = UnityEditor.ShaderGraph.DataStore<UnityEditor.ShaderGraph.GraphData>;
using BlackboardItem = UnityEditor.ShaderGraph.Internal.ShaderInput;

namespace UnityEditor.ShaderGraph.Drawing
{
    struct BlackboardShaderInputOrder
    {
        public bool isKeyword;
        public bool isDropdown;
        public KeywordType keywordType;
        public ShaderKeyword builtInKeyword;
        public string deprecatedPropertyName;
        public int version;
    }
    class BlackboardShaderInputFactory
    {
        static public ShaderInput GetShaderInput(BlackboardShaderInputOrder order)
        {
            ShaderInput output;
            if (order.isKeyword)
            {
                if (order.builtInKeyword == null)
                {
                    output = new ShaderKeyword(order.keywordType);
                }
                else
                {
                    output = order.builtInKeyword;
                }
            }
            else if (order.isDropdown)
            {
                output = new ShaderDropdown();
            }
            else
            {
                switch (order.deprecatedPropertyName)
                {
                    case "Color":
                        output = new ColorShaderProperty(order.version);
                        break;
                    default:
                        output = null;
                        AssertHelpers.Fail("BlackboardShaderInputFactory: Unknown deprecated property type.");
                        break;
                }
            }

            return output;
        }
    }
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
            {
                shaderInputReference = (BlackboardItem)Activator.CreateInstance(blackboardItemType, true);
            }
            else if (m_ShaderInputReferenceGetter != null)
            {
                shaderInputReference = m_ShaderInputReferenceGetter();
            }
            // If type is null a direct override object must have been provided or else we are in an error-state
            else if (shaderInputReference == null)
            {
                AssertHelpers.Fail("BlackboardController: Unable to complete Add Shader Input action.");
                return;
            }

            shaderInputReference.generatePropertyBlock = shaderInputReference.isExposable;

            if (graphData.owner != null)
                graphData.owner.RegisterCompleteObjectUndo("Add Shader Input");
            else
                AssertHelpers.Fail("GraphObject is null while carrying out AddShaderInputAction");

            graphData.AddGraphInput(shaderInputReference);

            // If no categoryToAddItemToGuid is provided, add the input to the default category
            if (categoryToAddItemToGuid == String.Empty)
            {
                var defaultCategory = graphData.categories.FirstOrDefault();
                AssertHelpers.IsNotNull(defaultCategory, "Default category reference is null.");
                if (defaultCategory != null)
                {
                    var addItemToCategoryAction = new AddItemToCategoryAction();
                    addItemToCategoryAction.categoryGuid = defaultCategory.categoryGuid;
                    addItemToCategoryAction.itemToAdd = shaderInputReference;
                    graphData.owner.graphDataStore.Dispatch(addItemToCategoryAction);
                }
            }
            else
            {
                var addItemToCategoryAction = new AddItemToCategoryAction();
                addItemToCategoryAction.categoryGuid = categoryToAddItemToGuid;
                addItemToCategoryAction.itemToAdd = shaderInputReference;
                graphData.owner.graphDataStore.Dispatch(addItemToCategoryAction);
            }
        }

        public static AddShaderInputAction AddDeprecatedPropertyAction(BlackboardShaderInputOrder order)
        {
            return new() { shaderInputReference = BlackboardShaderInputFactory.GetShaderInput(order), addInputActionType = AddShaderInputAction.AddActionSource.AddMenu };
        }

        public static AddShaderInputAction AddDropdownAction(BlackboardShaderInputOrder order)
        {
            return new() { shaderInputReference = BlackboardShaderInputFactory.GetShaderInput(order), addInputActionType = AddShaderInputAction.AddActionSource.AddMenu };
        }

        public static AddShaderInputAction AddKeywordAction(BlackboardShaderInputOrder order)
        {
            return new() { shaderInputReference = BlackboardShaderInputFactory.GetShaderInput(order), addInputActionType = AddShaderInputAction.AddActionSource.AddMenu };
        }

        public static AddShaderInputAction AddPropertyAction(Type shaderInputType)
        {
            return new() { blackboardItemType = shaderInputType, addInputActionType = AddShaderInputAction.AddActionSource.AddMenu };
        }

        public Action<GraphData> modifyGraphDataAction => AddShaderInput;
        // If this is a subclass of ShaderInput and is not null, then an object of this type is created to add to blackboard
        // If the type field above is null and this is provided, then it is directly used as the item to add to blackboard
        public BlackboardItem shaderInputReference { get; set; }
        public AddActionSource addInputActionType { get; set; }
        public string categoryToAddItemToGuid { get; set; } = String.Empty;

        Type blackboardItemType { get; set; }

        Func<BlackboardItem> m_ShaderInputReferenceGetter = null;
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

            // Don't handle undo here as there are different contexts in which this action is used, that define the undo action
            // TODO: Perhaps a sign that each of those need to be made their own actions instead of conflating intent into a single action

            switch (shaderInputToCopy)
            {
                case AbstractShaderProperty property:

                    insertIndex = Mathf.Clamp(insertIndex, -1, graphData.properties.Count() - 1);
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
                    // InsertIndex gets passed in relative to the blackboard position of an item overall,
                    // and not relative to the array sizes of the properties/keywords/dropdowns
                    var keywordInsertIndex = insertIndex - graphData.properties.Count();

                    keywordInsertIndex = Mathf.Clamp(keywordInsertIndex, -1, graphData.keywords.Count() - 1);

                    // Don't duplicate built-in keywords within the same graph
                    if (shaderKeyword.isBuiltIn && graphData.keywords.Any(p => p.referenceName == shaderInputToCopy.referenceName))
                        return;

                    var copiedKeyword = (ShaderKeyword)graphData.AddCopyOfShaderInput(shaderKeyword, keywordInsertIndex);

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

                case ShaderDropdown shaderDropdown:
                    // InsertIndex gets passed in relative to the blackboard position of an item overall,
                    // and not relative to the array sizes of the properties/keywords/dropdowns
                    var dropdownInsertIndex = insertIndex - graphData.properties.Count() - graphData.keywords.Count();

                    dropdownInsertIndex = Mathf.Clamp(dropdownInsertIndex, -1, graphData.dropdowns.Count() - 1);

                    var copiedDropdown = (ShaderDropdown)graphData.AddCopyOfShaderInput(shaderDropdown, dropdownInsertIndex);

                    // Update the dropdown nodes that depends on the copied node
                    foreach (var node in dependentNodeList)
                    {
                        if (node is DropdownNode propertyNode)
                        {
                            propertyNode.owner = graphData;
                            propertyNode.dropdown = copiedDropdown;
                        }
                    }

                    copiedShaderInput = copiedDropdown;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (copiedShaderInput != null)
            {
                // If specific category to copy to is provided, find and use it
                foreach (var category in graphData.categories)
                {
                    if (category.categoryGuid == containingCategoryGuid)
                    {
                        // Ensures that the new item gets added after the item it was duplicated from
                        insertIndex += 1;
                        // If the source item was already the last item in list, just add to end of list
                        if (insertIndex >= category.childCount)
                            insertIndex = -1;
                        graphData.InsertItemIntoCategory(category.objectId, copiedShaderInput, insertIndex);
                        return;
                    }
                }

                // Else, add to default category
                graphData.categories.First().InsertItemIntoCategory(copiedShaderInput);
            }
        }

        public Action<GraphData> modifyGraphDataAction => CopyShaderInput;

        public IEnumerable<AbstractMaterialNode> dependentNodeList { get; set; } = new List<AbstractMaterialNode>();

        public BlackboardItem shaderInputToCopy { get; set; }

        public BlackboardItem copiedShaderInput { get; set; }

        public string containingCategoryGuid { get; set; }

        public int insertIndex { get; set; } = -1;
    }

    class AddCategoryAction : IGraphDataAction
    {
        void AddCategory(GraphData graphData)
        {
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out AddCategoryAction");
            graphData.owner.RegisterCompleteObjectUndo("Add Category");
            // If categoryDataReference is not null, directly add it to graphData
            if (categoryDataReference == null)
                categoryDataReference = new CategoryData(categoryName, childObjects);
            graphData.AddCategory(categoryDataReference);
        }

        public Action<GraphData> modifyGraphDataAction => AddCategory;

        // Direct reference to the categoryData to use if it is specified
        public CategoryData categoryDataReference { get; set; }
        public string categoryName { get; set; } = String.Empty;
        public List<ShaderInput> childObjects { get; set; }
    }

    class MoveCategoryAction : IGraphDataAction
    {
        void MoveCategory(GraphData graphData)
        {
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out MoveCategoryAction");
            graphData.owner.RegisterCompleteObjectUndo("Move Category");
            // Handling for out of range moves is slightly different, but otherwise we need to reverse for insertion order.
            var guids = newIndexValue >= graphData.categories.Count() ? categoryGuids : categoryGuids.Reverse<string>();
            foreach (var guid in categoryGuids)
            {
                var cat = graphData.categories.FirstOrDefault(c => c.categoryGuid == guid);
                graphData.MoveCategory(cat, newIndexValue);
            }
        }

        public Action<GraphData> modifyGraphDataAction => MoveCategory;

        // Reference to the shader input being modified
        internal List<string> categoryGuids { get; set; }

        internal int newIndexValue { get; set; }
    }

    class AddItemToCategoryAction : IGraphDataAction
    {
        public enum AddActionSource
        {
            Default,
            DragDrop
        }

        void AddItemsToCategory(GraphData graphData)
        {
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out AddItemToCategoryAction");
            graphData.owner.RegisterCompleteObjectUndo("Add Item to Category");
            graphData.InsertItemIntoCategory(categoryGuid, itemToAdd, indexToAddItemAt);
        }

        public Action<GraphData> modifyGraphDataAction => AddItemsToCategory;

        public string categoryGuid { get; set; }

        public ShaderInput itemToAdd { get; set; }

        // By default an item is always added to the end of a category, if this value is set to something other than -1, will insert the item at that position within the category
        public int indexToAddItemAt { get; set; } = -1;

        public AddActionSource addActionSource { get; set; }
    }

    class CopyCategoryAction : IGraphDataAction
    {
        void CopyCategory(GraphData graphData)
        {
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out CopyCategoryAction");
            AssertHelpers.IsNotNull(categoryToCopyReference, "CategoryToCopyReference is null while carrying out CopyCategoryAction");

            // This is called by MaterialGraphView currently, no need to repeat it here, though ideally it would live here
            //graphData.owner.RegisterCompleteObjectUndo("Copy Category");

            newCategoryDataReference = graphData.CopyCategory(categoryToCopyReference);
        }

        // Reference to the new category created as a copy
        public CategoryData newCategoryDataReference { get; set; }

        // After category has been copied, store reference to it
        public CategoryData categoryToCopyReference { get; set; }

        public Action<GraphData> modifyGraphDataAction => CopyCategory;
    }

    class ShaderVariantLimitAction : IGraphDataAction
    {
        public int currentVariantCount { get; set; } = 0;
        public int maxVariantCount { get; set; } = 0;

        public ShaderVariantLimitAction(int currentVariantCount, int maxVariantCount)
        {
            this.maxVariantCount = maxVariantCount;
            this.currentVariantCount = currentVariantCount;
        }

        // There's no action actually performed on the graph, but we need to implement this as a valid function
        public Action<GraphData> modifyGraphDataAction => Empty;

        void Empty(GraphData graphData)
        {
        }
    }

    class BlackboardController : SGViewController<GraphData, BlackboardViewModel>
    {
        // Type changes (adds/removes of Types) only happen after a full assembly reload so its safe to make this static
        static IList<Type> s_ShaderInputTypes;

        static BlackboardController()
        {
            var shaderInputTypes = TypeCache.GetTypesWithAttribute<BlackboardInputInfo>().ToList();
            // Sort the ShaderInput by priority using the BlackboardInputInfo attribute
            shaderInputTypes.Sort((s1, s2) =>
            {
                var info1 = Attribute.GetCustomAttribute(s1, typeof(BlackboardInputInfo)) as BlackboardInputInfo;
                var info2 = Attribute.GetCustomAttribute(s2, typeof(BlackboardInputInfo)) as BlackboardInputInfo;

                if (info1.priority == info2.priority)
                    return (info1.name ?? s1.Name).CompareTo(info2.name ?? s2.Name);
                else
                    return info1.priority.CompareTo(info2.priority);
            });

            s_ShaderInputTypes = shaderInputTypes.ToList();
        }

        BlackboardCategoryController m_DefaultCategoryController = null;
        Dictionary<string, BlackboardCategoryController> m_BlackboardCategoryControllers = new Dictionary<string, BlackboardCategoryController>();

        protected SGBlackboard m_Blackboard;

        internal SGBlackboard blackboard
        {
            get => m_Blackboard;
            private set => m_Blackboard = value;
        }
        public string GetFirstSelectedCategoryGuid()
        {
            if (m_Blackboard == null)
            {
                return string.Empty;
            }
            var copiedSelectionList = new List<ISelectable>(m_Blackboard.selection);
            var selectedCategories = new List<SGBlackboardCategory>();
            var selectedCategoryGuid = String.Empty;
            for (int i = 0; i < copiedSelectionList.Count; i++)
            {
                var selectable = copiedSelectionList[i];
                if (selectable is SGBlackboardCategory category)
                {
                    selectedCategories.Add(selectable as SGBlackboardCategory);
                }
            }
            if (selectedCategories.Any())
            {
                selectedCategoryGuid = selectedCategories[0].viewModel.associatedCategoryGuid;
            }
            return selectedCategoryGuid;
        }

        void InitializeViewModel(bool useDropdowns)
        {
            // Clear the view model
            ViewModel.ResetViewModelData();
            ViewModel.subtitle = BlackboardUtils.FormatPath(Model.path);
            BlackboardShaderInputOrder propertyTypesOrder = new BlackboardShaderInputOrder();

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
                    propertyTypesOrder.isKeyword = false;
                    propertyTypesOrder.deprecatedPropertyName = name;
                    propertyTypesOrder.version = ColorShaderProperty.deprecatedVersion;
                    ViewModel.propertyNameToAddActionMap.Add($"Color (Legacy v0)", AddShaderInputAction.AddDeprecatedPropertyAction(propertyTypesOrder));
                    ViewModel.propertyNameToAddActionMap.Add(name, AddShaderInputAction.AddPropertyAction(shaderInputType));
                }
                else
                    ViewModel.propertyNameToAddActionMap.Add(name, AddShaderInputAction.AddPropertyAction(shaderInputType));
            }

            // Default Keywords next
            BlackboardShaderInputOrder keywordTypesOrder = new BlackboardShaderInputOrder();
            keywordTypesOrder.isKeyword = true;
            keywordTypesOrder.keywordType = KeywordType.Boolean;
            ViewModel.defaultKeywordNameToAddActionMap.Add("Boolean", AddShaderInputAction.AddKeywordAction(keywordTypesOrder));
            keywordTypesOrder.keywordType = KeywordType.Enum;
            ViewModel.defaultKeywordNameToAddActionMap.Add("Enum", AddShaderInputAction.AddKeywordAction(keywordTypesOrder));

            // Built-In Keywords after that
            foreach (var builtinKeywordDescriptor in KeywordUtil.GetBuiltinKeywordDescriptors())
            {
                var keyword = ShaderKeyword.CreateBuiltInKeyword(builtinKeywordDescriptor);
                // Do not allow user to add built-in keywords that conflict with user-made keywords that have the same reference name or display name
                if (Model.keywords.Any(x => x.referenceName == keyword.referenceName || x.displayName == keyword.displayName))
                {
                    ViewModel.disabledKeywordNameList.Add(keyword.displayName);
                }
                else
                {
                    keywordTypesOrder.builtInKeyword = (ShaderKeyword)keyword.Copy();
                    ViewModel.builtInKeywordNameToAddActionMap.Add(keyword.displayName, AddShaderInputAction.AddKeywordAction(keywordTypesOrder));
                }
            }

            if (useDropdowns)
            {
                BlackboardShaderInputOrder dropdownsOrder = new BlackboardShaderInputOrder();
                dropdownsOrder.isDropdown = true;
                ViewModel.defaultDropdownNameToAdd = new Tuple<string, IGraphDataAction>("Dropdown", AddShaderInputAction.AddDropdownAction(dropdownsOrder));
            }

            // Category data last
            var defaultNewCategoryReference = new CategoryData("Category");
            ViewModel.addCategoryAction = new AddCategoryAction() { categoryDataReference = defaultNewCategoryReference };

            ViewModel.requestModelChangeAction = this.RequestModelChange;
            ViewModel.categoryInfoList.AddRange(DataStore.State.categories.ToList());
        }

        internal BlackboardController(GraphData model, BlackboardViewModel inViewModel, GraphDataStore graphDataStore)
            : base(model, inViewModel, graphDataStore)
        {
            // TODO: hide this more generically for category types.
            bool useDropdowns = model.isSubGraph;
            InitializeViewModel(useDropdowns);

            blackboard = new SGBlackboard(ViewModel, this);

            // Add default category at the top of the blackboard (create it if it doesn't exist already)
            var existingDefaultCategory = DataStore.State.categories.FirstOrDefault();
            if (existingDefaultCategory != null && existingDefaultCategory.IsNamedCategory() == false)
            {
                AddBlackboardCategory(graphDataStore, existingDefaultCategory);
            }
            else
            {
                // Any properties that don't already have a category (for example, if this graph is being loaded from an older version that doesn't have category data)
                var uncategorizedBlackboardItems = new List<ShaderInput>();
                foreach (var shaderProperty in DataStore.State.properties)
                    if (IsInputUncategorized(shaderProperty))
                        uncategorizedBlackboardItems.Add(shaderProperty);

                foreach (var shaderKeyword in DataStore.State.keywords)
                    if (IsInputUncategorized(shaderKeyword))
                        uncategorizedBlackboardItems.Add(shaderKeyword);

                if (useDropdowns)
                {
                    foreach (var shaderDropdown in DataStore.State.dropdowns)
                        if (IsInputUncategorized(shaderDropdown))
                            uncategorizedBlackboardItems.Add(shaderDropdown);
                }

                var addCategoryAction = new AddCategoryAction();
                addCategoryAction.categoryDataReference = CategoryData.DefaultCategory(uncategorizedBlackboardItems);
                graphDataStore.Dispatch(addCategoryAction);
            }

            // Get the reference to default category controller after its been added
            m_DefaultCategoryController = m_BlackboardCategoryControllers.Values.FirstOrDefault();
            AssertHelpers.IsNotNull(m_DefaultCategoryController, "Failed to instantiate default category.");

            // Handle loaded-in categories from graph first, skipping the first/default category
            foreach (var categoryData in ViewModel.categoryInfoList.Skip(1))
            {
                AddBlackboardCategory(graphDataStore, categoryData);
            }
        }

        internal string editorPrefsBaseKey => "unity.shadergraph." + DataStore.State.objectId;

        BlackboardCategoryController AddBlackboardCategory(GraphDataStore graphDataStore, CategoryData categoryInfo)
        {
            var blackboardCategoryViewModel = new BlackboardCategoryViewModel();
            blackboardCategoryViewModel.parentView = blackboard;
            blackboardCategoryViewModel.requestModelChangeAction = ViewModel.requestModelChangeAction;
            blackboardCategoryViewModel.name = categoryInfo.name;
            blackboardCategoryViewModel.associatedCategoryGuid = categoryInfo.categoryGuid;
            blackboardCategoryViewModel.isExpanded = EditorPrefs.GetBool($"{editorPrefsBaseKey}.{categoryInfo.categoryGuid}.{ChangeCategoryIsExpandedAction.kEditorPrefKey}", true);

            var blackboardCategoryController = new BlackboardCategoryController(categoryInfo, blackboardCategoryViewModel, graphDataStore);
            if (m_BlackboardCategoryControllers.ContainsKey(categoryInfo.categoryGuid) == false)
            {
                m_BlackboardCategoryControllers.Add(categoryInfo.categoryGuid, blackboardCategoryController);
            }
            else
            {
                AssertHelpers.Fail("Failed to add category controller due to category with same GUID already having been added.");
                return null;
            }
            return blackboardCategoryController;
        }

        // Creates controller, view and view model for a blackboard item and adds the view to the specified index in the category
        SGBlackboardRow InsertBlackboardRow(BlackboardItem shaderInput, int insertionIndex = -1)
        {
            return m_DefaultCategoryController.InsertBlackboardRow(shaderInput, insertionIndex);
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
            // TODO: hide this more generically for category types.
            bool useDropdowns = graphData.isSubGraph;
            InitializeViewModel(useDropdowns);

            var graphView = ViewModel.parentView as MaterialGraphView;

            switch (changeAction)
            {
                // If newly added input doesn't belong to any of the user-made categories, add it to the default category at top of blackboard
                case AddShaderInputAction addBlackboardItemAction:
                    if (IsInputUncategorized(addBlackboardItemAction.shaderInputReference))
                    {
                        var blackboardRow = InsertBlackboardRow(addBlackboardItemAction.shaderInputReference);
                        if (blackboardRow != null)
                        {
                            var propertyView = blackboardRow.Q<SGBlackboardField>();
                            if (addBlackboardItemAction.addInputActionType == AddShaderInputAction.AddActionSource.AddMenu)
                                propertyView.OpenTextEditor();
                        }
                    }
                    break;
                // Need to handle deletion of shader inputs here as opposed to BlackboardCategoryController, as currently,
                // once removed from the categories there is no way to associate an input with the category that owns it
                case DeleteShaderInputAction deleteShaderInputAction:
                    foreach (var shaderInput in deleteShaderInputAction.shaderInputsToDelete)
                        RemoveInputFromBlackboard(shaderInput);
                    break;

                case HandleUndoRedoAction handleUndoRedoAction:
                    ClearBlackboardCategories();

                    foreach (var categoryData in graphData.addedCategories)
                        AddBlackboardCategory(DataStore, categoryData);

                    m_DefaultCategoryController = m_BlackboardCategoryControllers.Values.FirstOrDefault();

                    break;
                case CopyShaderInputAction copyShaderInputAction:
                    // In the specific case of only-one keywords like Material Quality and Raytracing, they can get copied, but because only one can exist, the output copied value is null
                    if (copyShaderInputAction.copiedShaderInput != null && IsInputUncategorized(copyShaderInputAction.copiedShaderInput))
                    {
                        var blackboardRow = InsertBlackboardRow(copyShaderInputAction.copiedShaderInput, copyShaderInputAction.insertIndex);
                        var propertyView = blackboardRow.Q<SGBlackboardField>();
                        graphView?.AddToSelectionNoUndoRecord(propertyView);
                    }

                    break;

                case AddCategoryAction addCategoryAction:
                    AddBlackboardCategory(DataStore, addCategoryAction.categoryDataReference);
                    // Iterate through anything that is selected currently
                    foreach (var selectedElement in blackboard.selection.ToList())
                    {
                        if (selectedElement is SGBlackboardField { userData: ShaderInput shaderInput })
                        {
                            // If a blackboard item is selected, first remove it from the blackboard
                            RemoveInputFromBlackboard(shaderInput);

                            // Then add input to the new category
                            var addItemToCategoryAction = new AddItemToCategoryAction();
                            addItemToCategoryAction.categoryGuid = addCategoryAction.categoryDataReference.categoryGuid;
                            addItemToCategoryAction.itemToAdd = shaderInput;
                            DataStore.Dispatch(addItemToCategoryAction);
                        }
                    }
                    break;

                case DeleteCategoryAction deleteCategoryAction:
                    // Clean up deleted categories
                    foreach (var categoryGUID in deleteCategoryAction.categoriesToRemoveGuids)
                    {
                        RemoveBlackboardCategory(categoryGUID);
                    }
                    break;

                case MoveCategoryAction moveCategoryAction:
                    ClearBlackboardCategories();
                    foreach (var categoryData in ViewModel.categoryInfoList)
                        AddBlackboardCategory(graphData.owner.graphDataStore, categoryData);
                    break;

                case CopyCategoryAction copyCategoryAction:
                    var blackboardCategory = AddBlackboardCategory(graphData.owner.graphDataStore, copyCategoryAction.newCategoryDataReference);
                    if (blackboardCategory != null)
                        graphView?.AddToSelectionNoUndoRecord(blackboardCategory.blackboardCategoryView);
                    break;
                case ShaderVariantLimitAction shaderVariantLimitAction:
                    blackboard.SetCurrentVariantUsage(shaderVariantLimitAction.currentVariantCount, shaderVariantLimitAction.maxVariantCount);
                    break;
            }

            // Lets all event handlers this controller owns/manages know that the model has changed
            // Usually this is to update views and make them reconstruct themself from updated view-model
            //NotifyChange(changeAction);

            // Let child controllers know about changes to this controller so they may update themselves in turn
            //ApplyChanges();
        }

        void RemoveInputFromBlackboard(ShaderInput shaderInput)
        {
            // Check if input is in one of the categories
            foreach (var controller in m_BlackboardCategoryControllers.Values)
            {
                var blackboardRow = controller.FindBlackboardRow(shaderInput);
                if (blackboardRow != null)
                {
                    controller.RemoveBlackboardRow(shaderInput);
                    return;
                }
            }
        }

        bool IsInputUncategorized(ShaderInput shaderInput)
        {
            // Skip the first category controller as that is guaranteed to be the default category
            foreach (var categoryController in m_BlackboardCategoryControllers.Values.Skip(1))
            {
                if (categoryController.IsInputInCategory(shaderInput))
                    return false;
            }

            return true;
        }

        public SGBlackboardCategory GetBlackboardCategory(string inputGuid)
        {
            foreach (var categoryController in m_BlackboardCategoryControllers.Values)
            {
                if (categoryController.Model.categoryGuid == inputGuid)
                    return categoryController.blackboardCategoryView;
            }

            return null;
        }

        public SGBlackboardRow GetBlackboardRow(ShaderInput blackboardItem)
        {
            foreach (var categoryController in m_BlackboardCategoryControllers.Values)
            {
                var blackboardRow = categoryController.FindBlackboardRow(blackboardItem);
                if (blackboardRow != null)
                    return blackboardRow;
            }

            return null;
        }

        int numberOfCategories => m_BlackboardCategoryControllers.Count;

        // Gets the index after the currently selected shader input for pasting properties into this graph
        internal int GetInsertionIndexForPaste()
        {
            if (blackboard?.selection == null || blackboard.selection.Count == 0)
            {
                return 0;
            }

            foreach (ISelectable selection in blackboard.selection)
            {
                if (selection is SGBlackboardField blackboardPropertyView)
                {
                    SGBlackboardRow row = blackboardPropertyView.GetFirstAncestorOfType<SGBlackboardRow>();
                    SGBlackboardCategory category = blackboardPropertyView.GetFirstAncestorOfType<SGBlackboardCategory>();
                    if (row == null || category == null)
                        continue;
                    int blackboardFieldIndex = category.IndexOf(row);

                    return blackboardFieldIndex;
                }
            }

            return 0;
        }

        void RemoveBlackboardCategory(string categoryGUID)
        {
            m_BlackboardCategoryControllers.TryGetValue(categoryGUID, out var blackboardCategoryController);
            if (blackboardCategoryController != null)
            {
                blackboardCategoryController.Destroy();
                m_BlackboardCategoryControllers.Remove(categoryGUID);
            }
            else
                AssertHelpers.Fail("Tried to remove a category that doesn't exist. ");
        }

        void ClearBlackboardCategories()
        {
            foreach (var categoryController in m_BlackboardCategoryControllers.Values)
            {
                categoryController.Destroy();
            }
            m_BlackboardCategoryControllers.Clear();
        }

        // Meant to be used by UI testing in order to clear blackboard state
        internal void ResetBlackboardState()
        {
            ClearBlackboardCategories();
            var addCategoryAction = new AddCategoryAction();
            addCategoryAction.categoryDataReference = CategoryData.DefaultCategory();
            DataStore.Dispatch(addCategoryAction);
        }
    }
}
