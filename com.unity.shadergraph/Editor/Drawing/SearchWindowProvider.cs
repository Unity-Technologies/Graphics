using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
#if SEARCHER_PRESENT
using UnityEditor.Searcher;
#endif

namespace UnityEditor.ShaderGraph.Drawing
{
    internal struct NodeEntry
    {
        public string[] title;
        public AbstractMaterialNode node;
        public int compatibleSlotId;
        public string slotName;
    }

    class SearchWindowProvider : ScriptableObject
    {
        internal EditorWindow m_EditorWindow;
        internal GraphData m_Graph;
        internal GraphView m_GraphView;
        internal Texture2D m_Icon;
        public List<NodeEntry> currentNodeEntries;
        public ShaderPort connectedPort { get; set; }
        public bool nodeNeedsRepositioning { get; set; }
        public SlotReference targetSlotReference { get; internal set; }
        public Vector2 targetPosition { get; internal set; }

        public void Initialize(EditorWindow editorWindow, GraphData graph, GraphView graphView)
        {
            m_EditorWindow = editorWindow;
            m_Graph = graph;
            m_GraphView = graphView;
            GenerateNodeEntries();

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
        
        List<int> m_Ids;
        List<ISlot> m_Slots = new List<ISlot>();

        public void GenerateNodeEntries()
        {
            // First build up temporary data structure containing group & title as an array of strings (the last one is the actual title) and associated node type.
            List<NodeEntry> nodeEntries = new List<NodeEntry>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypesOrNothing())
                {
                    if (type.IsClass && !type.IsAbstract && (type.IsSubclassOf(typeof(AbstractMaterialNode)))
                        && type != typeof(PropertyNode)
                        && type != typeof(SubGraphNode))
                    {
                        var attrs = type.GetCustomAttributes(typeof(TitleAttribute), false) as TitleAttribute[];
                        if (attrs != null && attrs.Length > 0)
                        {
                            var node = (AbstractMaterialNode)Activator.CreateInstance(type);
                            AddEntries(node, attrs[0].title, nodeEntries);
                        }
                    }
                }
            }

            foreach (var guid in AssetDatabase.FindAssets(string.Format("t:{0}", typeof(SubGraphAsset))))
            {
                var asset = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(AssetDatabase.GUIDToAssetPath(guid));
                var node = new SubGraphNode { subGraphAsset = asset };
                if (node.subGraphData.descendents.Contains(m_Graph.assetGuid) || node.subGraphData.assetGuid == m_Graph.assetGuid)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(node.subGraphData.path))
                {
                    AddEntries(node, new string[1] { asset.name }, nodeEntries);
                }
                else
                {
                    var title = node.subGraphData.path.Split('/').ToList();
                    title.Add(asset.name);
                    AddEntries(node, title.ToArray(), nodeEntries);
                }
            }

