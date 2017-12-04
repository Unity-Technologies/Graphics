using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using INode = UnityEditor.Graphing.INode;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class SearchWindowProvider : ScriptableObject, ISearchWindowProvider
    {
        EditorWindow m_EditorWindow;
        IGraph m_Graph;
        GraphView m_GraphView;
        Texture2D m_Icon;

        const string k_Actions = "Actions";
        const string k_AddNode = "Add Node";
        const string k_ConvertToProperty = "Convert To Property";
        const string k_ConvertToInlineNode = "Convert To Inline Node";

        public void Initialize(EditorWindow editorWindow, IGraph graph, GraphView graphView)
        {
            m_EditorWindow = editorWindow;
            m_Graph = graph;
            m_GraphView = graphView;

            // Transparent icon to trick search window into indenting items
            m_Icon = new Texture2D(1, 1);
            m_Icon.SetPixel(0, 0, new Color(0, 0, 0, 0));
            m_Icon.Apply();
        }

        void OnDestroy()
        {
            if (m_Icon != null)
            {
                DestroyImmediate(m_Icon);
                m_Icon = null;
            }
        }

        struct NestedEntry
        {
            public string[] title;
            public object userData;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            // First build up temporary data structure containing group & title as an array of strings (the last one is the actual title) and associated node type.
            var nestedEntries = new List<NestedEntry>();
            foreach (var type in Assembly.GetAssembly(typeof(AbstractMaterialNode)).GetTypes())
            {
                if (type.IsClass && !type.IsAbstract && (type.IsSubclassOf(typeof(AbstractMaterialNode))))
                {
                    var attrs = type.GetCustomAttributes(typeof(TitleAttribute), false) as TitleAttribute[];
                    if (attrs != null && attrs.Length > 0)
                        nestedEntries.Add(new NestedEntry { title = attrs[0].title, userData = type });
                }
            }

            foreach (var guid in AssetDatabase.FindAssets(string.Format("t:{0}", typeof(MaterialSubGraphAsset))))
            {
                var asset = AssetDatabase.LoadAssetAtPath<MaterialSubGraphAsset>(AssetDatabase.GUIDToAssetPath(guid));
                nestedEntries.Add(new NestedEntry
                {
                    title = new [] {"Sub-graph Assets", asset.name},
                    userData = asset
                });
            }

            // Sort the entries lexicographically by group then title with the requirement that items always comes before sub-groups in the same group.
            // Example result:
            // - Art/BlendMode
            // - Art/Adjustments/ColorBalance
            // - Art/Adjustments/Contrast
            nestedEntries.Sort((entry1, entry2) =>
            {
                for (var i = 0; i < entry1.title.Length; i++)
                {
                    if (i >= entry2.title.Length)
                        return 1;
                    var value = entry1.title[i].CompareTo(entry2.title[i]);
                    if (value != 0)
                    {
                        // Make sure that leaves go before nodes
                        if (entry1.title.Length != entry2.title.Length && (i == entry1.title.Length - 1 || i == entry2.title.Length - 1))
                            return entry1.title.Length < entry2.title.Length ? -1 : 1;
                        return value;
                    }
                }
                return 0;
            });

            //* Build up the data structure needed by SearchWindow.

            // `groups` contains the current group path we're in.
            var groups = new List<string>();

            // First item in the tree is the title of the window.
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent(k_Actions), 0),
                new SearchTreeGroupEntry(new GUIContent(k_AddNode)) { level = 1 },
            };

            // Add in contextual node actions
            var selection = m_GraphView.selection.OfType<MaterialNodeView>().ToList();
            if (selection.Any(v => v.node is IPropertyFromNode))
                tree.Add(new SearchTreeEntry(new GUIContent(k_ConvertToProperty, m_Icon)) { level = 1 });
            if (selection.Any(v => v.node is PropertyNode))
                tree.Add(new SearchTreeEntry(new GUIContent(k_ConvertToInlineNode, m_Icon)) { level = 1 });

            foreach (var nestedEntry in nestedEntries)
            {
                // `createIndex` represents from where we should add new group entries from the current entry's group path.
                var createIndex = int.MaxValue;

                // Compare the group path of the current entry to the current group path.
                for (var i = 0; i < nestedEntry.title.Length - 1; i++)
                {
                    var group = nestedEntry.title[i];
                    if (i >= groups.Count)
                    {
                        // The current group path matches a prefix of the current entry's group path, so we add the
                        // rest of the group path from the currrent entry.
                        createIndex = i;
                        break;
                    }
                    if (groups[i] != group)
                    {
                        // A prefix of the current group path matches a prefix of the current entry's group path,
                        // so we remove everyfrom from the point where it doesn't match anymore, and then add the rest
                        // of the group path from the current entry.
                        groups.RemoveRange(i, groups.Count - i);
                        createIndex = i;
                        break;
                    }
                }

                // Create new group entries as needed.
                // If we don't need to modify the group path, `createIndex` will be `int.MaxValue` and thus the loop won't run.
                for (var i = createIndex; i < nestedEntry.title.Length - 1; i++)
                {
                    var group = nestedEntry.title[i];
                    groups.Add(group);
                    tree.Add(new SearchTreeGroupEntry(new GUIContent(group)) { level = i + 2 });
                }

                // Finally, add the actual entry.
                tree.Add(new SearchTreeEntry(new GUIContent(nestedEntry.title.Last(), m_Icon)) { level = nestedEntry.title.Length + 1, userData = nestedEntry.userData });
            }

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if (entry.name == k_ConvertToProperty)
                return OnConvertToProperty();
            if (entry.name == k_ConvertToInlineNode)
                return OnConvertToInlineNode();
            return OnAddNode(entry, context);
        }

        bool OnConvertToProperty()
        {
            if (m_GraphView == null)
                return false;

            var graph = m_Graph as AbstractMaterialGraph;
            if (graph == null)
                return false;

            var selectedNodeViews = m_GraphView.selection.OfType<MaterialNodeView>().Select(x => x.node);

            foreach (var node in selectedNodeViews.ToArray())
            {
                if (!(node is IPropertyFromNode))
                    continue;

                var converter = node as IPropertyFromNode;
                var prop = converter.AsShaderProperty();
                graph.AddShaderProperty(prop);

                var propNode = new PropertyNode();
                propNode.drawState = node.drawState;
                graph.AddNode(propNode);
                propNode.propertyGuid = prop.guid;

                var oldSlot = node.FindSlot<MaterialSlot>(converter.outputSlotId);
                var newSlot = propNode.FindSlot<MaterialSlot>(PropertyNode.OutputSlotId);

                var edges = graph.GetEdges(oldSlot.slotReference).ToArray();
                foreach (var edge in edges)
                    graph.Connect(newSlot.slotReference, edge.inputSlot);

                graph.RemoveNode(node);
            }

            return true;
        }

        bool OnConvertToInlineNode()
        {
            if (m_GraphView == null)
                return false;

            var selectedNodeViews = m_GraphView.selection.OfType<MaterialNodeView>()
                .Select(x => x.node)
                .OfType<PropertyNode>();

            foreach (var propNode in selectedNodeViews)
                ((AbstractMaterialGraph)propNode.owner).ReplacePropertyNodeWithConcreteNode(propNode);

            return true;
        }

        bool OnAddNode(SearchTreeEntry entry, SearchWindowContext context)
        {
            Type type;
            var asset = entry.userData as MaterialSubGraphAsset;
            if (asset != null)
                type = typeof(SubGraphNode);
            else
                type = (Type)entry.userData;

            var node = Activator.CreateInstance(type) as INode;
            if (node == null)
                return false;

            var drawState = node.drawState;
            var windowMousePosition = context.screenMousePosition - m_EditorWindow.position.position;
            var graphMousePosition = m_EditorWindow.GetRootVisualContainer().ChangeCoordinatesTo(m_GraphView.contentViewContainer, windowMousePosition);
            drawState.position = new Rect(graphMousePosition, Vector2.zero);
            node.drawState = drawState;

            if (asset != null)
            {
                var subgraphNode = (SubGraphNode)node;
                subgraphNode.subGraphAsset = asset;
            }

            m_Graph.owner.RegisterCompleteObjectUndo("Add " + node.name);
            m_Graph.AddNode(node);

            return true;
        }
    }
}
