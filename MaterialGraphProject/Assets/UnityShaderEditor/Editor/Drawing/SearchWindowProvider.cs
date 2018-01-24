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
    class SearchWindowProvider : ScriptableObject, ISearchWindowProvider
    {
        EditorWindow m_EditorWindow;
        AbstractMaterialGraph m_Graph;
        GraphView m_GraphView;
        Texture2D m_Icon;
        public ShaderPort connectedPort { get; set; }
        public bool nodeNeedsRepositioning { get; set; }
        public SlotReference targetSlotReference { get; private set; }
        public Vector2 targetPosition { get; private set; }

        public void Initialize(EditorWindow editorWindow, AbstractMaterialGraph graph, GraphView graphView)
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

        struct NodeEntry
        {
            public string[] title;
            public AbstractMaterialNode node;
            public int compatibleSlotId;
        }

        List<ISlot> m_Slots = new List<ISlot>();

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            // First build up temporary data structure containing group & title as an array of strings (the last one is the actual title) and associated node type.
            var nodeEntries = new List<NodeEntry>();
            foreach (var type in Assembly.GetAssembly(typeof(AbstractMaterialNode)).GetTypes())
            {
                if (type.IsClass && !type.IsAbstract && (type.IsSubclassOf(typeof(AbstractMaterialNode))))
                {
                    var attrs = type.GetCustomAttributes(typeof(TitleAttribute), false) as TitleAttribute[];
                    if (attrs != null && attrs.Length > 0)
                    {
                        var node = (AbstractMaterialNode) Activator.CreateInstance(type);
                        var compatibleSlotId = -1;
                        if (connectedPort != null)
                        {
                            compatibleSlotId = GetFirstCompatibleSlotId(node);
                            if (compatibleSlotId == -1)
                                continue;
                        }
                        nodeEntries.Add(new NodeEntry { title = attrs[0].title, node = node, compatibleSlotId = compatibleSlotId});
                    }
                }
            }

            foreach (var guid in AssetDatabase.FindAssets(string.Format("t:{0}", typeof(MaterialSubGraphAsset))))
            {
                var asset = AssetDatabase.LoadAssetAtPath<MaterialSubGraphAsset>(AssetDatabase.GUIDToAssetPath(guid));
                var node = Activator.CreateInstance<SubGraphNode>();
                node.subGraphAsset = asset;
                var compatibleSlotId = -1;
                if (connectedPort != null)
                {
                    compatibleSlotId = GetFirstCompatibleSlotId(node);
                    if (compatibleSlotId == -1)
                        continue;
                }
                nodeEntries.Add(new NodeEntry
                {
                    title = new[] { "Sub-graph Assets", asset.name },
                    node = node,
                    compatibleSlotId = compatibleSlotId
                });
            }

            // Sort the entries lexicographically by group then title with the requirement that items always comes before sub-groups in the same group.
            // Example result:
            // - Art/BlendMode
            // - Art/Adjustments/ColorBalance
            // - Art/Adjustments/Contrast
            nodeEntries.Sort((entry1, entry2) =>
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
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),
            };

            foreach (var nodeEntry in nodeEntries)
            {
                // `createIndex` represents from where we should add new group entries from the current entry's group path.
                var createIndex = int.MaxValue;

                // Compare the group path of the current entry to the current group path.
                for (var i = 0; i < nodeEntry.title.Length - 1; i++)
                {
                    var group = nodeEntry.title[i];
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
                for (var i = createIndex; i < nodeEntry.title.Length - 1; i++)
                {
                    var group = nodeEntry.title[i];
                    groups.Add(group);
                    tree.Add(new SearchTreeGroupEntry(new GUIContent(group)) { level = i + 1 });
                }

                // Finally, add the actual entry.
                tree.Add(new SearchTreeEntry(new GUIContent(nodeEntry.title.Last(), m_Icon)) { level = nodeEntry.title.Length, userData = nodeEntry });
            }

            return tree;
        }

        int GetFirstCompatibleSlotId(AbstractMaterialNode node)
        {
            var connectedSlot = connectedPort.slot;
            m_Slots.Clear();
            node.GetSlots(m_Slots);
            foreach (var slot in m_Slots)
            {
                var materialSlot = (MaterialSlot)slot;
                if (materialSlot.IsCompatibleWith(connectedSlot))
                {
                    return materialSlot.id;
                }
            }
            return -1;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            var nodeEntry = (NodeEntry)entry.userData;
            var node = nodeEntry.node;

            var drawState = node.drawState;
            var windowMousePosition = m_EditorWindow.GetRootVisualContainer().ChangeCoordinatesTo(m_EditorWindow.GetRootVisualContainer().parent, context.screenMousePosition - m_EditorWindow.position.position);
            var graphMousePosition = m_GraphView.contentViewContainer.WorldToLocal(windowMousePosition);
            drawState.position = new Rect(graphMousePosition, Vector2.zero);
            node.drawState = drawState;

            m_Graph.owner.RegisterCompleteObjectUndo("Add " + node.name);
            m_Graph.AddNode(node);

            if (connectedPort != null)
            {
                var connectedSlot = connectedPort.slot;
                var connectedSlotReference = connectedSlot.owner.GetSlotReference(connectedSlot.id);
                var compatibleSlotReference = node.GetSlotReference(nodeEntry.compatibleSlotId);

                var fromReference = connectedSlot.isOutputSlot ? connectedSlotReference : compatibleSlotReference;
                var toReference = connectedSlot.isOutputSlot ? compatibleSlotReference : connectedSlotReference;
                m_Graph.Connect(fromReference, toReference);

                nodeNeedsRepositioning = true;
                targetSlotReference = compatibleSlotReference;
                targetPosition = graphMousePosition;
            }

            return true;
        }
    }
}
