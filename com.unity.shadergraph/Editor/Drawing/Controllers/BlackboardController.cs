using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;
using UnityEditor.ShaderGraph.Drawing.Views;
using UnityEditor.ShaderGraph.Internal;

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
                if (blackboardItemType != null && blackboardItemType.IsSubclassOf(typeof(BlackboardItem)))
                    blackboardItemReference = (BlackboardItem)Activator.CreateInstance(blackboardItemType, true);
                // If type is null a direct override object must have been provided or else we are in an error-state
                else if (blackboardItemReference == null)
                {
                    Debug.Log("ERROR: BlackboardController: Unable to complete Add Blackboard Item action.");
                    return;
                }

                m_GraphData.owner.RegisterCompleteObjectUndo("Add Blackboard Item");
                m_GraphData.AddGraphInput(blackboardItemReference);
            }
        }

        public Action<GraphData> ModifyGraphDataAction => AddBlackboardItem;

        // If this is a subclass of ShaderInput and is not null, then an object of this type is created to add to blackboard
        public Type blackboardItemType { get; set; }

        // If the type field above is null and this is provided, then it is directly used as the item to add to blackboard
        public BlackboardItem blackboardItemReference { get; set; }
    }

    class MoveBlackboardItemAction : IGraphDataAction
    {
        void MoveBlackboardItem(GraphData m_GraphData)
        {

        }

        public Action<GraphData> ModifyGraphDataAction => MoveBlackboardItem;

        BlackboardItem m_ItemToMove;
        int m_NewIndex;
    }

    class RemoveBlackboardItemAction : IGraphDataAction
    {
        void RemoveBlackboardItem(GraphData m_GraphData)
        {

        }

        public Action<GraphData> ModifyGraphDataAction => RemoveBlackboardItem;

        BlackboardItem m_ItemToRemove;
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

            // TODO: Change it so MoveItem isn't handled by the BlackboardController,
            // it'd be nice to have a BlackboardRowController that handled moving, deleting, updating etc after a BlackboardRow is created and its assigned that as its view
            foreach (var shaderInput in DataStore.State.properties)
            {
                var shaderInputViewModel = new ShaderInputViewModel();
                var shaderInputViewController = new ShaderInputViewController(shaderInput, shaderInputViewModel, graphDataStore);
                m_BlackboardItemControllers.Add(shaderInputViewController);
                // TODO: How to communicate with child controllers?
            }
        }

        void InitializeViewModel()
        {
            // Clear the view model
           iewModel.Reset();

            ViewModel.Subtitle = FormatPath(Model.path);

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

                ViewModel.PropertyNameToAddActionMap.Add(name, new AddBlackboardItemAction() { blackboardItemType = shaderInputType });

                // QUICK FIX TO DEAL WITH DEPRECATED COLOR PROPERTY
                if (ShaderGraphPreferences.allowDeprecatedBehaviors)
                {
                    ViewModel.PropertyNameToAddActionMap.Add("Color (Deprecated)", new AddBlackboardItemAction() { blackboardItemReference = new ColorShaderProperty(ColorShaderProperty.deprecatedVersion) });
                }
            }

            // Default Keywords next
            ViewModel.DefaultKeywordNameToAddActionMap.Add("Boolean",  new AddBlackboardItemAction() { blackboardItemReference = new ShaderKeyword(KeywordType.Boolean) });
            ViewModel.DefaultKeywordNameToAddActionMap.Add("Enum",  new AddBlackboardItemAction() { blackboardItemReference = new ShaderKeyword(KeywordType.Enum) });

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
                    ViewModel.BuiltInKeywordNameToAddActionMap.Add(keyword.displayName,  new AddBlackboardItemAction() { blackboardItemReference = keyword.Copy() });
                }
            }
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
                //CreateBlackboardRow(addBlackboardItemAction.blackboardItemReference);
            }

            // Reconstruct view-model first
            // TODO: (would be cool to have some scoping here to see if the action was one that changed the UI or not, could avoid reconstructing the ViewModel based on that)
            InitializeViewModel();

            // Lets all event handlers this controller owns/manages know that the model has changed
            // Usually this is to update views and make them reconstruct themself from updated view-model
            NotifyChange(changeAction);

            // Let child controllers know about changes to this controller so they may update themselves in turn
            foreach (var controller in allChildren)
            {
                controller.ApplyChanges();
            }
        }

        internal void CreateBlackboardRow(BlackboardItem newBlackboardItem)
        {

        }

        void AddBlackboardRow(BlackboardItem blackboardItem)
        {
            // Create BlackboardRowController, give it a field view and Shader Input to manage
            // let it handle all the stuff that comes in AddInputRow()
        }

        static string FormatPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "—";
            return path;
        }

        static string SanitizePath(string path)
        {
            var splitString = path.Split('/');
            List<string> newStrings = new List<string>();
            foreach (string s in splitString)
            {
                var str = s.Trim();
                if (!string.IsNullOrEmpty(str))
                {
                    newStrings.Add(str);
                }
            }

            return string.Join("/", newStrings.ToArray());
        }

        public BlackboardRow GetBlackboardRow(ShaderInput blackboardItem)
        {
            return new BlackboardRow(new VisualElement(), null);
        }

        public override void ApplyChanges()
        {

        }
    }
}
