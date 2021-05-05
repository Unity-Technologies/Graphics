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
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out AddShaderInputAction");

            // If type property is valid, create instance of that type
            if (blackboardItemType != null && blackboardItemType.IsSubclassOf(typeof(BlackboardItem)))
            {
                shaderInputReference = (BlackboardItem)Activator.CreateInstance(blackboardItemType, true);
            }
            else if (shaderInputReferenceGetter != null)
            {
                shaderInputReference = shaderInputReferenceGetter();
            }
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

        public Func<BlackboardItem> shaderInputReferenceGetter = null;

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

                case ShaderDropdown shaderDropdown:
                    var copiedDropdown = (ShaderDropdown)graphData.AddCopyOfShaderInput(shaderDropdown, insertIndex);

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
        }

        public Action<GraphData> modifyGraphDataAction => CopyShaderInput;

        public IEnumerable<AbstractMaterialNode> dependentNodeList { get; set; } = new List<AbstractMaterialNode>();

        public BlackboardItem shaderInputToCopy { get; set; }

        public BlackboardItem copiedShaderInput { get; set; }

        public int insertIndex { get; set; } = -1;
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

        internal int propertySectionIndex = 0;
        internal int keywordSectionIndex = 1;
        internal int dropdownSectionIndex = 2;

        BlackboardSectionController m_PropertySectionController;
        BlackboardSectionController m_KeywordSectionController;
        BlackboardSectionController m_DropdownSectionController;
        IList<BlackboardSectionController> m_BlackboardSectionControllers = new List<BlackboardSectionController>();

        SGBlackboard m_Blackboard;

        internal SGBlackboard blackboard
        {
            get => m_Blackboard;
            private set => m_Blackboard = value;
        }

        void InitializeViewModel(bool useDropdowns)
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
                    ViewModel.propertyNameToAddActionMap.Add("Color (Deprecated)", new AddShaderInputAction() { shaderInputReferenceGetter = () => new ColorShaderProperty(ColorShaderProperty.deprecatedVersion), addInputActionType = AddShaderInputAction.AddActionSource.AddMenu});
                    ViewModel.propertyNameToAddActionMap.Add(name, new AddShaderInputAction() { blackboardItemType = shaderInputType, addInputActionType = AddShaderInputAction.AddActionSource.AddMenu });
                }
                else
                    ViewModel.propertyNameToAddActionMap.Add(name, new AddShaderInputAction() { blackboardItemType = shaderInputType, addInputActionType = AddShaderInputAction.AddActionSource.AddMenu });
            }

            // Default Keywords next
            ViewModel.defaultKeywordNameToAddActionMap.Add("Boolean", new AddShaderInputAction() { shaderInputReferenceGetter = () => new ShaderKeyword(KeywordType.Boolean), addInputActionType = AddShaderInputAction.AddActionSource.AddMenu });
            ViewModel.defaultKeywordNameToAddActionMap.Add("Enum", new AddShaderInputAction() { shaderInputReferenceGetter = () => new ShaderKeyword(KeywordType.Enum), addInputActionType = AddShaderInputAction.AddActionSource.AddMenu });
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

            if (useDropdowns)
                ViewModel.defaultDropdownNameToAdd = new Tuple<string, IGraphDataAction>("Dropdown", new AddShaderInputAction() { shaderInputReferenceGetter = () => new ShaderDropdown(), addInputActionType = AddShaderInputAction.AddActionSource.AddMenu });

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

                if (useDropdowns)
                {
                    var dropdownGUIDs = new List<Guid>();
                    foreach (var dropdown in DataStore.State.dropdowns)
                        dropdownGUIDs.Add(dropdown.guid);
                    var defaultDropdownCategory = new CategoryData("Dropdowns", dropdownGUIDs);
                    ViewModel.categoryInfoList.Add(defaultDropdownCategory);
                }
            }
        }

        internal BlackboardController(GraphData model, BlackboardViewModel inViewModel, GraphDataStore graphDataStore)
            : base(model, inViewModel, graphDataStore)
        {
            // TODO: hide this more generically for category types.
            bool useDropdowns = model.isSubGraph;
            InitializeViewModel(useDropdowns);

            blackboard = new SGBlackboard(ViewModel);
            blackboard.controller = this;

            foreach (var categoryInfo in ViewModel.categoryInfoList)
            {
                var blackboardSectionViewModel = new BlackboardSectionViewModel();
                blackboardSectionViewModel.parentView = blackboard;
                blackboardSectionViewModel.requestModelChangeAction = ViewModel.requestModelChangeAction;
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

            blackboard.contentContainer.Add(m_PropertySectionController.BlackboardSectionView);
            blackboard.contentContainer.Add(m_KeywordSectionController.BlackboardSectionView);

            if (useDropdowns)
            {
                m_DropdownSectionController = m_BlackboardSectionControllers[2];
                foreach (var shaderDropdown in DataStore.State.dropdowns)
                    if (IsInputInDefaultCategory(shaderDropdown))
                        AddInputToDefaultSection(shaderDropdown);
                blackboard.contentContainer.Add(m_DropdownSectionController.BlackboardSectionView);
            }
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

            switch (changeAction)
            {
                // If newly added input doesn't belong to any of the sections, add it to the appropriate default section
                case AddShaderInputAction addBlackboardItemAction:
                    if (IsInputInDefaultCategory(addBlackboardItemAction.shaderInputReference))
                    {
                        var blackboardRow = AddInputToDefaultSection(addBlackboardItemAction.shaderInputReference);
                        var propertyView = blackboardRow.Q<BlackboardPropertyView>();
                        if (addBlackboardItemAction.addInputActionType == AddShaderInputAction.AddActionSource.AddMenu)
                            propertyView.OpenTextEditor();
                    }

                    break;

                case DeleteShaderInputAction deleteShaderInputAction:
                    foreach (var shaderInput in deleteShaderInputAction.shaderInputsToDelete)
                        if (IsInputInDefaultCategory(shaderInput))
                            RemoveInputFromDefaultSection(shaderInput);
                    break;

                case HandleUndoRedoAction handleUndoRedoAction:
                    foreach (var shaderInput in graphData.removedInputs)
                        if (IsInputInDefaultCategory(shaderInput))
                            RemoveInputFromDefaultSection(shaderInput);

                    foreach (var shaderInput in graphData.addedInputs)
                        if (IsInputInDefaultCategory(shaderInput))
                            AddInputToDefaultSection(shaderInput);
                    break;

                case CopyShaderInputAction copyShaderInputAction:
                    if (IsInputInDefaultCategory(copyShaderInputAction.copiedShaderInput))
                    {
                        var blackboardRow = InsertInputInDefaultSection(copyShaderInputAction.copiedShaderInput, copyShaderInputAction.insertIndex);

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
                            AddInputToDefaultSection(convertedProperty);
                    break;
            }

            // Lets all event handlers this controller owns/manages know that the model has changed
            // Usually this is to update views and make them reconstruct themself from updated view-model
            //NotifyChange(changeAction);

            // Let child controllers know about changes to this controller so they may update themselves in turn
            //ApplyChanges();
        }

        SGBlackboardRow AddInputToDefaultSection(ShaderInput shaderInput)
        {
            switch (shaderInput)
            {
                case AbstractShaderProperty property:
                    return m_PropertySectionController.InsertBlackboardRow(property);
                case ShaderKeyword keyword:
                    return m_KeywordSectionController.InsertBlackboardRow(keyword);
                case ShaderDropdown dropdown:
                    if (Model.isSubGraph)
                        return m_DropdownSectionController.InsertBlackboardRow(dropdown);
                    break;
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
                case ShaderDropdown dropdown:
                    if (Model.isSubGraph)
                        return m_DropdownSectionController.InsertBlackboardRow(dropdown, insertionIndex);
                    break;
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
                case ShaderDropdown dropdown:
                    if (Model.isSubGraph)
                        m_DropdownSectionController.RemoveBlackboardRow(dropdown);
                    break;
            }
        }

        bool IsInputInDefaultCategory(ShaderInput shaderInput)
        {
            foreach (var sectionController in m_BlackboardSectionControllers)
            {
                if (sectionController.IsInputInSection(shaderInput))
                    return false;
            }

            return true;
        }

        public SGBlackboardRow GetBlackboardRow(ShaderInput blackboardItem)
        {
            foreach (var sectionController in m_BlackboardSectionControllers)
            {
                var blackboardRow = sectionController.FindBlackboardRow(blackboardItem);
                if (blackboardRow != null)
                    return blackboardRow;
            }

            return null;
        }

        int numberOfSections => m_BlackboardSectionControllers.Count;

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
    }
}