            foreach (var property in m_Graph.properties)
            {
                var node = new PropertyNode();
                node.owner = m_Graph;
                node.propertyGuid = property.guid;
                node.owner = null;
                AddEntries(node, new[] { "Properties", "Property: " + property.displayName }, nodeEntries);
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
                            {
                                //once nodes are sorted, sort slot entries by slot order instead of alphebetically                                 
                                return entry1.compatibleSlotId.CompareTo(entry2.compatibleSlotId);
                            }                                                         
                            
                            return value;
                        }
                    }
                    return 0;
                });

            
            currentNodeEntries = nodeEntries;
        }       

        void AddEntries(AbstractMaterialNode node, string[] title, List<NodeEntry> addNodeEntries)
        {
            if (m_Graph.isSubGraph && !node.allowedInSubGraph)
                return;
            if (!m_Graph.isSubGraph && !node.allowedInMainGraph)
                return;
            if (connectedPort == null)
            {
                addNodeEntries.Add(new NodeEntry
                {
                    node = node,
                    title = title,
                    compatibleSlotId = -1
                });
                return;
            }

            var connectedSlot = connectedPort.slot;
            m_Slots.Clear();
            node.GetSlots(m_Slots);
            var hasSingleSlot = m_Slots.Count(s => s.isOutputSlot != connectedSlot.isOutputSlot) == 1;
            m_Slots.RemoveAll(slot =>
                {
                    var materialSlot = (MaterialSlot)slot;
                    return !materialSlot.IsCompatibleWith(connectedSlot);
                });

            m_Slots.RemoveAll(slot =>
                {
                    var materialSlot = (MaterialSlot)slot;
                    return !materialSlot.IsCompatibleStageWith(connectedSlot);
                });
            
            foreach (var slot in m_Slots)
            {
                //var entryTitle = new string[title.Length];
                //title.CopyTo(entryTitle, 0);
                //entryTitle[entryTitle.Length - 1] += ": " + slot.displayName;
                addNodeEntries.Add(new NodeEntry
                {
                    title = title,
                    node = node,
                    compatibleSlotId = slot.id,
                    slotName = slot.displayName
                });
            }
        }
    }
    #if SEARCHER_PRESENT
    class SearcherProvider : SearchWindowProvider
    {
        public SearchWindowAdapter searcherAdapter;
        public void InitializeSearcher(EditorWindow editorWindow, GraphData graph, GraphView graphView)
        {
            searcherAdapter = new SearchWindowAdapter("Create Node");
        }
        
        public Searcher.Searcher LoadSearchWindow()
        {
            GenerateNodeEntries();

            //create empty root for searcher tree 
            var root = new List<SearcherItem>();
            
            foreach (var nodeEntry in currentNodeEntries)
            {
                SearcherItem item = null;
                SearcherItem parent = null;
                for(int i = 0; i < nodeEntry.title.Length; i++)
                {
                    var pathEntry = nodeEntry.title[i];
                    List<SearcherItem> children = parent != null ? parent.Children : root;
                    item = children.Find(x => x.Name == pathEntry);

                    if (item == null)
                    {
                        //if we have slot entries and are at a leaf, add the slot name to the entry title
                        if (nodeEntry.compatibleSlotId != -1 && i == nodeEntry.title.Length - 1)
                            item = new SearchNodeItem(pathEntry + ": " + nodeEntry.slotName, nodeEntry);
                        else
                            item = new SearchNodeItem(pathEntry, nodeEntry);

                        if (parent != null)
                        {
                            parent.AddChild(item);
                        }
                        else
                        {
                            children.Add(item);
                        }
                    }

                    parent = item;

                    if (parent.Depth == 0 && !root.Contains(parent))
                        root.Add(parent);
                }
                
            }

            var nodeDatabase = SearcherDatabase.Create(root, string.Empty, false);
            
            return new Searcher.Searcher(nodeDatabase, searcherAdapter);             
        }
        public bool OnSearcherSelectEntry(SearcherItem entry, Vector2 screenMousePosition)
        {
            if(entry == null)
                return false;
           
            var nodeEntry = (entry as SearchNodeItem).UserData;
            var node = nodeEntry.node;

            var drawState = node.drawState;


            var windowRoot = m_EditorWindow.rootVisualElement;
            var windowMousePosition = windowRoot.ChangeCoordinatesTo(windowRoot.parent, screenMousePosition );//- m_EditorWindow.position.position);
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

    #else
    class FallbackSearchProvider : SearchWindowProvider, ISearchWindowProvider
    {
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            GenerateNodeEntries();
            //* Build up the data structure needed by SearchWindow.

            // `groups` contains the current group path we're in.
            var groups = new List<string>();

            // First item in the tree is the title of the window.
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),
            };

            foreach (var nodeEntry in currentNodeEntries)
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
                //if item is a leaf and has different slot ids, add slot name 
                if (nodeEntry.compatibleSlotId != -1)
                    tree.Add(new SearchTreeEntry(new GUIContent(nodeEntry.title.Last() + ": " + nodeEntry.slotName, m_Icon)) { level = nodeEntry.title.Length, userData = nodeEntry });
                else
                    tree.Add(new SearchTreeEntry(new GUIContent(nodeEntry.title.Last(), m_Icon)) { level = nodeEntry.title.Length, userData = nodeEntry });
            }

            return tree;
        }
        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            var nodeEntry = (NodeEntry)entry.userData;
            var node = nodeEntry.node;

            var drawState = node.drawState;


            var windowRoot = m_EditorWindow.rootVisualElement;
            var windowMousePosition = windowRoot.ChangeCoordinatesTo(windowRoot.parent, context.screenMousePosition - m_EditorWindow.position.position);
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
    #endif
    
}
