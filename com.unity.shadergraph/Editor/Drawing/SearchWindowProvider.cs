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
using UnityEngine.Profiling;
using UnityEngine.Pool;

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
        public VisualElement target { get; internal set; }
        public bool regenerateEntries { get; set; }
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
        List<MaterialSlot> m_Slots = new List<MaterialSlot>();

        public void GenerateNodeEntries()
        {
            Profiler.BeginSample("SearchWindowProvider.GenerateNodeEntries");
            // First build up temporary data structure containing group & title as an array of strings (the last one is the actual title) and associated node type.
            List<NodeEntry> nodeEntries = new List<NodeEntry>();

            if (target is ContextView contextView)
            {
                // Iterate all BlockFieldDescriptors currently cached on GraphData
                foreach (var field in m_Graph.blockFieldDescriptors)
                {
                    if (field.isHidden)
                        continue;

                    // Test stage
                    if (field.shaderStage != contextView.contextData.shaderStage)
                        continue;

                    // Create title
                    List<string> title = ListPool<string>.Get();
                    if (!string.IsNullOrEmpty(field.path))
                    {
                        var path = field.path.Split('/').ToList();
                        title.AddRange(path);
                    }
                    title.Add(field.displayName);

                    // Create and initialize BlockNode instance then add entry
                    var node = (BlockNode)Activator.CreateInstance(typeof(BlockNode));
                    node.Init(field);
                    AddEntries(node, title.ToArray(), nodeEntries);
                }

                SortEntries(nodeEntries);
                currentNodeEntries = nodeEntries;
                return;
            }

            foreach (var type in NodeClassCache.knownNodeTypes)
            {
                if ((!type.IsClass || type.IsAbstract)
                    || type == typeof(PropertyNode)
                    || type == typeof(KeywordNode)
                    || type == typeof(SubGraphNode))
                    continue;

                TitleAttribute titleAttribute = NodeClassCache.GetAttributeOnNodeType<TitleAttribute>(type);
                if (titleAttribute != null)
                {
                    var node = (AbstractMaterialNode)Activator.CreateInstance(type);
                    if (ShaderGraphPreferences.allowDeprecatedBehaviors && node.latestVersion > 0)
                    {
                        var versions = node.allowedNodeVersions ?? Enumerable.Range(0, node.latestVersion + 1);
                        bool multiple = (versions.Count() > 1);
                        foreach (int i in versions)
                        {
                            var depNode = (AbstractMaterialNode)Activator.CreateInstance(type);
                            depNode.ChangeVersion(i);
                            if (multiple)
                                AddEntries(depNode, titleAttribute.title.Append($"V{i}").ToArray(), nodeEntries);
                            else
                                AddEntries(depNode, titleAttribute.title, nodeEntries);
                        }
                    }
                    else
                    {
                        AddEntries(node, titleAttribute.title, nodeEntries);
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
                if (property is Serialization.MultiJsonInternal.UnknownShaderPropertyType)
                    continue;

                var node = new PropertyNode();
                node.property = property;
                AddEntries(node, new[] { "Properties", "Property: " + property.displayName }, nodeEntries);
            }
            foreach (var keyword in m_Graph.keywords)
            {
                var node = new KeywordNode();
                node.keyword = keyword;
                AddEntries(node, new[] { "Keywords", "Keyword: " + keyword.displayName }, nodeEntries);
            }

            SortEntries(nodeEntries);
            currentNodeEntries = nodeEntries;
            Profiler.EndSample();
        }

        void SortEntries(List<NodeEntry> nodeEntries)
        {
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
            if (regenerateEntries)
            {
                GenerateNodeEntries();
                regenerateEntries = false;
            }

            //create empty root for searcher tree
            var root = new List<SearcherItem>();
            var dummyEntry = new NodeEntry();

            foreach (var nodeEntry in currentNodeEntries)
            {
                SearcherItem item = null;
                SearcherItem parent = null;
                for (int i = 0; i < nodeEntry.title.Length; i++)
                {
                    var pathEntry = nodeEntry.title[i];
                    List<SearcherItem> children = parent != null ? parent.Children : root;
                    item = children.Find(x => x.Name == pathEntry);

                    if (item == null)
                    {
                        //if we have slot entries and are at a leaf, add the slot name to the entry title
                        if (nodeEntry.compatibleSlotId != -1 && i == nodeEntry.title.Length - 1)
                            item = new SearchNodeItem(pathEntry + ": " + nodeEntry.slotName, nodeEntry, nodeEntry.node.synonyms);
                        //if we don't have slot entries and are at a leaf, add userdata to the entry
                        else if (nodeEntry.compatibleSlotId == -1 && i == nodeEntry.title.Length - 1)
                            item = new SearchNodeItem(pathEntry, nodeEntry, nodeEntry.node.synonyms);
                        //if we aren't a leaf, don't add user data
                        else
                            item = new SearchNodeItem(pathEntry, dummyEntry, null);

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
            if (entry == null || (entry as SearchNodeItem).NodeGUID.node == null)
                return true;

            var nodeEntry = (entry as SearchNodeItem).NodeGUID;

            if (nodeEntry.node is PropertyNode propNode)
                if (propNode.property is Serialization.MultiJsonInternal.UnknownShaderPropertyType)
                    return true;

            var node = CopyNodeForGraph(nodeEntry.node);

            var windowRoot = m_EditorWindow.rootVisualElement;
            var windowMousePosition = windowRoot.ChangeCoordinatesTo(windowRoot.parent, screenMousePosition); //- m_EditorWindow.position.position);
            var graphMousePosition = m_GraphView.contentViewContainer.WorldToLocal(windowMousePosition);

            m_Graph.owner.RegisterCompleteObjectUndo("Add " + node.name);

            if (node is BlockNode blockNode)
            {
                if (!(target is ContextView contextView))
                    return true;

                // Test against all current BlockNodes in the Context
                // Never allow duplicate BlockNodes
                if (contextView.contextData.blocks.Where(x => x.value.name == blockNode.name).FirstOrDefault().value != null)
                    return true;

                // Insert block to Data
                blockNode.owner = m_Graph;
                int index = contextView.GetInsertionIndex(screenMousePosition);
                m_Graph.AddBlock(blockNode, contextView.contextData, index);
                return true;
            }

            var drawState = node.drawState;
            drawState.position = new Rect(graphMousePosition, Vector2.zero);
            node.drawState = drawState;
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

        public AbstractMaterialNode CopyNodeForGraph(AbstractMaterialNode oldNode)
        {
            var newNode = (AbstractMaterialNode)Activator.CreateInstance(oldNode.GetType());
            if (ShaderGraphPreferences.allowDeprecatedBehaviors && oldNode.sgVersion != newNode.sgVersion)
            {
                newNode.ChangeVersion(oldNode.sgVersion);
            }
            if (newNode is SubGraphNode subgraphNode)
            {
                subgraphNode.asset = ((SubGraphNode)oldNode).asset;
            }
            else if (newNode is PropertyNode propertyNode)
            {
                propertyNode.owner = m_Graph;
                propertyNode.property = ((PropertyNode)oldNode).property;
                propertyNode.owner = null;
            }
            else if (newNode is KeywordNode keywordNode)
            {
                keywordNode.owner = m_Graph;
                keywordNode.keyword = ((KeywordNode)oldNode).keyword;
                keywordNode.owner = null;
            }
            else if (newNode is BlockNode blockNode)
            {
                blockNode.owner = m_Graph;
                blockNode.Init(((BlockNode)oldNode).descriptor);
                blockNode.owner = null;
            }
            return newNode;
        }
    }
}
