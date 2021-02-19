using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;
using UnityEditor.ShaderGraph.Drawing.Views;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Assertions;
using GraphDataStore = UnityEditor.ShaderGraph.DataStore<UnityEditor.ShaderGraph.GraphData>;
using BlackboardItem = UnityEditor.ShaderGraph.Internal.ShaderInput;
using BlackboardItemController = UnityEditor.ShaderGraph.Drawing.ShaderInputViewController;

namespace UnityEditor.ShaderGraph.Drawing
{
    class AddBlackboardItemAction : IGraphDataAction
    {
        void AddBlackboardItem(GraphData m_GraphData)
        {
            if (m_GraphData != null)
            {
                // If type property is valid, create instance of that type
                if (BlackboardItemType != null && BlackboardItemType.IsSubclassOf(typeof(BlackboardItem)))
                    BlackboardItemReference = (BlackboardItem)Activator.CreateInstance(BlackboardItemType, true);
                // If type is null a direct override object must have been provided or else we are in an error-state
                else if (BlackboardItemReference == null)
                {
                    Debug.Log("ERROR: BlackboardController: Unable to complete Add Blackboard Item action.");
                    return;
                }

                BlackboardItemReference.generatePropertyBlock = BlackboardItemReference.isExposable;

                m_GraphData.owner.RegisterCompleteObjectUndo("Add Blackboard Item");
                m_GraphData.AddGraphInput(BlackboardItemReference);
            }
        }

        public Action<GraphData> ModifyGraphDataAction => AddBlackboardItem;

        // If this is a subclass of ShaderInput and is not null, then an object of this type is created to add to blackboard
        public Type BlackboardItemType { get; set; }

        // If the type field above is null and this is provided, then it is directly used as the item to add to blackboard
        public BlackboardItem BlackboardItemReference { get; set; }
    }

	class ChangeGraphPathAction : IGraphDataAction
    {
		void ChangeGraphPath(GraphData m_GraphData)
		{
            if (m_GraphData != null)
            {
                m_GraphData.path = NewGraphPath;
            }
		}

        public Action<GraphData> ModifyGraphDataAction => ChangeGraphPath;

		public string NewGraphPath { get; set; }
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

        BlackboardSectionController m_PropertySectionController;
        BlackboardSectionController m_KeywordSectionController;

        IList<BlackboardItemController> m_BlackboardItemControllers = new List<BlackboardItemController>();

        SGBlackboard m_Blackboard;

        internal SGBlackboard Blackboard
        {
            get => m_Blackboard;
            private set => m_Blackboard = value;
        }

        internal BlackboardController(GraphData model, BlackboardViewModel inViewModel, GraphDataStore graphDataStore)
            : base(model, inViewModel, graphDataStore)
        {
            InitializeViewModel();

            Blackboard = new SGBlackboard(ViewModel);
            Blackboard.controller = this;

            // Temporary patch-fix for how GraphView handles deleting graph selections, should go through data store ideally
            if (ViewModel.ParentView is MaterialGraphView graphView)
                graphView.blackboardItemRemovedDelegate += RemoveBlackboardRow;

            var propertySectionViewModel = new BlackboardSectionViewModel();
            propertySectionViewModel.ParentView = Blackboard;
            propertySectionViewModel.Name = "Properties";
            propertySectionViewModel.RequestModelChangeAction = ViewModel.RequestModelChangeAction;
            m_PropertySectionController = new BlackboardSectionController(model, propertySectionViewModel, graphDataStore);
            //RegisterHandler(m_PropertySectionController);

            var keywordSectionViewModel = new BlackboardSectionViewModel();
            keywordSectionViewModel.ParentView = Blackboard;
            keywordSectionViewModel.Name = "Keywords";
            keywordSectionViewModel.RequestModelChangeAction = ViewModel.RequestModelChangeAction;
            m_KeywordSectionController = new BlackboardSectionController(model, keywordSectionViewModel, graphDataStore);
            //RegisterHandler(m_KeywordSectionController);

            Blackboard.PropertySection = m_PropertySectionController.BlackboardSectionView;
            Blackboard.KeywordSection = m_KeywordSectionController.BlackboardSectionView;
            Blackboard.contentContainer.Add(Blackboard.PropertySection);
            Blackboard.contentContainer.Add(Blackboard.KeywordSection);

            foreach (var shaderInput in DataStore.State.properties)
            {
                CreateBlackboardRow(shaderInput);
            }

            foreach (var shaderInput in DataStore.State.keywords)
            {
                CreateBlackboardRow(shaderInput);
            }
        }

