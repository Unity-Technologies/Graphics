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
using UnityEditor.Searcher;

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
        private const string k_HiddenFolderName = "Hidden";

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
                        && type != typeof(KeywordNode)
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
                var node = new SubGraphNode { asset = asset };
                var title = asset.path.Split('/').ToList();
                
                if (asset.descendents.Contains(m_Graph.assetGuid) || asset.assetGuid == m_Graph.assetGuid)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(asset.path))
                {
                    AddEntries(node, new string[1] { asset.name }, nodeEntries);
                }

                else if (title[0] != k_HiddenFolderName)
                {
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
            foreach (var keyword in m_Graph.keywords)
            {
                var node = new KeywordNode();
                node.owner = m_Graph;
                node.keywordGuid = keyword.guid;
                node.owner = null;
                AddEntries(node, new[] { "Keywords", "Keyword: " + keyword.displayName }, nodeEntries);
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
                                var alphaOrder = entry1.title.Length < entry2.title.Length ? -1 : 1;
                                var slotOrder = entry1.compatibleSlotId.CompareTo(entry2.compatibleSlotId);                     
                                return alphaOrder.CompareTo(slotOrder);
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
    class SearcherProvider : SearchWindowProvider
    {        
        public Searcher.Searcher LoadSearchWindow()
        {
            GenerateNodeEntries();

            //create empty root for searcher tree 
            var root = new List<SearcherItem>();
            var dummyEntry = new NodeEntry();
            
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
                        //if we don't have slot entries and are at a leaf, add userdata to the entry
                        else if (nodeEntry.compatibleSlotId == -1 && i == nodeEntry.title.Length - 1)
                            item = new SearchNodeItem(pathEntry, nodeEntry);
                        //if we aren't a leaf, don't add user data
                        else
                            item = new SearchNodeItem(pathEntry, dummyEntry);

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
            
            return new Searcher.Searcher(nodeDatabase, new SearchWindowAdapter("Create Node"));             
        }
        public bool OnSearcherSelectEntry(SearcherItem entry, Vector2 screenMousePosition)
        {
            if(entry == null || (entry as SearchNodeItem).NodeGUID.node == null)
                return false;
           
            var nodeEntry = (entry as SearchNodeItem).NodeGUID;
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
    
}
