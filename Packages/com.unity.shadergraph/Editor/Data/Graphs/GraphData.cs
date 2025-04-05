using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.ShaderGraph.Serialization;
using UnityEditor.ShaderGraph.Drawing;
using Edge = UnityEditor.Graphing.Edge;

using UnityEngine.UIElements;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.MaterialGraph")]
    [FormerName("UnityEditor.ShaderGraph.SubGraph")]
    [FormerName("UnityEditor.ShaderGraph.AbstractMaterialGraph")]
    sealed partial class GraphData : JsonObject
    {
        public override int latestVersion => 3;

        public GraphObject owner { get; set; }

        [NonSerialized]
        internal bool graphIsConcretizing = false;

        #region Input data

        [SerializeField]
        List<JsonData<AbstractShaderProperty>> m_Properties = new List<JsonData<AbstractShaderProperty>>();

        public DataValueEnumerable<AbstractShaderProperty> properties => m_Properties.SelectValue();

        [SerializeField]
        List<JsonData<ShaderKeyword>> m_Keywords = new List<JsonData<ShaderKeyword>>();

        public DataValueEnumerable<ShaderKeyword> keywords => m_Keywords.SelectValue();

        [SerializeField]
        List<JsonData<ShaderDropdown>> m_Dropdowns = new List<JsonData<ShaderDropdown>>();

        public DataValueEnumerable<ShaderDropdown> dropdowns => m_Dropdowns.SelectValue();

        [NonSerialized]
        List<ShaderInput> m_AddedInputs = new List<ShaderInput>();

        public IEnumerable<ShaderInput> addedInputs
        {
            get { return m_AddedInputs; }
        }

        [NonSerialized]
        List<ShaderInput> m_RemovedInputs = new List<ShaderInput>();

        public IEnumerable<ShaderInput> removedInputs
        {
            get { return m_RemovedInputs; }
        }

        [NonSerialized]
        List<ShaderInput> m_MovedInputs = new List<ShaderInput>();

        public IEnumerable<ShaderInput> movedInputs
        {
            get { return m_MovedInputs; }
        }

        [NonSerialized]
        List<CategoryData> m_AddedCategories = new List<CategoryData>();

        public IEnumerable<CategoryData> addedCategories
        {
            get { return m_AddedCategories; }
        }

        [NonSerialized]
        List<CategoryData> m_RemovedCategories = new List<CategoryData>();

        public IEnumerable<CategoryData> removedCategories
        {
            get { return m_RemovedCategories; }
        }

        [NonSerialized]
        List<CategoryData> m_MovedCategories = new List<CategoryData>();

        public IEnumerable<CategoryData> movedCategories
        {
            get { return m_MovedCategories; }
        }

        [NonSerialized]
        bool m_MovedContexts = false;
        public bool movedContexts => m_MovedContexts;

        public string assetGuid { get; set; }

        #endregion

        #region Category Data

        [SerializeField]
        List<JsonData<CategoryData>> m_CategoryData = new List<JsonData<CategoryData>>();

        public DataValueEnumerable<CategoryData> categories => m_CategoryData.SelectValue();

        #endregion

        #region Node data

        [SerializeField]
        List<JsonData<AbstractMaterialNode>> m_Nodes = new List<JsonData<AbstractMaterialNode>>();

        [NonSerialized]
        Dictionary<string, AbstractMaterialNode> m_NodeDictionary = new Dictionary<string, AbstractMaterialNode>();

        [NonSerialized]
        Dictionary<string, AbstractMaterialNode> m_LegacyUpdateDictionary = new Dictionary<string, AbstractMaterialNode>();

        public IEnumerable<T> GetNodes<T>()
        {
            return m_Nodes.SelectValue().OfType<T>();
        }

        [NonSerialized]
        List<AbstractMaterialNode> m_AddedNodes = new List<AbstractMaterialNode>();

        public IEnumerable<AbstractMaterialNode> addedNodes
        {
            get { return m_AddedNodes; }
        }

        [NonSerialized]
        List<AbstractMaterialNode> m_RemovedNodes = new List<AbstractMaterialNode>();

        public IEnumerable<AbstractMaterialNode> removedNodes
        {
            get { return m_RemovedNodes; }
        }

        [NonSerialized]
        List<AbstractMaterialNode> m_PastedNodes = new List<AbstractMaterialNode>();

        public IEnumerable<AbstractMaterialNode> pastedNodes
        {
            get { return m_PastedNodes; }
        }
        #endregion

        #region Group Data

        [SerializeField]
        List<JsonData<GroupData>> m_GroupDatas = new List<JsonData<GroupData>>();

        public DataValueEnumerable<GroupData> groups
        {
            get { return m_GroupDatas.SelectValue(); }
        }

        [NonSerialized]
        List<GroupData> m_AddedGroups = new List<GroupData>();

        public IEnumerable<GroupData> addedGroups
        {
            get { return m_AddedGroups; }
        }

        [NonSerialized]
        List<GroupData> m_RemovedGroups = new List<GroupData>();

        public IEnumerable<GroupData> removedGroups
        {
            get { return m_RemovedGroups; }
        }

        [NonSerialized]
        List<GroupData> m_PastedGroups = new List<GroupData>();

        public IEnumerable<GroupData> pastedGroups
        {
            get { return m_PastedGroups; }
        }

        [NonSerialized]
        List<ParentGroupChange> m_ParentGroupChanges = new List<ParentGroupChange>();

        public IEnumerable<ParentGroupChange> parentGroupChanges
        {
            get { return m_ParentGroupChanges; }
        }

        [NonSerialized]
        GroupData m_MostRecentlyCreatedGroup;

        public GroupData mostRecentlyCreatedGroup => m_MostRecentlyCreatedGroup;

        [NonSerialized]
        Dictionary<JsonRef<GroupData>, List<IGroupItem>> m_GroupItems = new Dictionary<JsonRef<GroupData>, List<IGroupItem>>();

        public IEnumerable<IGroupItem> GetItemsInGroup(GroupData groupData)
        {
            if (m_GroupItems.TryGetValue(groupData, out var nodes))
            {
                return nodes;
            }
            return Enumerable.Empty<IGroupItem>();
        }

        #endregion

        #region StickyNote Data
        [SerializeField]
        List<JsonData<StickyNoteData>> m_StickyNoteDatas = new List<JsonData<StickyNoteData>>();

        public DataValueEnumerable<StickyNoteData> stickyNotes => m_StickyNoteDatas.SelectValue();

        [NonSerialized]
        List<StickyNoteData> m_AddedStickyNotes = new List<StickyNoteData>();

        public List<StickyNoteData> addedStickyNotes => m_AddedStickyNotes;

        [NonSerialized]
        List<StickyNoteData> m_RemovedNotes = new List<StickyNoteData>();

        public IEnumerable<StickyNoteData> removedNotes => m_RemovedNotes;

        [NonSerialized]
        List<StickyNoteData> m_PastedStickyNotes = new List<StickyNoteData>();

        public IEnumerable<StickyNoteData> pastedStickyNotes => m_PastedStickyNotes;

        #endregion

        #region Edge data

        [SerializeField]
        List<Edge> m_Edges = new List<Edge>();

        public IEnumerable<Edge> edges => m_Edges;

        [NonSerialized]
        Dictionary<string, List<IEdge>> m_NodeEdges = new Dictionary<string, List<IEdge>>();

        [NonSerialized]
        List<IEdge> m_AddedEdges = new List<IEdge>();

        public IEnumerable<IEdge> addedEdges
        {
            get { return m_AddedEdges; }
        }

        [NonSerialized]
        List<IEdge> m_RemovedEdges = new List<IEdge>();

        public IEnumerable<IEdge> removedEdges
        {
            get { return m_RemovedEdges; }
        }

        #endregion

        #region Context Data

        [SerializeField]
        ContextData m_VertexContext;

        [SerializeField]
        ContextData m_FragmentContext;

        // We build this once and cache it as it uses reflection
        // This list is used to build the Create Node menu entries for Blocks
        // as well as when deserializing descriptor fields on serialized Blocks
        [NonSerialized]
        List<BlockFieldDescriptor> m_BlockFieldDescriptors;

        public ContextData vertexContext => m_VertexContext;
        public ContextData fragmentContext => m_FragmentContext;
        public List<BlockFieldDescriptor> blockFieldDescriptors => m_BlockFieldDescriptors;

        #endregion

        [SerializeField]
        InspectorPreviewData m_PreviewData = new InspectorPreviewData();

        public InspectorPreviewData previewData
        {
            get { return m_PreviewData; }
            set { m_PreviewData = value; }
        }

        [SerializeField]
        string m_Path;

        public string path
        {
            get { return m_Path; }
            set
            {
                if (m_Path == value)
                    return;
                m_Path = value;
                if (owner != null)
                    owner.RegisterCompleteObjectUndo("Change Path");
            }
        }

        public MessageManager messageManager { get; set; }
        public bool isSubGraph { get; set; }

        // we default this to Graph for subgraphs
        // but for shadergraphs, this will get replaced with Single
        [SerializeField]
        private GraphPrecision m_GraphPrecision = GraphPrecision.Graph;

        public GraphPrecision graphDefaultPrecision
        {
            get
            {
                // shader graphs are not allowed to have graph precision
                // we force them to Single if they somehow get set to graph
                if ((!isSubGraph) && (m_GraphPrecision == GraphPrecision.Graph))
                    return GraphPrecision.Single;

                return m_GraphPrecision;
            }
        }

        public ConcretePrecision graphDefaultConcretePrecision
        {
            get
            {
                // when in "Graph switchable" mode, we choose Half as the default concrete precision
                // so you can visualize the worst-case
                return graphDefaultPrecision.ToConcrete(ConcretePrecision.Half);
            }
        }

        // Some state has been changed that requires checking for the auto add/removal of blocks.
        // This needs to be checked at a later point in time so actions like replace (remove + add) don't remove blocks.
        internal bool checkAutoAddRemoveBlocks { get; set; }

        public void SetGraphDefaultPrecision(GraphPrecision newGraphDefaultPrecision)
        {
            if ((!isSubGraph) && (newGraphDefaultPrecision == GraphPrecision.Graph))
            {
                // shader graphs can't be set to "Graph", only subgraphs can
                Debug.LogError("Cannot set ShaderGraph to a default precision of Graph");
            }
            else
            {
                m_GraphPrecision = newGraphDefaultPrecision;
            }
        }

        // NOTE: having preview mode default to 3D preserves the old behavior of pre-existing subgraphs
        // if we change this, we would have to introduce a versioning step if we want to maintain the old behavior
        [SerializeField]
        private PreviewMode m_PreviewMode = PreviewMode.Preview3D;

        public PreviewMode previewMode
        {
            get => m_PreviewMode;
            set => m_PreviewMode = value;
        }

        [SerializeField]
        JsonRef<AbstractMaterialNode> m_OutputNode;

        public AbstractMaterialNode outputNode
        {
            get => m_OutputNode;
            set => m_OutputNode = value;
        }

        internal delegate void SaveGraphDelegate(Shader shader, object context);
        internal static SaveGraphDelegate onSaveGraph;


        #region SubData

        [SerializeField]
        internal List<JsonData<AbstractShaderGraphDataExtension>> m_SubDatas = new List<JsonData<AbstractShaderGraphDataExtension>>();
        public DataValueEnumerable<AbstractShaderGraphDataExtension> SubDatas => m_SubDatas.SelectValue();

        #endregion

        #region Targets

        // Serialized list of user-selected active targets, sorted in displayName order (to maintain deterministic serialization order)
        // some of these may be MultiJsonInternal.UnknownTargetType if we can't recognize the type of the target
        [SerializeField]
        internal List<JsonData<Target>> m_ActiveTargets = new List<JsonData<Target>>();      // After adding to this list, you MUST call SortActiveTargets()
        public DataValueEnumerable<Target> activeTargets => m_ActiveTargets.SelectValue();

        // this stores all of the current possible Target types (including any unknown target types we serialized in)
        class PotentialTarget
        {
            // the potential Target
            Target m_Target;

            // a Target is either known (we know the Type) or unknown (can't find a matching definition of the Type)
            // Targets of unknown type are stored in an UnknownTargetType
            private Type m_KnownType;
            private MultiJsonInternal.UnknownTargetType m_UnknownTarget;

            public PotentialTarget(Target target)
            {
                m_Target = target;

                if (target is MultiJsonInternal.UnknownTargetType)
                {
                    m_UnknownTarget = (MultiJsonInternal.UnknownTargetType)target;
                    m_KnownType = null;
                }
                else
                {
                    m_UnknownTarget = null;
                    m_KnownType = target.GetType();
                }
            }

            public bool IsUnknown()
            {
                return m_UnknownTarget != null;
            }

            public MultiJsonInternal.UnknownTargetType GetUnknown()
            {
                return m_UnknownTarget;
            }

            public Type knownType { get { return m_KnownType; } }

            public bool Is(Target t)
            {
                return t == m_Target;
            }

            public string GetDisplayName()
            {
                return m_Target.displayName;
            }

            public void ReplaceStoredTarget(Target t)
            {
                if (m_KnownType != null)
                    Assert.IsTrue(t.GetType() == m_KnownType);
                m_Target = t;
            }

            public Target GetTarget()
            {
                return m_Target;
            }
        }
        [NonSerialized]
        List<PotentialTarget> m_AllPotentialTargets = new List<PotentialTarget>();
        public IEnumerable<Target> allPotentialTargets => m_AllPotentialTargets.Select(x => x.GetTarget());

        public int GetTargetIndexByKnownType(Type targetType)
        {
            return m_AllPotentialTargets.FindIndex(pt => pt.knownType == targetType);
        }

        public int GetTargetIndex(Target t)
        {
            int result = m_AllPotentialTargets.FindIndex(pt => pt.Is(t));
            return result;
        }

        public IEnumerable<Target> GetValidTargets()
        {
            foreach (var potentialTarget in m_AllPotentialTargets)
            {
                var target = potentialTarget.GetTarget();
                if (target is IMayObsolete targetObsolete && targetObsolete.IsObsolete())
                    continue;
                yield return potentialTarget.GetTarget();
            }
        }

        public void SetTargetActive(Target target, bool skipSortAndUpdate = false)
        {
            int activeIndex = m_ActiveTargets.IndexOf(target);
            if (activeIndex < 0)
            {
                activeIndex = m_ActiveTargets.Count;
                m_ActiveTargets.Add(target);
            }

            // active known targets should replace the stored Target in AllPotentialTargets
            if (target is MultiJsonInternal.UnknownTargetType unknownTarget)
            {
                // find any existing potential target with the same unknown jsonData
                int targetIndex = m_AllPotentialTargets.FindIndex(
                    pt => pt.IsUnknown() && (pt.GetUnknown().jsonData == unknownTarget.jsonData));

                // replace existing target, or add it if there is none
                if (targetIndex >= 0)
                    m_AllPotentialTargets[targetIndex] = new PotentialTarget(target);
                else
                    m_AllPotentialTargets.Add(new PotentialTarget(target));
            }
            else
            {
                // known types should already have been registered
                Type targetType = target.GetType();
                int targetIndex = GetTargetIndexByKnownType(targetType);
                Assert.IsTrue(targetIndex >= 0);
                m_AllPotentialTargets[targetIndex].ReplaceStoredTarget(target);
            }

            if (!skipSortAndUpdate)
                SortAndUpdateActiveTargets();
        }

        public void SetTargetActive(int targetIndex, bool skipSortAndUpdate = false)
        {
            Target target = m_AllPotentialTargets[targetIndex].GetTarget();
            SetTargetActive(target, skipSortAndUpdate);
        }

        public void SetTargetInactive(Target target, bool skipSortAndUpdate = false)
        {
            int activeIndex = m_ActiveTargets.IndexOf(target);
            if (activeIndex < 0)
                return;

            int targetIndex = GetTargetIndex(target);

            // if a target was in the active targets, it should also have been in the potential targets list
            Assert.IsTrue(targetIndex >= 0);

            m_ActiveTargets.RemoveAt(activeIndex);

            if (!skipSortAndUpdate)
                SortAndUpdateActiveTargets();
        }

        // this list is populated by graph validation, and lists all of the targets that nodes did not like
        [NonSerialized]
        List<Target> m_UnsupportedTargets = new List<Target>();
        public List<Target> unsupportedTargets { get => m_UnsupportedTargets; }

        private Comparison<Target> targetComparison = new Comparison<Target>((a, b) => string.Compare(a.displayName, b.displayName));
        public void SortActiveTargets()
        {
            activeTargets.Sort(targetComparison);
        }

        // TODO: Need a better way to handle this
        public bool hasVFXCompatibleTarget => activeTargets.Any(o => o.SupportsVFX());
#if VFX_GRAPH_10_0_0_OR_NEWER
        public bool hasVFXTarget
        {
            get
            {
                bool supports = true;
                supports &= !isSubGraph;
                supports &= activeTargets.Any();
                // Maintain support for VFXTarget and VFX compatible targets.
                supports &= activeTargets.OfType<VFXTarget>().Any() || hasVFXCompatibleTarget;
                return supports;
            }
        }

        public bool isOnlyVFXTarget => activeTargets.Count() == 1 &&
        activeTargets.Count(t => t is VFXTarget) == 1;
#else
        public bool isVFXTarget => false;
        public bool isOnlyVFXTarget => false;
#endif
        #endregion

        public GraphData()
        {
            m_GroupItems[null] = new List<IGroupItem>();
            GetBlockFieldDescriptors();
            AddKnownTargetsToPotentialTargets();
        }

        // used to initialize the graph with targets, i.e. when creating new graphs via the popup menu
        public void InitializeOutputs(Target[] targets, BlockFieldDescriptor[] blockDescriptors)
        {
            if (targets == null)
                return;

            foreach (var target in targets)
            {
                if (GetTargetIndexByKnownType(target.GetType()) >= 0)
                {
                    SetTargetActive(target, true);
                }
            }
            SortActiveTargets();

            if (blockDescriptors != null)
            {
                foreach (var descriptor in blockDescriptors)
                {
                    var contextData = descriptor.shaderStage == ShaderStage.Fragment ? m_FragmentContext : m_VertexContext;
                    var block = (BlockNode)Activator.CreateInstance(typeof(BlockNode));
                    block.Init(descriptor);
                    AddBlockNoValidate(block, contextData, contextData.blocks.Count);
                }
            }

            ValidateGraph();
            var activeBlocks = GetActiveBlocksForAllActiveTargets();
            UpdateActiveBlocks(activeBlocks);
        }

        void GetBlockFieldDescriptors()
        {
            m_BlockFieldDescriptors = new List<BlockFieldDescriptor>();

            var asmTypes = TypeCache.GetTypesWithAttribute<GenerateBlocksAttribute>();
            foreach (var type in asmTypes)
            {
                var attrs = type.GetCustomAttributes(typeof(GenerateBlocksAttribute), false);
                if (attrs == null || attrs.Length <= 0)
                    continue;

                var attribute = attrs[0] as GenerateBlocksAttribute;

                // Get all fields that are BlockFieldDescriptor
                // If field and context stages match add to list
                foreach (var fieldInfo in type.GetFields())
                {
                    if (fieldInfo.GetValue(type) is BlockFieldDescriptor blockFieldDescriptor)
                    {
                        blockFieldDescriptor.path = attribute.path;
                        m_BlockFieldDescriptors.Add(blockFieldDescriptor);
                    }
                }
            }
        }

        void AddKnownTargetsToPotentialTargets()
        {
            Assert.AreEqual(m_AllPotentialTargets.Count, 0);

            // Find all valid Targets by looking in the TypeCache
            var targetTypes = TypeCache.GetTypesDerivedFrom<Target>();
            foreach (var type in targetTypes)
            {
                if (type.IsAbstract || type.IsGenericType || !type.IsClass)
                    continue;

                // create a new instance of the Target, to represent the potential Target
                // NOTE: this instance may be replaced later if we serialize in an Active Target of that type
                var target = (Target)Activator.CreateInstance(type);
                if (!target.isHidden)
                {
                    m_AllPotentialTargets.Add(new PotentialTarget(target));
                }
            }
        }

        public void SortAndUpdateActiveTargets()
        {
            SortActiveTargets();
            ValidateGraph();
            NodeUtils.ReevaluateActivityOfNodeList(m_Nodes.SelectValue());
        }

        public void ClearChanges()
        {
            m_AddedNodes.Clear();
            m_RemovedNodes.Clear();
            m_PastedNodes.Clear();
            m_ParentGroupChanges.Clear();
            m_AddedGroups.Clear();
            m_RemovedGroups.Clear();
            m_PastedGroups.Clear();
            m_AddedEdges.Clear();
            m_RemovedEdges.Clear();
            m_AddedInputs.Clear();
            m_RemovedInputs.Clear();
            m_MovedInputs.Clear();
            m_AddedCategories.Clear();
            m_RemovedCategories.Clear();
            m_MovedCategories.Clear();
            m_AddedStickyNotes.Clear();
            m_RemovedNotes.Clear();
            m_PastedStickyNotes.Clear();
            m_MostRecentlyCreatedGroup = null;
            m_MovedContexts = false;
        }

        public void AddNode(AbstractMaterialNode node)
        {
            if (node is AbstractMaterialNode materialNode)
            {
                if (isSubGraph && !materialNode.allowedInSubGraph)
                {
                    Debug.LogWarningFormat("Attempting to add {0} to Sub Graph. This is not allowed.", materialNode.GetType());
                    return;
                }

                AddNodeNoValidate(materialNode);

                // If adding a Sub Graph node whose asset contains Keywords
                // Need to restest Keywords against the variant limit
                if (node is SubGraphNode subGraphNode &&
                    subGraphNode.asset != null &&
                    subGraphNode.asset.keywords.Any())
                {
                    OnKeywordChangedNoValidate();
                }

                ValidateGraph();
            }
            else
            {
                Debug.LogWarningFormat("Trying to add node {0} to Material graph, but it is not a {1}", node, typeof(AbstractMaterialNode));
            }
        }

        public void CreateGroup(GroupData groupData)
        {
            if (AddGroup(groupData))
            {
                m_MostRecentlyCreatedGroup = groupData;
            }
        }

        bool AddGroup(GroupData groupData)
        {
            if (m_GroupDatas.Contains(groupData))
                return false;

            m_GroupDatas.Add(groupData);
            m_AddedGroups.Add(groupData);
            m_GroupItems.Add(groupData, new List<IGroupItem>());

            return true;
        }

        public void RemoveGroup(GroupData groupData)
        {
            RemoveGroupNoValidate(groupData);
            ValidateGraph();
        }

        void RemoveGroupNoValidate(GroupData group)
        {
            if (!m_GroupDatas.Contains(group))
                throw new InvalidOperationException("Cannot remove a group that doesn't exist.");
            m_GroupDatas.Remove(group);
            m_RemovedGroups.Add(group);

            if (m_GroupItems.TryGetValue(group, out var items))
            {
                foreach (IGroupItem groupItem in items.ToList())
                {
                    SetGroup(groupItem, null);
                }

                m_GroupItems.Remove(group);
            }
        }

        public void AddStickyNote(StickyNoteData stickyNote)
        {
            if (m_StickyNoteDatas.Contains(stickyNote))
            {
                throw new InvalidOperationException("Sticky note has already been added to the graph.");
            }

            if (!m_GroupItems.ContainsKey(stickyNote.group))
            {
                throw new InvalidOperationException("Trying to add sticky note with group that doesn't exist.");
            }

            m_StickyNoteDatas.Add(stickyNote);
            m_AddedStickyNotes.Add(stickyNote);
            m_GroupItems[stickyNote.group].Add(stickyNote);
        }

        void RemoveNoteNoValidate(StickyNoteData stickyNote)
        {
            if (!m_StickyNoteDatas.Contains(stickyNote))
            {
                throw new InvalidOperationException("Cannot remove a note that doesn't exist.");
            }

            m_StickyNoteDatas.Remove(stickyNote);
            m_RemovedNotes.Add(stickyNote);

            if (m_GroupItems.TryGetValue(stickyNote.group, out var groupItems))
            {
                groupItems.Remove(stickyNote);
            }
        }

        public void RemoveStickyNote(StickyNoteData stickyNote)
        {
            RemoveNoteNoValidate(stickyNote);
            ValidateGraph();
        }

        public void SetGroup(IGroupItem node, GroupData group)
        {
            var groupChange = new ParentGroupChange()
            {
                groupItem = node,
                oldGroup = node.group,
                // Checking if the groupdata is null. If it is, then it means node has been removed out of a group.
                // If the group data is null, then maybe the old group id should be removed
                newGroup = group,
            };
            node.group = groupChange.newGroup;

            var oldGroupNodes = m_GroupItems[groupChange.oldGroup];
            oldGroupNodes.Remove(node);

            m_GroupItems[groupChange.newGroup].Add(node);
            m_ParentGroupChanges.Add(groupChange);
        }

        public void AddContexts()
        {
            m_VertexContext = new ContextData();
            m_VertexContext.shaderStage = ShaderStage.Vertex;
            m_VertexContext.position = new Vector2(0, 0);
            m_FragmentContext = new ContextData();
            m_FragmentContext.shaderStage = ShaderStage.Fragment;
            m_FragmentContext.position = new Vector2(0, 200);
        }

        public void AddBlock(BlockNode blockNode, ContextData contextData, int index)
        {
            AddBlockNoValidate(blockNode, contextData, index);
            ValidateGraph();

            var activeBlocks = GetActiveBlocksForAllActiveTargets();
            UpdateActiveBlocks(activeBlocks);
        }

        void AddBlockNoValidate(BlockNode blockNode, ContextData contextData, int index)
        {
            // Regular AddNode path
            AddNodeNoValidate(blockNode);

            // Set BlockNode properties
            blockNode.contextData = contextData;

            // Add to ContextData
            if (index == -1 || index >= contextData.blocks.Count())
            {
                contextData.blocks.Add(blockNode);
            }
            else
            {
                contextData.blocks.Insert(index, blockNode);
            }
        }

        public List<BlockFieldDescriptor> GetActiveBlocksForAllActiveTargets()
        {
            // Get list of active Block types
            var currentBlocks = GetNodes<BlockNode>();
            var context = new TargetActiveBlockContext(currentBlocks.Select(x => x.descriptor).ToList(), null);
            foreach (var target in activeTargets)
            {
                target.GetActiveBlocks(ref context);
            }

            // custom blocks aren't going to exist in GetActiveBlocks, we need to ensure we grab those too.
            foreach (var cibnode in currentBlocks.Where(bn => bn.isCustomBlock))
            {
                context.AddBlock(cibnode.descriptor);
            }
            return context.activeBlocks;
        }

        public void UpdateActiveBlocks(List<BlockFieldDescriptor> activeBlockDescriptors)
        {
            // Set Blocks as active based on supported Block list
            //Note: we never want unknown blocks to be active, so explicitly set them to inactive always
            bool disableCI = activeTargets.All(at => at.ignoreCustomInterpolators);
            foreach (var vertexBlock in vertexContext.blocks)
            {
                if (vertexBlock.value?.isCustomBlock == true)
                {
                    vertexBlock.value.SetOverrideActiveState(disableCI ? AbstractMaterialNode.ActiveState.ExplicitInactive : AbstractMaterialNode.ActiveState.ExplicitActive);
                }
                else if (vertexBlock.value?.descriptor?.isUnknown == true)
                {
                    vertexBlock.value.SetOverrideActiveState(AbstractMaterialNode.ActiveState.ExplicitInactive);
                }
                else
                {
                    vertexBlock.value.SetOverrideActiveState(activeBlockDescriptors.Contains(vertexBlock.value.descriptor) ? AbstractMaterialNode.ActiveState.ExplicitActive
                        : AbstractMaterialNode.ActiveState.ExplicitInactive);
                }
            }
            foreach (var fragmentBlock in fragmentContext.blocks)
            {
                if (fragmentBlock.value?.descriptor?.isUnknown == true)
                {
                    fragmentBlock.value.SetOverrideActiveState(AbstractMaterialNode.ActiveState.ExplicitInactive);
                }
                else
                {
                    fragmentBlock.value.SetOverrideActiveState(activeBlockDescriptors.Contains(fragmentBlock.value.descriptor) ? AbstractMaterialNode.ActiveState.ExplicitActive
                        : AbstractMaterialNode.ActiveState.ExplicitInactive);
                }
            }
        }

        public void AddRemoveBlocksFromActiveList(List<BlockFieldDescriptor> activeBlockDescriptors)
        {
            var blocksToRemove = ListPool<BlockNode>.Get();

            void GetBlocksToRemoveForContext(ContextData contextData)
            {
                for (int i = 0; i < contextData.blocks.Count; i++)
                {
                    if (contextData.blocks[i].value?.isCustomBlock == true) // custom interpolators are fine.
                        continue;

                    var block = contextData.blocks[i];
                    if (!activeBlockDescriptors.Contains(block.value.descriptor))
                    {
                        var slot = block.value.FindSlot<MaterialSlot>(0);
                        //Need to check if a slot is not default value OR is an untracked unknown block type
                        if (slot.IsUsingDefaultValue() || block.value.descriptor.isUnknown) // TODO: How to check default value
                        {
                            blocksToRemove.Add(block);
                        }
                    }
                }
            }

            void TryAddBlockToContext(BlockFieldDescriptor descriptor, ContextData contextData)
            {
                if (descriptor.shaderStage != contextData.shaderStage)
                    return;

                if (contextData.blocks.Any(x => x.value.descriptor.Equals(descriptor)))
                    return;

                var node = (BlockNode)Activator.CreateInstance(typeof(BlockNode));
                node.Init(descriptor);
                AddBlockNoValidate(node, contextData, contextData.blocks.Count);
            }

            // Get inactive Blocks to remove
            GetBlocksToRemoveForContext(vertexContext);
            GetBlocksToRemoveForContext(fragmentContext);

            // Remove blocks
            foreach (var block in blocksToRemove)
            {
                RemoveNodeNoValidate(block);
            }

            // Add active Blocks not currently in Contexts
            foreach (var descriptor in activeBlockDescriptors)
            {
                TryAddBlockToContext(descriptor, vertexContext);
                TryAddBlockToContext(descriptor, fragmentContext);
            }
        }

        void AddNodeNoValidate(AbstractMaterialNode node)
        {
            if (node.group != null && !m_GroupItems.ContainsKey(node.group))
            {
                throw new InvalidOperationException("Cannot add a node whose group doesn't exist.");
            }
            node.owner = this;
            m_Nodes.Add(node);
            m_NodeDictionary.Add(node.objectId, node);
            m_AddedNodes.Add(node);
            m_GroupItems[node.group].Add(node);
        }

        public void RemoveNode(AbstractMaterialNode node)
        {
            if (!node.canDeleteNode)
            {
                throw new InvalidOperationException($"Node {node.name} ({node.objectId}) cannot be deleted.");
            }
            RemoveNodeNoValidate(node);
            ValidateGraph();

            if (node is BlockNode blockNode)
            {
                var activeBlocks = GetActiveBlocksForAllActiveTargets();
                UpdateActiveBlocks(activeBlocks);
                blockNode.Dirty(ModificationScope.Graph);
            }
        }

        void RemoveNodeNoValidate(AbstractMaterialNode node)
        {
            if (!m_NodeDictionary.ContainsKey(node.objectId) && node.isActive && !m_RemovedNodes.Contains(node))
            {
                throw new InvalidOperationException("Cannot remove a node that doesn't exist.");
            }

            m_Nodes.Remove(node);
            m_NodeDictionary.Remove(node.objectId);
            messageManager?.RemoveNode(node.objectId);
            m_RemovedNodes.Add(node);

            if (m_GroupItems.TryGetValue(node.group, out var groupItems))
            {
                groupItems.Remove(node);
            }

            if (node is BlockNode blockNode && blockNode.contextData != null)
            {
                // Remove from ContextData
                blockNode.contextData.blocks.Remove(blockNode);
            }
        }

        void AddEdgeToNodeEdges(IEdge edge)
        {
            List<IEdge> inputEdges;
            if (!m_NodeEdges.TryGetValue(edge.inputSlot.node.objectId, out inputEdges))
                m_NodeEdges[edge.inputSlot.node.objectId] = inputEdges = new List<IEdge>();
            inputEdges.Add(edge);

            List<IEdge> outputEdges;
            if (!m_NodeEdges.TryGetValue(edge.outputSlot.node.objectId, out outputEdges))
                m_NodeEdges[edge.outputSlot.node.objectId] = outputEdges = new List<IEdge>();
            outputEdges.Add(edge);
        }

        IEdge ConnectNoValidate(SlotReference fromSlotRef, SlotReference toSlotRef, bool assumeBatchSafety = false)
        {
            var fromNode = fromSlotRef.node;
            var toNode = toSlotRef.node;

            if (fromNode == null || toNode == null)
                return null;

            // both nodes must belong to this graph
            if ((fromNode.owner != this) || (toNode.owner != this))
                return null;

            // if fromNode is already connected to toNode
            // do now allow a connection as toNode will then
            // have an edge to fromNode creating a cycle.
            // if this is parsed it will lead to an infinite loop.
            if (!assumeBatchSafety) // This may not be that helpful.
            {
                var dependentNodes = new List<AbstractMaterialNode>();
                NodeUtils.CollectNodesNodeFeedsInto(dependentNodes, toNode);
                if (dependentNodes.Contains(fromNode))
                    return null;
            }

            var fromSlot = fromNode.FindSlot<MaterialSlot>(fromSlotRef.slotId);
            var toSlot = toNode.FindSlot<MaterialSlot>(toSlotRef.slotId);

            if (fromSlot == null || toSlot == null)
                return null;

            if (fromSlot.isOutputSlot == toSlot.isOutputSlot)
                return null;

            var outputSlot = fromSlot.isOutputSlot ? fromSlotRef : toSlotRef;
            var inputSlot = fromSlot.isInputSlot ? fromSlotRef : toSlotRef;

            s_TempEdges.Clear();
            GetEdges(inputSlot, s_TempEdges);

            // remove any inputs that exits before adding
            foreach (var edge in s_TempEdges)
            {
                RemoveEdgeNoValidate(edge);
            }

            var newEdge = new Edge(outputSlot, inputSlot);
            m_Edges.Add(newEdge);
            m_AddedEdges.Add(newEdge);
            AddEdgeToNodeEdges(newEdge);
            if (!assumeBatchSafety)
            {
                NodeUtils.ReevaluateActivityOfConnectedNodes(toNode);
            }

            //Debug.LogFormat("Connected edge: {0} -> {1} ({2} -> {3})\n{4}", newEdge.outputSlot.nodeGuid, newEdge.inputSlot.nodeGuid, fromNode.name, toNode.name, Environment.StackTrace);
            return newEdge;
        }

        public IEdge Connect(SlotReference fromSlotRef, SlotReference toSlotRef)
        {
            var newEdge = ConnectNoValidate(fromSlotRef, toSlotRef);
            ValidateGraph();
            return newEdge;
        }

        internal void UnnotifyAddedEdge(IEdge edge)
        {
            m_AddedEdges.Remove(edge);
        }

        public void RemoveEdges(IEdge[] edges)
        {
            if (edges.Length == 0)
                return;
            foreach (var edge in edges)
            {
                RemoveEdgeNoValidate(edge);
            }
            ValidateGraph();
        }

        public void RemoveEdge(IEdge e)
        {
            RemoveEdgeNoValidate(e);
            ValidateGraph();
        }

        public void RemoveElements(AbstractMaterialNode[] nodes, IEdge[] edges, GroupData[] groups, StickyNoteData[] notes, ShaderInput[] inputs = null)
        {
            foreach (var node in nodes)
            {
                if (!node.canDeleteNode)
                {
                    throw new InvalidOperationException($"Node {node.name} ({node.objectId}) cannot be deleted.");
                }
            }

            foreach (var edge in edges.ToArray())
            {
                RemoveEdgeNoValidate(edge);
            }

            foreach (var serializableNode in nodes)
            {
                // Check if it is a Redirect Node
                // Get the edges and then re-create all Edges
                // This only works if it has all the edges.
                // If one edge is already deleted then we can not re-create.
                if (serializableNode is RedirectNodeData redirectNode)
                {
                    redirectNode.GetOutputAndInputSlots(out SlotReference outputSlotRef, out var inputSlotRefs);

                    foreach (SlotReference slot in inputSlotRefs)
                    {
                        ConnectNoValidate(outputSlotRef, slot);
                    }
                }

                RemoveNodeNoValidate(serializableNode);
            }

            foreach (var noteData in notes)
            {
                RemoveNoteNoValidate(noteData);
            }

            foreach (var groupData in groups)
            {
                RemoveGroupNoValidate(groupData);
            }

            if (inputs != null)
            {
                foreach (var shaderInput in inputs)
                {
                    RemoveGraphInputNoValidate(shaderInput);
                }
            }

            ValidateGraph();

            if (nodes.Any(x => x is BlockNode))
            {
                var activeBlocks = GetActiveBlocksForAllActiveTargets();
                UpdateActiveBlocks(activeBlocks);
            }
        }

        void RemoveEdgeNoValidate(IEdge e, bool reevaluateActivity = true)
        {
            e = m_Edges.FirstOrDefault(x => x.Equals(e));
            if (e == null)
                throw new ArgumentException("Trying to remove an edge that does not exist.", "e");
            m_Edges.Remove(e as Edge);

            AbstractMaterialNode input = e.inputSlot.node, output = e.outputSlot.node;
            if (input != null && ShaderGraphPreferences.autoAddRemoveBlocks)
            {
                checkAutoAddRemoveBlocks = true;
            }

            List<IEdge> inputNodeEdges;
            if (m_NodeEdges.TryGetValue(input.objectId, out inputNodeEdges))
                inputNodeEdges.Remove(e);

            List<IEdge> outputNodeEdges;
            if (m_NodeEdges.TryGetValue(output.objectId, out outputNodeEdges))
                outputNodeEdges.Remove(e);

            m_AddedEdges.Remove(e);
            m_RemovedEdges.Add(e);

            if (reevaluateActivity)
            {
                if (input != null)
                {
                    NodeUtils.ReevaluateActivityOfConnectedNodes(input);
                }

                if (output != null)
                {
                    NodeUtils.ReevaluateActivityOfConnectedNodes(output);
                }
            }
        }

        public AbstractMaterialNode GetNodeFromId(string nodeId)
        {
            m_NodeDictionary.TryGetValue(nodeId, out var node);
            return node;
        }

        public T GetNodeFromId<T>(string nodeId) where T : class
        {
            m_NodeDictionary.TryGetValue(nodeId, out var node);
            return node as T;
        }

        internal Texture2DShaderProperty GetMainTexture()
        {
            foreach (var prop in properties)
            {
                if (prop is Texture2DShaderProperty tex)
                {
                    if (tex.isMainTexture)
                    {
                        return tex;
                    }
                }
            }
            return null;
        }

        internal ColorShaderProperty GetMainColor()
        {
            foreach (var prop in properties)
            {
                if (prop is ColorShaderProperty col)
                {
                    if (col.isMainColor)
                    {
                        return col;
                    }
                }
            }
            return null;
        }

        public bool ContainsCategory(CategoryData categoryData)
        {
            return categories.Contains(categoryData);
        }

        public bool ContainsInput(ShaderInput shaderInput)
        {
            if (shaderInput == null)
                return false;

            return properties.Contains(shaderInput) || keywords.Contains(shaderInput) || dropdowns.Contains(shaderInput);
        }

        public bool ContainsNode(AbstractMaterialNode node)
        {
            if (node == null)
                return false;

            return m_NodeDictionary.TryGetValue(node.objectId, out var foundNode) && node == foundNode;
        }

        public void GetEdges(MaterialSlot slot, List<IEdge> foundEdges)
        {
            List<IEdge> candidateEdges;
            if (!m_NodeEdges.TryGetValue(slot.owner.objectId, out candidateEdges))
                return;

            foreach (var edge in candidateEdges)
            {
                var cs = slot.isInputSlot ? edge.inputSlot : edge.outputSlot;
                if (cs.node == slot.owner && cs.slotId == slot.id)
                    foundEdges.Add(edge);
            }
        }

        public void GetEdges(SlotReference s, List<IEdge> foundEdges)
        {
            List<IEdge> candidateEdges;
            if (!m_NodeEdges.TryGetValue(s.node.objectId, out candidateEdges))
                return;

            MaterialSlot slot = s.slot;
            foreach (var edge in candidateEdges)
            {
                var cs = slot.isInputSlot ? edge.inputSlot : edge.outputSlot;
                if (cs.node == s.node && cs.slotId == s.slotId)
                    foundEdges.Add(edge);
            }
        }

        public IEnumerable<IEdge> GetEdges(SlotReference s)
        {
            var edges = new List<IEdge>();
            GetEdges(s, edges);
            return edges;
        }

        public void GetEdges(AbstractMaterialNode node, List<IEdge> foundEdges)
        {
            if (m_NodeEdges.TryGetValue(node.objectId, out var edges))
            {
                foundEdges.AddRange(edges);
            }
        }

        public IEnumerable<IEdge> GetEdges(AbstractMaterialNode node)
        {
            List<IEdge> edges = new List<IEdge>();
            GetEdges(node, edges);
            return edges;
        }

        public void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            foreach (var prop in properties)
                prop.ForeachHLSLProperty(action);
        }

        public void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            foreach (var prop in properties)
            {
                // For VFX Shader generation, we must omit exposed properties from the Material CBuffer.
                // This is because VFX computes properties on the fly in the vertex stage, and packed into interpolator.
                if (generationMode == GenerationMode.VFX && prop.isExposed)
                {
                    prop.overrideHLSLDeclaration = true;
                    prop.hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare;
                }

                // ugh, this needs to be moved to the gradient property implementation
                if (prop is GradientShaderProperty gradientProp && generationMode == GenerationMode.Preview)
                {
                    GradientUtil.GetGradientPropertiesForPreview(collector, gradientProp.referenceName, gradientProp.value);
                    continue;
                }

                collector.AddShaderProperty(prop);
            }
        }

        private static void CollectSubgraphKeywordsR(KeywordCollector collector, SubGraphAsset asset)
        {
            if (asset is null || !asset.isValid || asset.isNull)
                return;

            foreach(var keyword in asset.keywords)
            {
                collector.AddShaderKeyword(keyword);
            }
            foreach(var guid in asset.children)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var child = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(path);
                CollectSubgraphKeywordsR(collector, child);
            }
        }

        public void CollectShaderKeywords(KeywordCollector collector, GenerationMode generationMode)
        {
            foreach (var keyword in keywords)
            {
                collector.AddShaderKeyword(keyword);
            }
            foreach(var node in GetNodes<SubGraphNode>())
            {
                CollectSubgraphKeywordsR(collector, node.asset);
            }

            // Alwways calculate permutations when collecting
            collector.CalculateKeywordPermutations();
        }

        public bool IsInputAllowedInGraph(ShaderInput input)
        {
            return (isSubGraph && input.allowedInSubGraph) || (!isSubGraph && input.allowedInMainGraph);
        }

        public bool IsInputAllowedInGraph(AbstractMaterialNode node)
        {
            return (isSubGraph && node.allowedInSubGraph) || (!isSubGraph && node.allowedInMainGraph);
        }

        // adds the input to the graph, and sanitizes the names appropriately
        public void AddGraphInput(ShaderInput input, int index = -1)
        {
            if (input == null)
                return;

            // sanitize the display name
            input.SetDisplayNameAndSanitizeForGraph(this);

            // sanitize the reference name
            input.SetReferenceNameAndSanitizeForGraph(this);

            AddGraphInputNoSanitization(input, index);
        }

        // just adds the input to the graph, does not fix colliding or illegal names
        internal void AddGraphInputNoSanitization(ShaderInput input, int index = -1)
        {
            if (input == null)
                return;

            switch (input)
            {
                case AbstractShaderProperty property:
                    if (m_Properties.Contains(property))
                        return;

                    if (index < 0)
                        m_Properties.Add(property);
                    else
                        m_Properties.Insert(index, property);

                    break;
                case ShaderKeyword keyword:
                    if (m_Keywords.Contains(keyword))
                        return;

                    if (index < 0)
                        m_Keywords.Add(keyword);
                    else
                        m_Keywords.Insert(index, keyword);

                    OnKeywordChangedNoValidate();

                    break;
                case ShaderDropdown dropdown:
                    if (m_Dropdowns.Contains(dropdown))
                        return;

                    if (index < 0)
                        m_Dropdowns.Add(dropdown);
                    else
                        m_Dropdowns.Insert(index, dropdown);

                    OnDropdownChangedNoValidate();

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            m_AddedInputs.Add(input);
        }

        // only ignores names matching ignoreName on properties matching ignoreGuid
        public List<string> BuildPropertyDisplayNameList(AbstractShaderProperty ignoreProperty, string ignoreName)
        {
            List<String> result = new List<String>();
            foreach (var p in properties)
            {
                int before = result.Count;
                p.GetPropertyDisplayNames(result);

                if ((p == ignoreProperty) && (ignoreName != null))
                {
                    // remove ignoreName, if it was just added
                    for (int i = before; i < result.Count; i++)
                    {
                        if (result[i] == ignoreName)
                        {
                            result.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            return result;
        }

        // only ignores names matching ignoreName on properties matching ignoreGuid
        public List<string> BuildPropertyReferenceNameList(AbstractShaderProperty ignoreProperty, string ignoreName)
        {
            List<String> result = new List<String>();
            foreach (var p in properties)
            {
                int before = result.Count;
                p.GetPropertyReferenceNames(result);

                if ((p == ignoreProperty) && (ignoreName != null))
                {
                    // remove ignoreName, if it was just added
                    for (int i = before; i < result.Count; i++)
                    {
                        if (result[i] == ignoreName)
                        {
                            result.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
            return result;
        }

        public string SanitizeGraphInputName(ShaderInput input, string desiredName)
        {
            string currentName = input.displayName;
            string sanitizedName = desiredName.Trim();
            switch (input)
            {
                case AbstractShaderProperty property:
                    sanitizedName = GraphUtil.SanitizeName(BuildPropertyDisplayNameList(property, currentName), "{0} ({1})", sanitizedName);
                    break;
                case ShaderKeyword keyword:
                    sanitizedName = GraphUtil.SanitizeName(keywords.Where(p => p != input).Select(p => p.displayName), "{0} ({1})", sanitizedName);
                    break;
                case ShaderDropdown dropdown:
                    sanitizedName = GraphUtil.SanitizeName(dropdowns.Where(p => p != input).Select(p => p.displayName), "{0} ({1})", sanitizedName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return sanitizedName;
        }

        public string SanitizeGraphInputReferenceName(ShaderInput input, string desiredName)
        {
            var sanitizedName = NodeUtils.ConvertToValidHLSLIdentifier(desiredName, (desiredName) => (NodeUtils.IsShaderLabKeyWord(desiredName) || NodeUtils.IsShaderGraphKeyWord(desiredName)));

            switch (input)
            {
                case AbstractShaderProperty property:
                {
                    // must deduplicate ref names against keywords, dropdowns, and properties, as they occupy the same name space
                    var existingNames = properties.Where(p => p != property).Select(p => p.referenceName).Union(keywords.Select(p => p.referenceName)).Union(dropdowns.Select(p => p.referenceName));
                    sanitizedName = GraphUtil.DeduplicateName(existingNames, "{0}_{1}", sanitizedName);
                }
                break;
                case ShaderKeyword keyword:
                {
                    // must deduplicate ref names against keywords, dropdowns, and properties, as they occupy the same name space
                    sanitizedName = sanitizedName.ToUpper();
                    var existingNames = properties.Select(p => p.referenceName).Union(keywords.Where(p => p != input).Select(p => p.referenceName)).Union(dropdowns.Select(p => p.referenceName));
                    sanitizedName = GraphUtil.DeduplicateName(existingNames, "{0}_{1}", sanitizedName);
                }
                break;
                case ShaderDropdown dropdown:
                {
                    // must deduplicate ref names against keywords, dropdowns, and properties, as they occupy the same name space
                    var existingNames = properties.Select(p => p.referenceName).Union(keywords.Select(p => p.referenceName)).Union(dropdowns.Where(p => p != input).Select(p => p.referenceName));
                    sanitizedName = GraphUtil.DeduplicateName(existingNames, "{0}_{1}", sanitizedName);
                }
                break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return sanitizedName;
        }

        // copies the ShaderInput, and adds it to the graph with proper name sanitization, returning the copy
        public ShaderInput AddCopyOfShaderInput(ShaderInput source, int insertIndex = -1)
        {
            ShaderInput copy = source.Copy();

            // some ShaderInputs cannot be copied (unknown types)
            if (copy == null)
                return null;

            // copy common properties that should always be copied over
            copy.generatePropertyBlock = source.generatePropertyBlock;      // the exposed toggle

            if ((source is AbstractShaderProperty sourceProp) && (copy is AbstractShaderProperty copyProp))
            {
                copyProp.hidden = sourceProp.hidden;
                copyProp.precision = sourceProp.precision;
                copyProp.overrideHLSLDeclaration = sourceProp.overrideHLSLDeclaration;
                copyProp.hlslDeclarationOverride = sourceProp.hlslDeclarationOverride;
                copyProp.useCustomSlotLabel = sourceProp.useCustomSlotLabel;
            }

            // sanitize the display name (we let the .Copy() function actually copy the display name over)
            copy.SetDisplayNameAndSanitizeForGraph(this);

            // copy and sanitize the reference name (must do this after the display name, so the default is correct)
            if (source.IsUsingNewDefaultRefName())
            {
                // if source was using new default, we can just rely on the default for the copy we made.
                // the code above has already handled collisions properly for the default,
                // and it will assign the same name as the source if there are no collisions.
                // Also it will result better names chosen when there are collisions.
            }
            else
            {
                // when the source is using an old default, we set it as an override
                copy.SetReferenceNameAndSanitizeForGraph(this, source.referenceName);
            }

            copy.OnBeforePasteIntoGraph(this);
            AddGraphInputNoSanitization(copy, insertIndex);

            return copy;
        }

        public void RemoveGraphInput(ShaderInput input)
        {
            switch (input)
            {
                case AbstractShaderProperty property:
                    var propertyNodes = GetNodes<PropertyNode>().Where(x => x.property == input).ToList();
                    foreach (var propertyNode in propertyNodes)
                        ReplacePropertyNodeWithConcreteNodeNoValidate(propertyNode);
                    break;
            }

            // Also remove this input from any category it existed in
            foreach (var categoryData in categories)
            {
                if (categoryData.IsItemInCategory(input))
                {
                    categoryData.RemoveItemFromCategory(input);
                    break;
                }
            }

            RemoveGraphInputNoValidate(input);
            ValidateGraph();
        }

        public void MoveCategory(CategoryData category, int newIndex)
        {
            if (newIndex > m_CategoryData.Count || newIndex < 0)
            {
                AssertHelpers.Fail("New index is not within categories list.");
                return;
            }
            var currentIndex = m_CategoryData.IndexOf(category);
            if (currentIndex == -1)
            {
                AssertHelpers.Fail("Category is not in graph.");
                return;
            }
            if (newIndex == currentIndex)
                return;
            m_CategoryData.RemoveAt(currentIndex);
            if (newIndex > currentIndex)
                newIndex--;
            var isLast = newIndex == m_CategoryData.Count;
            if (isLast)
                m_CategoryData.Add(category);
            else
                m_CategoryData.Insert(newIndex, category);
            if (!m_MovedCategories.Contains(category))
                m_MovedCategories.Add(category);
        }

        public void MoveItemInCategory(ShaderInput itemToMove, int newIndex, string associatedCategoryGuid)
        {
            foreach (var categoryData in categories)
            {
                if (categoryData.categoryGuid == associatedCategoryGuid && categoryData.IsItemInCategory(itemToMove))
                {
                    // Validate new index to move the item to
                    if (newIndex < -1 || newIndex >= categoryData.childCount)
                    {
                        AssertHelpers.Fail("Provided invalid index input to MoveItemInCategory.");
                        return;
                    }

                    categoryData.MoveItemInCategory(itemToMove, newIndex);
                    break;
                }
            }
        }

        public int GetGraphInputIndex(ShaderInput input)
        {
            switch (input)
            {
                case AbstractShaderProperty property:
                    return m_Properties.IndexOf(property);
                case ShaderKeyword keyword:
                    return m_Keywords.IndexOf(keyword);
                case ShaderDropdown dropdown:
                    return m_Dropdowns.IndexOf(dropdown);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void RemoveGraphInputNoValidate(ShaderInput shaderInput)
        {
            if (shaderInput is AbstractShaderProperty property && m_Properties.Remove(property) ||
                shaderInput is ShaderKeyword keyword && m_Keywords.Remove(keyword) ||
                shaderInput is ShaderDropdown dropdown && m_Dropdowns.Remove(dropdown))
            {
                m_RemovedInputs.Add(shaderInput);
                m_AddedInputs.Remove(shaderInput);
                m_MovedInputs.Remove(shaderInput);
            }
        }

        static List<IEdge> s_TempEdges = new List<IEdge>();

        public void ReplacePropertyNodeWithConcreteNode(PropertyNode propertyNode)
        {
            ReplacePropertyNodeWithConcreteNodeNoValidate(propertyNode);
            ValidateGraph();
        }

        void ReplacePropertyNodeWithConcreteNodeNoValidate(PropertyNode propertyNode, bool deleteNodeIfNoConcreteFormExists = true)
        {
            var property = properties.FirstOrDefault(x => x == propertyNode.property) ?? propertyNode.property;
            if (property == null)
                return;

            var node = property.ToConcreteNode() as AbstractMaterialNode;
            if (node == null)   // Some nodes have no concrete form
            {
                if (deleteNodeIfNoConcreteFormExists)
                    RemoveNodeNoValidate(propertyNode);
                return;
            }

            var slot = propertyNode.FindOutputSlot<MaterialSlot>(PropertyNode.OutputSlotId);
            var newSlot = node.GetOutputSlots<MaterialSlot>().FirstOrDefault(s => s.valueType == slot.valueType);
            if (newSlot == null)
                return;

            node.drawState = propertyNode.drawState;
            node.group = propertyNode.group;
            AddNodeNoValidate(node);

            foreach (var edge in this.GetEdges(slot.slotReference))
                ConnectNoValidate(newSlot.slotReference, edge.inputSlot);

            RemoveNodeNoValidate(propertyNode);
        }

        public void AddCategory(CategoryData categoryDataReference)
        {
            m_CategoryData.Add(categoryDataReference);
            m_AddedCategories.Add(categoryDataReference);
        }

        public string FindCategoryForInput(ShaderInput input)
        {
            foreach (var categoryData in categories)
            {
                if (categoryData.IsItemInCategory(input))
                {
                    return categoryData.categoryGuid;
                }
            }

            AssertHelpers.Fail("Attempted to find category for an input that doesn't exist in the graph.");
            return String.Empty;
        }

        public void ChangeCategoryName(string categoryGUID, string newName)
        {
            foreach (var categoryData in categories)
            {
                if (categoryData.categoryGuid == categoryGUID)
                {
                    var sanitizedCategoryName = GraphUtil.SanitizeCategoryName(newName);
                    categoryData.name = sanitizedCategoryName;
                    return;
                }
            }

            AssertHelpers.Fail("Attempted to change name of a category that does not exist in the graph.");
        }

        public void InsertItemIntoCategory(string categoryGUID, ShaderInput itemToAdd, int insertionIndex = -1)
        {
            foreach (var categoryData in categories)
            {
                if (categoryData.categoryGuid == categoryGUID)
                {
                    categoryData.InsertItemIntoCategory(itemToAdd, insertionIndex);
                }
                // Also make sure to remove this items guid from an existing category if it exists within one
                else if (categoryData.IsItemInCategory(itemToAdd))
                {
                    categoryData.RemoveItemFromCategory(itemToAdd);
                }
            }
        }

        public void RemoveItemFromCategory(string categoryGUID, ShaderInput itemToRemove)
        {
            foreach (var categoryData in categories)
            {
                if (categoryData.categoryGuid == categoryGUID)
                {
                    categoryData.RemoveItemFromCategory(itemToRemove);
                    return;
                }
            }

            AssertHelpers.Fail("Attempted to remove item from a category that does not exist in the graph.");
        }

        public void RemoveCategory(string categoryGUID)
        {
            var existingCategory = categories.FirstOrDefault(category => category.categoryGuid == categoryGUID);
            if (existingCategory != null)
            {
                m_CategoryData.Remove(existingCategory);
                m_RemovedCategories.Add(existingCategory);

                // Whenever a category is removed, also remove any inputs within that category
                foreach (var shaderInput in existingCategory.Children)
                    RemoveGraphInput(shaderInput);
            }
            else
                AssertHelpers.Fail("Attempted to remove a category that does not exist in the graph.");
        }

        // This differs from the rest of the category handling functions due to how categories can be copied between graphs
        // Since we have no guarantee of us owning the categories, we need a direct reference to the category to copy
        public CategoryData CopyCategory(CategoryData categoryToCopy)
        {
            var copiedCategory = new CategoryData(categoryToCopy);
            AddCategory(copiedCategory);
            // Whenever a category is copied, also copy over all the inputs within that category
            foreach (var childInputToCopy in categoryToCopy.Children)
            {
                var newShaderInput = AddCopyOfShaderInput(childInputToCopy);
                copiedCategory.InsertItemIntoCategory(newShaderInput);
            }

            return copiedCategory;
        }

        public void OnKeywordChanged()
        {
            OnKeywordChangedNoValidate();
            ValidateGraph();
        }

        public void OnKeywordChangedNoValidate()
        {
            DirtyAll<AbstractMaterialNode>(ModificationScope.Topological);
        }

        public void OnDropdownChanged()
        {
            OnDropdownChangedNoValidate();
            ValidateGraph();
        }

        public void OnDropdownChangedNoValidate()
        {
            DirtyAll<AbstractMaterialNode>(ModificationScope.Topological);
        }

        public void CleanupGraph()
        {
            //First validate edges, remove any
            //orphans. This can happen if a user
            //manually modifies serialized data
            //of if they delete a node in the inspector
            //debug view.
            foreach (var edge in edges.ToArray())
            {
                var outputNode = edge.outputSlot.node;
                var inputNode = edge.inputSlot.node;

                MaterialSlot outputSlot = null;
                MaterialSlot inputSlot = null;
                if (ContainsNode(outputNode) && ContainsNode(inputNode))
                {
                    outputSlot = outputNode.FindOutputSlot<MaterialSlot>(edge.outputSlot.slotId);
                    inputSlot = inputNode.FindInputSlot<MaterialSlot>(edge.inputSlot.slotId);
                }

                if (outputNode == null
                    || inputNode == null
                    || outputSlot == null
                    || inputSlot == null)
                {
                    //orphaned edge
                    RemoveEdgeNoValidate(edge, false);
                }
            }
        }

        private void DirtyAll<T>(ModificationScope modificationScope) where T : AbstractMaterialNode
        {
            graphIsConcretizing = true;
            try
            {
                var allNodes = GetNodes<T>();
                foreach (var node in allNodes)
                {
                    node.Dirty(modificationScope);
                    node.ValidateNode();
                }
            }
            catch (System.Exception e)
            {
                graphIsConcretizing = false;
                throw e;
            }
            graphIsConcretizing = false;
        }

        public void ValidateGraph()
        {
            messageManager?.ClearAllFromProvider(this);
            CleanupGraph();
            GraphSetup.SetupGraph(this);
            GraphConcretization.ConcretizeGraph(this);
            GraphValidation.ValidateGraph(this);

            for (int i = 0; i < m_AddedEdges.Count; ++i)
            {
                var edge = m_AddedEdges[i];
                if (!ContainsNode(edge.outputSlot.node) || !ContainsNode(edge.inputSlot.node))
                {
                    Debug.LogWarningFormat("Added edge is invalid: {0} -> {1}\n{2}", edge.outputSlot.node.objectId, edge.inputSlot.node.objectId, Environment.StackTrace);
                    m_AddedEdges.Remove(edge);
                }
            }

            for (int i = 0; i < m_ParentGroupChanges.Count; ++i)
            {
                var groupChange = m_ParentGroupChanges[i];
                switch (groupChange.groupItem)
                {
                    case AbstractMaterialNode node when !ContainsNode(node):
                    case StickyNoteData stickyNote when !m_StickyNoteDatas.Contains(stickyNote):
                        m_ParentGroupChanges.Remove(groupChange);
                        break;
                }
            }

            var existingDefaultCategory = categories.FirstOrDefault();
            if (existingDefaultCategory?.childCount == 0 && categories.Count() == 1 && (properties.Count() != 0 || keywords.Count() != 0 || dropdowns.Count() != 0))
            {
                // Have a graph with category data in invalid state
                // there is only one category, the default category, and all shader inputs should belong to it
                // Clear category data as it will get reconstructed in the BlackboardController constructor
                m_CategoryData.Clear();
            }

            ValidateCustomBlockLimit();
            ValidateContextBlocks();
        }

        public void AddValidationError(string id, string errorMessage,
            ShaderCompilerMessageSeverity severity = ShaderCompilerMessageSeverity.Error)
        {
            messageManager?.AddOrAppendError(this, id, new ShaderMessage("Validation: " + errorMessage, severity));
        }

        public void AddSetupError(string id, string errorMessage,
            ShaderCompilerMessageSeverity severity = ShaderCompilerMessageSeverity.Error)
        {
            messageManager?.AddOrAppendError(this, id, new ShaderMessage("Setup: " + errorMessage, severity));
        }

        public void AddConcretizationError(string id, string errorMessage,
            ShaderCompilerMessageSeverity severity = ShaderCompilerMessageSeverity.Error)
        {
            messageManager?.AddOrAppendError(this, id, new ShaderMessage("Concretization: " + errorMessage, severity));
        }

        public void ClearErrorsForNode(AbstractMaterialNode node)
        {
            messageManager?.ClearNodesFromProvider(this, node.ToEnumerable());
        }

        internal bool replaceInProgress = false;
        public void ReplaceWith(GraphData other)
        {
            if (other == null)
                throw new ArgumentException("Can only replace with another AbstractMaterialGraph", "other");

            replaceInProgress = true;
            m_GraphPrecision = other.m_GraphPrecision;
            m_PreviewMode = other.m_PreviewMode;
            m_OutputNode = other.m_OutputNode;

            if ((this.vertexContext.position != other.vertexContext.position) ||
                (this.fragmentContext.position != other.fragmentContext.position))
            {
                this.vertexContext.position = other.vertexContext.position;
                this.fragmentContext.position = other.fragmentContext.position;
                m_MovedContexts = true;
            }

            using (var inputsToRemove = PooledList<ShaderInput>.Get())
            {
                foreach (var property in m_Properties.SelectValue())
                    inputsToRemove.Add(property);
                foreach (var keyword in m_Keywords.SelectValue())
                    inputsToRemove.Add(keyword);
                foreach (var dropdown in m_Dropdowns.SelectValue())
                    inputsToRemove.Add(dropdown);
                foreach (var input in inputsToRemove)
                    RemoveGraphInputNoValidate(input);
            }
            foreach (var otherProperty in other.properties)
            {
                AddGraphInputNoSanitization(otherProperty);
            }
            foreach (var otherKeyword in other.keywords)
            {
                AddGraphInputNoSanitization(otherKeyword);
            }
            foreach (var otherDropdown in other.dropdowns)
            {
                AddGraphInputNoSanitization(otherDropdown);
            }

            other.ValidateGraph();
            ValidateGraph();

            // Current tactic is to remove all nodes and edges and then re-add them, such that depending systems
            // will re-initialize with new references.

            using (ListPool<GroupData>.Get(out var removedGroupDatas))
            {
                removedGroupDatas.AddRange(m_GroupDatas.SelectValue());
                foreach (var groupData in removedGroupDatas)
                {
                    RemoveGroupNoValidate(groupData);
                }
            }

            using (ListPool<StickyNoteData>.Get(out var removedNoteDatas))
            {
                removedNoteDatas.AddRange(m_StickyNoteDatas.SelectValue());
                foreach (var groupData in removedNoteDatas)
                {
                    RemoveNoteNoValidate(groupData);
                }
            }

            using (var pooledList = ListPool<IEdge>.Get(out var removedNodeEdges))
            {
                removedNodeEdges.AddRange(m_Edges);
                foreach (var edge in removedNodeEdges)
                    RemoveEdgeNoValidate(edge, false);
            }

            using (var nodesToRemove = PooledList<AbstractMaterialNode>.Get())
            {
                nodesToRemove.AddRange(m_Nodes.SelectValue());
                foreach (var node in nodesToRemove)
                    RemoveNodeNoValidate(node);
            }

            // Clear category data too before re-adding
            m_CategoryData.Clear();

            ValidateGraph();

            foreach (GroupData groupData in other.groups)
                AddGroup(groupData);

            // If categories are ever removed completely, make sure there is always one default category that exists
            if (!other.categories.Any())
            {
                AddCategory(CategoryData.DefaultCategory());
            }
            else
            {
                foreach (CategoryData categoryData in other.categories)
                {
                    AddCategory(categoryData);
                }
            }


            foreach (var stickyNote in other.stickyNotes)
            {
                AddStickyNote(stickyNote);
            }

            foreach (var node in other.GetNodes<AbstractMaterialNode>())
            {
                if (node is BlockNode blockNode)
                {
                    var contextData = blockNode.descriptor.shaderStage == ShaderStage.Vertex ? vertexContext : fragmentContext;
                    AddBlockNoValidate(blockNode, contextData, blockNode.index);
                }
                else
                {
                    AddNodeNoValidate(node);
                }
            }

            foreach (var edge in other.edges)
            {
                ConnectNoValidate(edge.outputSlot, edge.inputSlot, true);
            }

            outputNode = other.outputNode;

            // clear our local active targets and copy state from the other GraphData

            // NOTE:  we DO NOT clear or rebuild m_AllPotentialTargets, in order to
            // retain the data from any inactive targets.
            // this allows the user can add them back and keep the old settings

            m_ActiveTargets.Clear();
            foreach (var target in other.activeTargets)
            {
                // Ensure target inits correctly
                var context = new TargetSetupContext();
                target.Setup(ref context);
                SetTargetActive(target, true);
            }
            SortActiveTargets();

            // Active blocks
            var activeBlocks = GetActiveBlocksForAllActiveTargets();
            UpdateActiveBlocks(activeBlocks);
            replaceInProgress = false;
            ValidateGraph();
        }

        internal void PasteGraph(CopyPasteGraph graphToPaste, List<AbstractMaterialNode> remappedNodes,
            List<Edge> remappedEdges)
        {
            var groupMap = new Dictionary<GroupData, GroupData>();
            foreach (var group in graphToPaste.groups)
            {
                var position = group.position;
                position.x += 30;
                position.y += 30;

                GroupData newGroup = new GroupData(group.title, position);

                groupMap[group] = newGroup;

                AddGroup(newGroup);
                m_PastedGroups.Add(newGroup);
            }

            foreach (var stickyNote in graphToPaste.stickyNotes)
            {
                var position = stickyNote.position;
                position.x += 30;
                position.y += 30;

                StickyNoteData pastedStickyNote = new StickyNoteData(stickyNote.title, stickyNote.content, position);
                pastedStickyNote.textSize = stickyNote.textSize;
                pastedStickyNote.theme = stickyNote.theme;
                if (stickyNote.group != null && groupMap.ContainsKey(stickyNote.group))
                {
                    pastedStickyNote.group = groupMap[stickyNote.group];
                }

                AddStickyNote(pastedStickyNote);
                m_PastedStickyNotes.Add(pastedStickyNote);
            }

            var edges = graphToPaste.edges.ToList();
            var nodeList = graphToPaste.GetNodes<AbstractMaterialNode>();
            foreach (var node in nodeList)
            {
                // cannot paste block nodes, or unknown node types
                if ((node is BlockNode) || (node is MultiJsonInternal.UnknownNodeType))
                    continue;

                if (!IsInputAllowedInGraph(node))
                    continue;

                AbstractMaterialNode pastedNode = node;

                // Check if the property nodes need to be made into a concrete node.
                if (node is PropertyNode propertyNode)
                {
                    // If the property is not in the current graph, do check if the
                    // property can be made into a concrete node.
                    var property = m_Properties.SelectValue().FirstOrDefault(x => x.objectId == propertyNode.property.objectId
                        || (x.propertyType == propertyNode.property.propertyType && x.referenceName == propertyNode.property.referenceName));
                    if (property != null)
                    {
                        propertyNode.property = property;
                    }
                    else
                    {
                        pastedNode = propertyNode.property.ToConcreteNode();
                        // some property nodes cannot be concretized..  fail to paste them
                        if (pastedNode == null)
                            continue;
                        pastedNode.drawState = node.drawState;
                        for (var i = 0; i < edges.Count; i++)
                        {
                            var edge = edges[i];
                            if (edge.outputSlot.node == node)
                            {
                                edges[i] = new Edge(new SlotReference(pastedNode, edge.outputSlot.slotId), edge.inputSlot);
                            }
                            else if (edge.inputSlot.node == node)
                            {
                                edges[i] = new Edge(edge.outputSlot, new SlotReference(pastedNode, edge.inputSlot.slotId));
                            }
                        }
                    }
                }

                // If the node has a group guid and no group has been copied, reset the group guid.
                // Check if the node is inside a group
                if (node.group != null)
                {
                    if (groupMap.ContainsKey(node.group))
                    {
                        var absNode = pastedNode;
                        absNode.group = groupMap[node.group];
                        pastedNode = absNode;
                    }
                    else
                    {
                        pastedNode.group = null;
                    }
                }

                remappedNodes.Add(pastedNode);
                AddNode(pastedNode);

                // add the node to the pasted node list
                m_PastedNodes.Add(pastedNode);

                // Check if the keyword nodes need to have their keywords copied.
                if (node is KeywordNode keywordNode)
                {
                    var keyword = m_Keywords.SelectValue().FirstOrDefault(x => x.objectId == keywordNode.keyword.objectId
                        || (x.keywordType == keywordNode.keyword.keywordType && x.referenceName == keywordNode.keyword.referenceName));
                    if (keyword != null)
                    {
                        keywordNode.keyword = keyword;
                    }
                    else
                    {
                        owner.graphDataStore.Dispatch(new AddShaderInputAction() { shaderInputReference = keywordNode.keyword });
                    }

                    // Always update Keyword nodes to handle any collisions resolved on the Keyword
                    keywordNode.UpdateNode();
                }

                // Check if the dropdown nodes need to have their dropdowns copied.
                if (node is DropdownNode dropdownNode)
                {
                    var dropdown = m_Dropdowns.SelectValue().FirstOrDefault(x => x.objectId == dropdownNode.dropdown.objectId
                        || x.referenceName == dropdownNode.dropdown.referenceName);
                    if (dropdown != null)
                    {
                        dropdownNode.dropdown = dropdown;
                    }
                    else
                    {
                        owner.graphDataStore.Dispatch(new AddShaderInputAction() { shaderInputReference = dropdownNode.dropdown });
                    }

                    // Always update Dropdown nodes to handle any collisions resolved on the Keyword
                    dropdownNode.UpdateNode();
                }
            }

            foreach (var edge in edges)
            {
                var newEdge = (Edge)Connect(edge.outputSlot, edge.inputSlot);
                if (newEdge != null)
                {
                    remappedEdges.Add(newEdge);
                }
            }

            ValidateGraph();
        }

        public override void OnBeforeSerialize()
        {
            m_Edges.Sort();
            ChangeVersion(latestVersion);
        }

        static T DeserializeLegacy<T>(string typeString, string json, Guid? overrideObjectId = null) where T : JsonObject
        {
            var jsonObj = MultiJsonInternal.CreateInstanceForDeserialization(typeString);
            var value = jsonObj as T;
            if (value == null)
            {
                Debug.Log($"Cannot create instance for {typeString}");
                return null;
            }

            // by default, MultiJsonInternal.CreateInstance will create a new objectID randomly..
            // we need some created objects to have deterministic objectIDs, because they affect the generated shader.
            // if the generated shader is not deterministic, it can create ripple effects (i.e. causing Materials to be modified randomly as properties are renamed)
            // so we provide this path to allow the calling code to override the objectID with something deterministic
            if (overrideObjectId.HasValue)
                value.OverrideObjectId(overrideObjectId.Value.ToString("N"));
            MultiJsonInternal.Enqueue(value, json);
            return value as T;
        }

        static AbstractMaterialNode DeserializeLegacyNode(string typeString, string json, Guid? overrideObjectId = null)
        {
            var jsonObj = MultiJsonInternal.CreateInstanceForDeserialization(typeString);
            var value = jsonObj as AbstractMaterialNode;
            if (value == null)
            {
                //Special case - want to support nodes of unknwon type for cross pipeline compatability
                value = new LegacyUnknownTypeNode(typeString, json);
                if (overrideObjectId.HasValue)
                    value.OverrideObjectId(overrideObjectId.Value.ToString("N"));
                MultiJsonInternal.Enqueue(value, json);
                return value as AbstractMaterialNode;
            }
            else
            {
                if (overrideObjectId.HasValue)
                    value.OverrideObjectId(overrideObjectId.Value.ToString("N"));
                MultiJsonInternal.Enqueue(value, json);
                return value as AbstractMaterialNode;
            }
        }

        public override void OnAfterDeserialize(string json)
        {
            if (sgVersion == 0)
            {
                var graphData0 = JsonUtility.FromJson<GraphData0>(json);
                //If a graph was previously updated to V2, since we had to rename m_Version to m_SGVersion to avoid collision with an upgrade system from
                //HDRP, we have to handle the case that our version might not be correct -
                if (graphData0.m_Version > 0)
                {
                    sgVersion = graphData0.m_Version;
                }
                else
                {
                    // graphData.m_Version == 0  (matches current sgVersion)
                    Guid assetGuid;
                    if (!Guid.TryParse(this.assetGuid, out assetGuid))
                        assetGuid = JsonObject.GenerateNamespaceUUID(Guid.Empty, json);

                    var nodeGuidMap = new Dictionary<string, AbstractMaterialNode>();
                    var propertyGuidMap = new Dictionary<string, AbstractShaderProperty>();
                    var keywordGuidMap = new Dictionary<string, ShaderKeyword>();
                    var groupGuidMap = new Dictionary<string, GroupData>();
                    var slotsField = typeof(AbstractMaterialNode).GetField("m_Slots", BindingFlags.Instance | BindingFlags.NonPublic);
                    var propertyField = typeof(PropertyNode).GetField("m_Property", BindingFlags.Instance | BindingFlags.NonPublic);
                    var keywordField = typeof(KeywordNode).GetField("m_Keyword", BindingFlags.Instance | BindingFlags.NonPublic);
                    var dropdownField = typeof(DropdownNode).GetField("m_Dropdown", BindingFlags.Instance | BindingFlags.NonPublic);
                    var defaultReferenceNameField = typeof(ShaderInput).GetField("m_DefaultReferenceName", BindingFlags.Instance | BindingFlags.NonPublic);

                    m_GroupDatas.Clear();
                    m_StickyNoteDatas.Clear();

                    foreach (var group0 in graphData0.m_Groups)
                    {
                        var group = new GroupData(group0.m_Title, group0.m_Position);
                        m_GroupDatas.Add(group);
                        if (!groupGuidMap.ContainsKey(group0.m_GuidSerialized))
                        {
                            groupGuidMap.Add(group0.m_GuidSerialized, group);
                        }
                        else if (!groupGuidMap[group0.m_GuidSerialized].Equals(group.objectId))
                        {
                            Debug.LogError("Group id mismatch");
                        }
                    }

                    foreach (var serializedProperty in graphData0.m_SerializedProperties)
                    {
                        var propObjectId = JsonObject.GenerateNamespaceUUID(assetGuid, serializedProperty.JSONnodeData);
                        var property = DeserializeLegacy<AbstractShaderProperty>(serializedProperty.typeInfo.fullName, serializedProperty.JSONnodeData, propObjectId);
                        if (property == null)
                            continue;

                        m_Properties.Add(property);

                        var input0 = JsonUtility.FromJson<ShaderInput0>(serializedProperty.JSONnodeData);
                        propertyGuidMap[input0.m_Guid.m_GuidSerialized] = property;

                        // Fix up missing reference names
                        // Properties on Sub Graphs in V0 never have reference names serialized
                        // To maintain Sub Graph node property mapping we force guid based reference names on upgrade
                        if (string.IsNullOrEmpty((string)defaultReferenceNameField.GetValue(property)))
                        {
                            // ColorShaderProperty is the only Property case where `GetDefaultReferenceName` was overriden
                            if (MultiJson.ParseType(serializedProperty.typeInfo.fullName) == typeof(ColorShaderProperty))
                            {
                                defaultReferenceNameField.SetValue(property, $"Color_{GuidEncoder.Encode(Guid.Parse(input0.m_Guid.m_GuidSerialized))}");
                            }
                            else
                            {
                                defaultReferenceNameField.SetValue(property, $"{property.concreteShaderValueType}_{GuidEncoder.Encode(Guid.Parse(input0.m_Guid.m_GuidSerialized))}");
                            }
                        }
                    }

                    foreach (var serializedKeyword in graphData0.m_SerializedKeywords)
                    {
                        var keyword = DeserializeLegacy<ShaderKeyword>(serializedKeyword.typeInfo.fullName, serializedKeyword.JSONnodeData);
                        if (keyword == null)
                        {
                            continue;
                        }

                        m_Keywords.Add(keyword);

                        var input0 = JsonUtility.FromJson<ShaderInput0>(serializedKeyword.JSONnodeData);
                        keywordGuidMap[input0.m_Guid.m_GuidSerialized] = keyword;
                    }

                    foreach (var serializedNode in graphData0.m_SerializableNodes)
                    {
                        var node0 = JsonUtility.FromJson<AbstractMaterialNode0>(serializedNode.JSONnodeData);

                        var nodeObjectId = JsonObject.GenerateNamespaceUUID(node0.m_GuidSerialized, "node");
                        var node = DeserializeLegacyNode(serializedNode.typeInfo.fullName, serializedNode.JSONnodeData, nodeObjectId);
                        if (node == null)
                        {
                            continue;
                        }

                        nodeGuidMap.Add(node0.m_GuidSerialized, node);
                        m_Nodes.Add(node);

                        if (!string.IsNullOrEmpty(node0.m_PropertyGuidSerialized) && propertyGuidMap.TryGetValue(node0.m_PropertyGuidSerialized, out var property))
                        {
                            propertyField.SetValue(node, (JsonRef<AbstractShaderProperty>)property);
                        }

                        if (!string.IsNullOrEmpty(node0.m_KeywordGuidSerialized) && keywordGuidMap.TryGetValue(node0.m_KeywordGuidSerialized, out var keyword))
                        {
                            keywordField.SetValue(node, (JsonRef<ShaderKeyword>)keyword);
                        }

                        var slots = (List<JsonData<MaterialSlot>>)slotsField.GetValue(node);
                        slots.Clear();

                        foreach (var serializedSlot in node0.m_SerializableSlots)
                        {
                            var slotObjectId = JsonObject.GenerateNamespaceUUID(node0.m_GuidSerialized, serializedSlot.JSONnodeData);
                            var slot = DeserializeLegacy<MaterialSlot>(serializedSlot.typeInfo.fullName, serializedSlot.JSONnodeData, slotObjectId);
                            if (slot == null)
                            {
                                continue;
                            }

                            slots.Add(slot);
                        }

                        if (!String.IsNullOrEmpty(node0.m_GroupGuidSerialized))
                        {
                            if (groupGuidMap.TryGetValue(node0.m_GroupGuidSerialized, out GroupData foundGroup))
                            {
                                node.group = foundGroup;
                            }
                        }
                    }

                    foreach (var stickyNote0 in graphData0.m_StickyNotes)
                    {
                        var stickyNote = new StickyNoteData(stickyNote0.m_Title, stickyNote0.m_Content, stickyNote0.m_Position);
                        if (!String.IsNullOrEmpty(stickyNote0.m_GroupGuidSerialized))
                        {
                            if (groupGuidMap.TryGetValue(stickyNote0.m_GroupGuidSerialized, out GroupData foundGroup))
                            {
                                stickyNote.group = foundGroup;
                            }
                        }
                        stickyNote.theme = stickyNote0.m_Theme;
                        stickyNote.textSize = stickyNote0.m_TextSize;
                        m_StickyNoteDatas.Add(stickyNote);
                    }

                    var subgraphOuput = GetNodes<SubGraphOutputNode>();
                    isSubGraph = subgraphOuput.Any();

                    if (isSubGraph)
                    {
                        m_OutputNode = subgraphOuput.FirstOrDefault();
                    }
                    else if (!string.IsNullOrEmpty(graphData0.m_ActiveOutputNodeGuidSerialized))
                    {
                        m_OutputNode = nodeGuidMap[graphData0.m_ActiveOutputNodeGuidSerialized];
                    }
                    else
                    {
                        m_OutputNode = (AbstractMaterialNode)GetNodes<IMasterNode1>().FirstOrDefault();
                    }

                    foreach (var serializedElement in graphData0.m_SerializableEdges)
                    {
                        var edge0 = JsonUtility.FromJson<Edge0>(serializedElement.JSONnodeData);
                        m_Edges.Add(new Edge(
                            new SlotReference(
                                nodeGuidMap[edge0.m_OutputSlot.m_NodeGUIDSerialized],
                                edge0.m_OutputSlot.m_SlotId),
                            new SlotReference(
                                nodeGuidMap[edge0.m_InputSlot.m_NodeGUIDSerialized],
                                edge0.m_InputSlot.m_SlotId)));
                    }
                }
            }
        }

        [Serializable]
        class OldGraphDataReadConcretePrecision
        {
            // old value just for upgrade
            [SerializeField]
            public ConcretePrecision m_ConcretePrecision = ConcretePrecision.Single;
        };

        public override void OnAfterMultiDeserialize(string json)
        {
            // Deferred upgrades
            if (sgVersion != latestVersion)
            {
                if (sgVersion < 2)
                {
                    var addedBlocks = ListPool<BlockFieldDescriptor>.Get();

                    void UpgradeFromBlockMap(Dictionary<BlockFieldDescriptor, int> blockMap)
                    {
                        // Map master node ports to blocks
                        if (blockMap != null)
                        {
                            foreach (var blockMapping in blockMap)
                            {
                                // Create a new BlockNode for each unique map entry
                                var descriptor = blockMapping.Key;
                                if (addedBlocks.Contains(descriptor))
                                    continue;

                                addedBlocks.Add(descriptor);

                                var contextData = descriptor.shaderStage == ShaderStage.Fragment ? m_FragmentContext : m_VertexContext;
                                var block = (BlockNode)Activator.CreateInstance(typeof(BlockNode));
                                block.Init(descriptor);
                                AddBlockNoValidate(block, contextData, contextData.blocks.Count);

                                // To avoid having to go around the following deserialization code
                                // We simply run OnBeforeSerialization here to ensure m_SerializedDescriptor is set
                                block.OnBeforeSerialize();

                                // Now remap the incoming edges to blocks
                                var slotId = blockMapping.Value;
                                var oldSlot = m_OutputNode.value.FindSlot<MaterialSlot>(slotId);
                                var newSlot = block.FindSlot<MaterialSlot>(0);
                                if (oldSlot == null)
                                    continue;

                                var oldInputSlotRef = m_OutputNode.value.GetSlotReference(slotId);
                                var newInputSlotRef = block.GetSlotReference(0);

                                // Always copy the value over for convenience
                                newSlot.CopyValuesFrom(oldSlot);

                                for (int i = 0; i < m_Edges.Count; i++)
                                {
                                    // Find all edges connected to the master node using slot ID from the block map
                                    // Remove them and replace them with new edges connected to the block nodes
                                    var edge = m_Edges[i];
                                    if (edge.inputSlot.Equals(oldInputSlotRef))
                                    {
                                        var outputSlot = edge.outputSlot;
                                        m_Edges.Remove(edge);
                                        m_Edges.Add(new Edge(outputSlot, newInputSlotRef));
                                    }
                                }

                                // manually handle a bug where fragment normal slots could get out of sync of the master node's set fragment normal space
                                if (descriptor == BlockFields.SurfaceDescription.NormalOS)
                                {
                                    NormalMaterialSlot norm = newSlot as NormalMaterialSlot;
                                    if (norm.space != CoordinateSpace.Object)
                                    {
                                        norm.space = CoordinateSpace.Object;
                                    }
                                }
                                else if (descriptor == BlockFields.SurfaceDescription.NormalTS)
                                {
                                    NormalMaterialSlot norm = newSlot as NormalMaterialSlot;
                                    if (norm.space != CoordinateSpace.Tangent)
                                    {
                                        norm.space = CoordinateSpace.Tangent;
                                    }
                                }
                                else if (descriptor == BlockFields.SurfaceDescription.NormalWS)
                                {
                                    NormalMaterialSlot norm = newSlot as NormalMaterialSlot;
                                    if (norm.space != CoordinateSpace.World)
                                    {
                                        norm.space = CoordinateSpace.World;
                                    }
                                }
                            }

                            // We need to call AddBlockNoValidate but this adds to m_AddedNodes resulting in duplicates
                            // Therefore we need to clear this list before the view is created
                            m_AddedNodes.Clear();
                        }
                    }

                    var masterNode = m_OutputNode.value as IMasterNode1;

                    // This is required for edge lookup during Target upgrade
                    if (m_OutputNode.value != null)
                    {
                        m_OutputNode.value.owner = this;
                    }
                    foreach (var edge in m_Edges)
                    {
                        AddEdgeToNodeEdges(edge);
                    }

                    // Ensure correct initialization of Contexts
                    AddContexts();

                    // Position Contexts to the match master node
                    var oldPosition = Vector2.zero;
                    if (m_OutputNode.value != null)
                    {
                        oldPosition = m_OutputNode.value.drawState.position.position;
                    }
                    m_VertexContext.position = oldPosition;
                    m_FragmentContext.position = new Vector2(oldPosition.x, oldPosition.y + 200);

                    // Try to upgrade all potential targets from master node
                    if (masterNode != null)
                    {
                        foreach (var potentialTarget in m_AllPotentialTargets)
                        {
                            if (potentialTarget.IsUnknown())
                                continue;

                            var target = potentialTarget.GetTarget();
                            if (!(target is ILegacyTarget legacyTarget))
                                continue;

                            if (!legacyTarget.TryUpgradeFromMasterNode(masterNode, out var newBlockMap))
                                continue;

                            // upgrade succeeded!  Activate it
                            SetTargetActive(target, true);
                            UpgradeFromBlockMap(newBlockMap);
                        }
                        SortActiveTargets();
                    }

                    // Clean up after upgrade
                    if (!isSubGraph)
                    {
                        m_OutputNode = null;
                    }

                    var masterNodes = GetNodes<IMasterNode1>().ToArray();
                    for (int i = 0; i < masterNodes.Length; i++)
                    {
                        var node = masterNodes.ElementAt(i) as AbstractMaterialNode;
                        m_Nodes.Remove(node);
                    }

                    m_NodeEdges.Clear();
                }

                if (sgVersion < 3)
                {
                    var oldGraph = JsonUtility.FromJson<OldGraphDataReadConcretePrecision>(json);

                    // upgrade concrete precision to the new graph precision
                    switch (oldGraph.m_ConcretePrecision)
                    {
                        case ConcretePrecision.Half:
                            m_GraphPrecision = GraphPrecision.Half;
                            break;
                        case ConcretePrecision.Single:
                            m_GraphPrecision = GraphPrecision.Single;
                            break;
                    }
                }

                ChangeVersion(latestVersion);
            }

            PooledList<(LegacyUnknownTypeNode, AbstractMaterialNode)> updatedNodes = PooledList<(LegacyUnknownTypeNode, AbstractMaterialNode)>.Get();
            foreach (var node in m_Nodes.SelectValue())
            {
                if (node is LegacyUnknownTypeNode lNode && lNode.foundType != null)
                {
                    AbstractMaterialNode legacyNode = (AbstractMaterialNode)Activator.CreateInstance(lNode.foundType);
                    JsonUtility.FromJsonOverwrite(lNode.serializedData, legacyNode);
                    legacyNode.group = lNode.group;
                    updatedNodes.Add((lNode, legacyNode));
                }
            }
            foreach (var nodePair in updatedNodes)
            {
                m_Nodes.Add(nodePair.Item2);
                ReplaceNodeWithNode(nodePair.Item1, nodePair.Item2);
            }
            updatedNodes.Dispose();

            m_NodeDictionary = new Dictionary<string, AbstractMaterialNode>(m_Nodes.Count);

            foreach (var group in m_GroupDatas.SelectValue())
            {
                m_GroupItems.Add(group, new List<IGroupItem>());
            }

            foreach (var node in m_Nodes.SelectValue())
            {
                node.owner = this;
                node.UpdateNodeAfterDeserialization();
                node.SetupSlots();
                m_NodeDictionary.Add(node.objectId, node);
                if (m_GroupItems.TryGetValue(node.group, out var groupItems))
                {
                    groupItems.Add(node);
                }
                else
                {
                    node.group = null;
                }
            }

            foreach (var stickyNote in m_StickyNoteDatas.SelectValue())
            {
                if (m_GroupItems.TryGetValue(stickyNote.group, out var groupItems))
                {
                    groupItems.Add(stickyNote);
                }
                else
                {
                    stickyNote.group = null;
                }
            }

            foreach (var edge in m_Edges)
                AddEdgeToNodeEdges(edge);

            // --------------------------------------------------
            // Deserialize Contexts & Blocks

            void DeserializeContextData(ContextData contextData, ShaderStage stage)
            {
                // Because Vertex/Fragment Contexts are serialized explicitly
                // we do not need to serialize the Stage value on the ContextData
                contextData.shaderStage = stage;

                var blocks = contextData.blocks.SelectValue().ToList();
                var blockCount = blocks.Count;
                for (int i = 0; i < blockCount; i++)
                {
                    // Update NonSerialized data on the BlockNode
                    var block = blocks[i];
                    // custom interpolators fully regenerate their own descriptor on deserialization
                    if (!block.isCustomBlock)
                    {
                        block.descriptor = m_BlockFieldDescriptors.FirstOrDefault(x => $"{x.tag}.{x.name}" == block.serializedDescriptor);
                    }
                    if (block.descriptor == null)
                    {
                        //Hit a descriptor that was not recognized from the assembly (likely from a different SRP)
                        //create a new entry for it and continue on
                        if (string.IsNullOrEmpty(block.serializedDescriptor))
                        {
                            throw new Exception($"Block {block} had no serialized descriptor");
                        }

                        var tmp = block.serializedDescriptor.Split('.');
                        if (tmp.Length != 2)
                        {
                            throw new Exception($"Block {block}'s serialized descriptor {block.serializedDescriptor} did not match expected format {{x.tag}}.{{x.name}}");
                        }
                        //right thing to do?
                        block.descriptor = new BlockFieldDescriptor(tmp[0], tmp[1], null, null, stage, true, true);
                        m_BlockFieldDescriptors.Add(block.descriptor);
                    }
                    block.contextData = contextData;
                }
            }

            // First deserialize the ContextDatas
            DeserializeContextData(m_VertexContext, ShaderStage.Vertex);
            DeserializeContextData(m_FragmentContext, ShaderStage.Fragment);

            // there should be no unknown potential targets at this point
            Assert.IsFalse(m_AllPotentialTargets.Any(pt => pt.IsUnknown()));

            foreach (var target in m_ActiveTargets.SelectValue())
            {
                var targetType = target.GetType();
                if (targetType == typeof(MultiJsonInternal.UnknownTargetType))
                {
                    // register any active UnknownTargetType as a potential target
                    m_AllPotentialTargets.Add(new PotentialTarget(target));
                }
                else
                {
                    // active known targets should replace the stored Target in AllPotentialTargets
                    int targetIndex = m_AllPotentialTargets.FindIndex(pt => pt.knownType == targetType);
                    m_AllPotentialTargets[targetIndex].ReplaceStoredTarget(target);
                }
            }

            SortActiveTargets();
        }

        private void ReplaceNodeWithNode(LegacyUnknownTypeNode nodeToReplace, AbstractMaterialNode nodeReplacement)
        {
            var oldSlots = new List<MaterialSlot>();
            nodeToReplace.GetSlots(oldSlots);
            var newSlots = new List<MaterialSlot>();
            nodeReplacement.GetSlots(newSlots);

            for (int i = 0; i < oldSlots.Count; i++)
            {
                newSlots[i].CopyValuesFrom(oldSlots[i]);
                var oldSlotRef = nodeToReplace.GetSlotReference(oldSlots[i].id);
                var newSlotRef = nodeReplacement.GetSlotReference(newSlots[i].id);

                for (int x = 0; x < m_Edges.Count; x++)
                {
                    var edge = m_Edges[x];
                    if (edge.inputSlot.Equals(oldSlotRef))
                    {
                        var outputSlot = edge.outputSlot;
                        m_Edges.Remove(edge);
                        m_Edges.Add(new Edge(outputSlot, newSlotRef));
                    }
                    else if (edge.outputSlot.Equals(oldSlotRef))
                    {
                        var inputSlot = edge.inputSlot;
                        m_Edges.Remove(edge);
                        m_Edges.Add(new Edge(newSlotRef, inputSlot));
                    }
                }
            }
        }

        public void OnEnable()
        {
            foreach (var node in GetNodes<AbstractMaterialNode>().OfType<IOnAssetEnabled>())
            {
                node.OnEnable();
            }

            // OnEnable may be called multiple times. Ensure the callback only exists once.
            ShaderGraphPreferences.onVariantLimitChanged -= OnKeywordChanged;
            ShaderGraphPreferences.onVariantLimitChanged += OnKeywordChanged;
        }

        public void OnDisable()
        {
            ShaderGraphPreferences.onVariantLimitChanged -= OnKeywordChanged;

            foreach (var node in GetNodes<AbstractMaterialNode>())
                node.Dispose();
        }

        internal void ValidateCustomBlockLimit()
        {
            if (m_ActiveTargets.Count() == 0)
                return;

            int nonCustomUsage = 0;
            foreach (var bnode in vertexContext.blocks.Where(jb => !jb.value.isCustomBlock).Select(b => b.value))
            {
                if (bnode == null || bnode.descriptor == null)
                    continue;

                if (bnode.descriptor.HasPreprocessor() || bnode.descriptor.HasSemantic() || bnode.descriptor.vectorCount == 0) // not packable.
                    nonCustomUsage += 4;
                else nonCustomUsage += bnode.descriptor.vectorCount;
            }
            int maxTargetUsage = m_ActiveTargets.Select(jt => jt.value.padCustomInterpolatorLimit).Max() * 4;

            int padding = nonCustomUsage + maxTargetUsage;

            int errRange = ShaderGraphProjectSettings.instance.customInterpolatorErrorThreshold;
            int warnRange = ShaderGraphProjectSettings.instance.customInterpolatorWarningThreshold;

            int errorLevel = errRange * 4 - padding;
            int warnLevel = warnRange * 4 - padding;

            int total = 0;

            // warn based on the interpolator's location in the block list.
            foreach (var cib in vertexContext.blocks.Where(jb => jb.value.isCustomBlock).Select(b => b.value))
            {
                ClearErrorsForNode(cib);
                total += (int)cib.customWidth;
                if (total > errorLevel)
                {
                    AddValidationError(cib.objectId, $"{cib.customName} exceeds the interpolation channel error threshold: {errRange}. See ShaderGraph project settings.");
                }
                else if (total > warnLevel)
                {
                    AddValidationError(cib.objectId, $"{cib.customName} exceeds the interpolation channel warning threshold: {warnRange}. See ShaderGraph project settings.", ShaderCompilerMessageSeverity.Warning);
                }
            }
        }

        void ValidateContextBlocks()
        {
            void ValidateContext(ContextData contextData, ShaderStage expectedShaderStage)
            {
                if (contextData == null)
                    return;

                foreach (var block in contextData.blocks)
                {
                    var slots = block.value.GetInputSlots<MaterialSlot>();
                    foreach (var slot in slots)
                        FindAndReportSlotErrors(slot, expectedShaderStage);
                }
            };

            ValidateContext(vertexContext, ShaderStage.Vertex);
            ValidateContext(fragmentContext, ShaderStage.Fragment);
        }

        void FindAndReportSlotErrors(MaterialSlot initialSlot, ShaderStage expectedShaderStage)
        {
            var expectedCapability = expectedShaderStage.GetShaderStageCapability();
            var errorSourceSlots = new HashSet<MaterialSlot>();
            var visitedNodes = new HashSet<AbstractMaterialNode>();

            var graph = initialSlot.owner.owner;
            var slotStack = new Stack<MaterialSlot>();
            slotStack.Clear();
            slotStack.Push(initialSlot);

            // Trace back and find any edges that introduce an error
            while (slotStack.Any())
            {
                var slot = slotStack.Pop();

                // If the slot is an input, jump across the connected edge to the output it's connected to
                if (slot.isInputSlot)
                {
                    foreach (var edge in graph.GetEdges(slot.slotReference))
                    {
                        var node = edge.outputSlot.node;

                        var outputSlot = node.FindOutputSlot<MaterialSlot>(edge.outputSlot.slotId);
                        // If the output slot this is connected to is invalid then this is a source of an error.
                        // Mark the slot and stop iterating, otherwise continue the recursion
                        if (!outputSlot.stageCapability.HasFlag(expectedCapability))
                            errorSourceSlots.Add(outputSlot);
                        else
                            slotStack.Push(outputSlot);
                    }
                }
                else
                {
                    // No need to double visit nodes
                    if (visitedNodes.Contains(slot.owner))
                        continue;
                    visitedNodes.Add(slot.owner);

                    var ownerSlots = slot.owner.GetInputSlots<MaterialSlot>(slot);
                    foreach (var ownerSlot in ownerSlots)
                        slotStack.Push(ownerSlot);
                }
            }

            bool IsEntireNodeStageLocked(AbstractMaterialNode node, ShaderStageCapability expectedNodeCapability)
            {
                var slots = node.GetOutputSlots<MaterialSlot>();
                foreach (var slot in slots)
                {
                    if (expectedNodeCapability != slot.stageCapability)
                        return false;
                }
                return true;
            };

            foreach (var errorSourceSlot in errorSourceSlots)
            {
                var errorNode = errorSourceSlot.owner;

                // Determine if only one slot or the entire node is at fault. Currently only slots are
                // denoted with stage capabilities so deduce this by checking all outputs
                string errorSource;
                if (IsEntireNodeStageLocked(errorNode, errorSourceSlot.stageCapability))
                    errorSource = $"Node {errorNode.name}";
                else
                    errorSource = $"Slot {errorSourceSlot.RawDisplayName()}";

                // Determine what action they can take. If the stage capability is None then this can't be connected to anything.
                string actionToTake;
                if (errorSourceSlot.stageCapability != ShaderStageCapability.None)
                {
                    var validStageName = errorSourceSlot.stageCapability.ToString().ToLower();
                    actionToTake = $"reconnect to a {validStageName} block or delete invalid connection";
                }
                else
                    actionToTake = "delete invalid connection";

                var invalidStageName = expectedShaderStage.ToString().ToLower();
                string message = $"{errorSource} is not compatible with {invalidStageName} block {initialSlot.RawDisplayName()}, {actionToTake}.";
                AddValidationError(errorNode.objectId, message, ShaderCompilerMessageSeverity.Error);
            }
        }
    }

    [Serializable]
    class InspectorPreviewData
    {
        public SerializableMesh serializedMesh = new SerializableMesh();
        public bool preventRotation;

        [NonSerialized]
        public Quaternion rotation = Quaternion.identity;

        [NonSerialized]
        public float scale = 1f;
    }
}