        void InitializeViewModel()
        {
            // Clear the view model
            ViewModel.Reset();

            ViewModel.Subtitle =  BlackboardUtils.FormatPath(Model.path);

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

                ViewModel.PropertyNameToAddActionMap.Add(name, new AddBlackboardItemAction() { BlackboardItemType = shaderInputType });

                // QUICK FIX TO DEAL WITH DEPRECATED COLOR PROPERTY
                if (ShaderGraphPreferences.allowDeprecatedBehaviors)
                {
                    ViewModel.PropertyNameToAddActionMap.Add("Color (Deprecated)", new AddBlackboardItemAction() { BlackboardItemReference = new ColorShaderProperty(ColorShaderProperty.deprecatedVersion) });
                }

            }

            // Default Keywords next
            ViewModel.DefaultKeywordNameToAddActionMap.Add("Boolean",  new AddBlackboardItemAction() { BlackboardItemReference = new ShaderKeyword(KeywordType.Boolean) });
            ViewModel.DefaultKeywordNameToAddActionMap.Add("Enum",  new AddBlackboardItemAction() { BlackboardItemReference = new ShaderKeyword(KeywordType.Enum) });

            // Built-In Keywords last
            foreach (var builtinKeywordDescriptor in KeywordUtil.GetBuiltinKeywordDescriptors())
            {
                var keyword = ShaderKeyword.CreateBuiltInKeyword(builtinKeywordDescriptor);
                // Do not allow user to add built-in keywords that conflict with user-made keywords that have the same reference name
                if (Model.keywords.Where(x => x.referenceName == keyword.referenceName).Any())
                {
                    ViewModel.DisabledKeywordNameList.Add(keyword.displayName);
                }
                else
                {
                    ViewModel.BuiltInKeywordNameToAddActionMap.Add(keyword.displayName,  new AddBlackboardItemAction() { BlackboardItemReference = keyword.Copy() });
                }
            }

			ViewModel.RequestModelChangeAction = this.RequestModelChange;
        }

        protected override void RequestModelChange(IGraphDataAction changeAction)
        {
            DataStore.Dispatch(changeAction);
        }

        // Called by GraphDataStore.Subscribe after the model has been changed
        protected override void ModelChanged(GraphData graphData, IGraphDataAction changeAction)
        {
            if (changeAction is AddBlackboardItemAction addBlackboardItemAction)
            {
                var blackboardRow = CreateBlackboardRow(addBlackboardItemAction.BlackboardItemReference);
                // Rows should auto-expand when an input is first added
                blackboardRow.expanded = true;
            }

            // Reconstruct view-model first
            // TODO: (would be cool to have some scoping here to see if the action was one that changed the UI or not, could avoid reconstructing the ViewModel based on that)
            InitializeViewModel();

            // Lets all event handlers this controller owns/manages know that the model has changed
            // Usually this is to update views and make them reconstruct themself from updated view-model
            NotifyChange(changeAction);

            // Let child controllers know about changes to this controller so they may update themselves in turn
            ApplyChanges();
        }

        // Creates controller, view and view model for a blackboard item and adds the view to the blackboard
        SGBlackboardRow CreateBlackboardRow(BlackboardItem shaderInput)
        {
            var shaderInputViewModel = new ShaderInputViewModel()
            {
                Model = shaderInput,
                ParentView = this.Blackboard
            };
            var blackboardItemController = new BlackboardItemController(shaderInput, shaderInputViewModel, DataStore);
            m_BlackboardItemControllers.Add(blackboardItemController);
            if (shaderInput is AbstractShaderProperty)
                Blackboard.AddPropertyRow(blackboardItemController.BlackboardItemView);
            else
                Blackboard.AddKeywordRow(blackboardItemController.BlackboardItemView);

            return blackboardItemController.BlackboardItemView;
        }

        void RemoveBlackboardRow(BlackboardItem shaderInput)
        {
            BlackboardItemController associatedBlackboardItemController = null;
            foreach (var blackboardItemController in m_BlackboardItemControllers.ToList())
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

            if(shaderInput is AbstractShaderProperty)
                Blackboard.RemovePropertyRow(associatedBlackboardItemController.BlackboardItemView);
            else
                Blackboard.RemoveKeywordRow(associatedBlackboardItemController.BlackboardItemView);

            m_BlackboardItemControllers.Remove(associatedBlackboardItemController);
        }

        public BlackboardRow GetBlackboardRow(ShaderInput blackboardItem)
        {
            return new BlackboardRow(new VisualElement(), null);
        }
    }
}
