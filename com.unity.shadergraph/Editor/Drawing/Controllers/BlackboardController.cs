using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using GraphDataStore = UnityEditor.ShaderGraph.DataStore<UnityEditor.ShaderGraph.GraphData>;

namespace UnityEditor.ShaderGraph.Drawing
{
    // Need some way to provide arguments to these actions so they have context, like type of blackboard item to add, etc
    class AddBlackboardItemAction : IGraphDataAction
    {
        Type m_BlackboardItemType;

        public GraphData MutateGraphData(GraphData m_graphData)
        {
            return m_graphData;
        }
    }

    class MoveBlackboardItemAction : IGraphDataAction
    {
        public GraphData MutateGraphData(GraphData m_graphData)
        {
            return m_graphData;
        }
    }

    class RemoveBlackboardItemAction : IGraphDataAction
    {
        public GraphData MutateGraphData(GraphData m_graphData)
        {
            return m_graphData;
        }
    }

    class BlackboardController : SGViewController<BlackboardViewModel>
    {
        public class Changes
        {
            public const int AddBlackboardItem = 0;
            public const int MoveBlackboardItem = 1;
            public const int RemoveBlackboardItem = 2;
        }

        static const IList<Type> k_shaderInputTypes = TypeCache.GetTypesWithAttribute<BlackboardInputInfo>().ToList();

        VisualElement m_Blackboard;

        GenericMenu m_AddPropertyMenu;

        public BlackboardController(BlackboardViewModel viewModel, GraphDataStore graphDataStore, VisualElement parentVisualElement)
            : base(viewModel, graphDataStore)
        {
            /*m_Blackboard = new SGBlackboard(parentVisualElement)
            {
                subTitle = FormatPath(graphDataStore.State.path),
                addItemRequested = () => ChangeModel(Changes.AddBlackboardItem),
                moveItemRequested = (newIndex, itemVisualElement) => ChangeModel(Changes.MoveBlackboardItem)
            };*/


            PopulateAddPropertyMenu();
        }

        void PopulateAddPropertyMenu()
        {
            m_AddPropertyMenu = new GenericMenu();
            AddPropertyItems(m_AddPropertyMenu);
            AddKeywordItems(m_AddPropertyMenu);
        }

        void AddPropertyItems(GenericMenu gm)
        {
            // Sort the ShaderInput by priority using the BlackboardInputInfo attribute
            k_shaderInputTypes.Sort((s1, s2) => {
                var info1 = Attribute.GetCustomAttribute(s1, typeof(BlackboardInputInfo)) as BlackboardInputInfo;
                var info2 = Attribute.GetCustomAttribute(s2, typeof(BlackboardInputInfo)) as BlackboardInputInfo;

                if (info1.priority == info2.priority)
                    return (info1.name ?? s1.Name).CompareTo(info2.name ?? s2.Name);
                else
                    return info1.priority.CompareTo(info2.priority);
            });

            foreach (var t in shaderInputTypes)
            {
                if (t.IsAbstract)
                    continue;

                var info = Attribute.GetCustomAttribute(t, typeof(BlackboardInputInfo)) as BlackboardInputInfo;
                string name = info?.name ?? ObjectNames.NicifyVariableName(t.Name.Replace("ShaderProperty", ""));
                // This is so bad, why do we need to create an instance of every type and hold onto it if we're going to throw away most of them?! Just keep the type info and instantiate on demand later instead of feeding actual instance to delegate
                gm.AddItem(new GUIContent(name), false, () => AddInputRow(si, true));
                //QUICK FIX TO DEAL WITH DEPRECATED COLOR PROPERTY
                if (ShaderGraphPreferences.allowDeprecatedBehaviors && si is ColorShaderProperty csp)
                {
                    gm.AddItem(new GUIContent($"Color (Deprecated)"), false, () => AddInputRow(new ColorShaderProperty(ColorShaderProperty.deprecatedVersion), true));
                }
            }
            gm.AddSeparator($"/");
        }

        void AddKeywordItems(GenericMenu gm)
        {
            gm.AddItem(new GUIContent($"Keyword/Boolean"), false, () => AddInputRow(new ShaderKeyword(KeywordType.Boolean), true));
            gm.AddItem(new GUIContent($"Keyword/Enum"), false, () => AddInputRow(new ShaderKeyword(KeywordType.Enum), true));
            gm.AddSeparator($"Keyword/");
            foreach (var builtinKeywordDescriptor in KeywordUtil.GetBuiltinKeywordDescriptors())
            {
                var keyword = ShaderKeyword.CreateBuiltInKeyword(builtinKeywordDescriptor);
                AddBuiltinKeyword(gm, keyword);
            }
        }

        void AddBuiltinKeyword(GenericMenu gm, ShaderKeyword keyword)
        {
            if (m_Graph.keywords.Where(x => x.referenceName == keyword.referenceName).Any())
            {
                gm.AddDisabledItem(new GUIContent($"Keyword/{keyword.displayName}"));
            }
            else
            {
                gm.AddItem(new GUIContent($"Keyword/{keyword.displayName}"), false, () => AddInputRow(keyword.Copy(), true));
            }
        }

        protected override void ChangeModel(int changeID)
        {
            switch (changeID)
            {
                case Changes.AddBlackboardItem:
                    graphDataStore.Dispatch(new AddBlackboardItemAction());
                    break;
                case Changes.MoveBlackboardItem:
                    graphDataStore.Dispatch(new MoveBlackboardItemAction());
                    break;
                default:
                    Debug.Log("ERROR: BlackboardController: Unhandled Model Change Requested.");
                    break;
            }
        }

        /*void MoveItemRequested(int newIndex, VisualElement visualElement)
        {
            var input = visualElement.userData as ShaderInput;
            if (input == null)
                return;

            m_Graph.owner.RegisterCompleteObjectUndo("Move Graph Input");
            switch (input)
            {
                case AbstractShaderProperty property:
                    m_Graph.MoveProperty(property, newIndex);
                    break;
                case ShaderKeyword keyword:
                    m_Graph.MoveKeyword(keyword, newIndex);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void AddItemRequested()
        {
            var gm = new GenericMenu();
            AddPropertyItems(gm);
            AddKeywordItems(gm);
            gm.ShowAsContext();
        }*/

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

        public override void ApplyChanges()
        {

        }

        public override void ModelChanged(GraphData graphData)
        {
            base.ModelChanged(graphData);


            // Do stuff
        }
    }
}
