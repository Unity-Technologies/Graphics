using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.UIElements;
using System.Reflection;

using NodeID = System.UInt32;

namespace UnityEditor.VFX.UI
{





    class VFXCopyPaste
    {
        const NodeID ParameterFlag = 1u << 30;
        const NodeID ContextFlag = 2u << 30;
        const NodeID OperatorFlag = 3u << 30;
        const NodeID BlockFlag = 0u << 30;
        const NodeID InvalidID = 0xFFFFFFFF;

        //NodeID are 32 identifiers to any node that can be in a groupNode or have links
        //The two high bits are use with the above flags to give the type.
        // For operators and context all the 30 remaining bit are used as an index
        // For blocks the high bits 2 to 13 are a block index in the context the 18 remaining bits are an index in the contexts
        // For parameters the high bits 2 to 13 are a parameter node index the 18 remaining are an index in the parameters
        // Therefore you can copy :
        // Up to 2^30 operators
        // Up to 2^30 contexts, but only the first 2^18 can have blocks with links
        // Up to 2^30 parameters, but only the first 2^18 can have nodes with links
        // Up to 2^11 block per context can have links
        // Up to 2^11 parameter nodes per parameter can have links
        // Note : 2^30 > 1 billion, 2^18 = 262144, 2^11 = 2048


        [Serializable]
        struct DataAnchor
        {
            public NodeID targetIndex;
            public int[] slotPath;
        }

        [Serializable]
        struct DataEdge
        {
            public DataAnchor input;
            public DataAnchor output;
        }

        [Serializable]
        struct FlowAnchor
        {
            public NodeID contextIndex;
            public int flowIndex;
        }


        [Serializable]
        struct FlowEdge
        {
            public FlowAnchor input;
            public FlowAnchor output;
        }

        [Serializable]
        struct Property
        {
            public string name;
            public VFXSerializableObject value;
        }

        [Serializable]
        struct Node
        {
            public Vector2 position;

            [Flags]
            public enum Flags
            {
                Collapsed = 1 << 0,
                SuperCollapsed = 1 << 1,
                Enabled = 1 << 2
            }
            public Flags flags;

            public SerializableType type;
            public Property[] settings;
            public Property[] inputSlots;
            public string[] expandedInputs;
            public string[] expandedOutputs;
        }



        [Serializable]
        struct Context
        {
            public Node node;
            public int dataIndex;
            public Node[] blocks;
        }

        [Serializable]
        struct Data
        {
            public Property[] settings;
        }

        [Serializable]
        struct ParameterNode
        {
            public Vector2 position;
            public bool collapsed;
            public string[] expandedOutput;
        }

        [Serializable]
        struct Parameter
        {
            public int originalInstanceID;
            public string name;
            public VFXSerializableObject value;
            public bool exposed;
            public bool range;
            public VFXSerializableObject min;
            public VFXSerializableObject max;
            public string tooltip;
            public ParameterNode[] nodes;
        }

        [Serializable]
        struct GroupNode
        {
            public VFXUI.UIInfo infos;
            public NodeID[] contents;
            public int stickNodeCount;
        }

        [Serializable]
        class SerializableGraph
        {
            public Rect bounds;

            public bool blocksOnly;

            public Context[] contexts;
            public Node[] operators;
            public Data[] datas;

            public Parameter[] parameters;
            
            public DataEdge[] dataEdges;
            public FlowEdge[] flowEdges;

            public VFXUI.StickyNoteInfo[] stickyNotes;
            public GroupNode[] groupNodes;
        }

        static void CopyGroupNodesAndStickyNotes(ref SerializableGraph data,IEnumerable<Controller> elements, ref CopyInfo infos)
        {
            VFXGroupNodeController[] groupNodes = elements.OfType<VFXGroupNodeController>().ToArray();
            VFXStickyNoteController[] stickyNotes = elements.OfType<VFXStickyNoteController>().ToArray();

            if (groupNodes.Length > 0 || stickyNotes.Length > 0)
            {

                var stickyNodeIndexToCopiedIndex = new Dictionary<int, int>();

                if (stickyNotes.Length > 0)
                {
                    data.stickyNotes = new VFXUI.StickyNoteInfo[stickyNotes.Length];

                    for (int i = 0; i < stickyNotes.Length; ++i)
                    {
                        VFXStickyNoteController stickyNote = stickyNotes[i];
                        stickyNodeIndexToCopiedIndex[stickyNote.index] = i;
                        VFXUI.StickyNoteInfo info = stickyNote.model.stickyNoteInfos[stickyNote.index];
                        data.stickyNotes[i] = new VFXUI.StickyNoteInfo(info);
                    }
                }

                if (groupNodes.Length > 0)
                {
                    data.groupNodes = new GroupNode[groupNodes.Length];
                    for (int i = 0; i < groupNodes.Length; ++i)
                    {
                        VFXGroupNodeController groupNode = groupNodes[i];
                        VFXUI.GroupInfo info = groupNode.model.groupInfos[groupNode.index];
                        
                        data.groupNodes[i] = new GroupNode { infos =new VFXUI.UIInfo(info)};

                        // only keep nodes and sticky notes that are copied because a element can not be in two groups at the same time.
                        if (info.contents != null)
                        {
                            var groupInfo = data.groupNodes[i];
                            var contexts = infos.contexts;
                            var nodes = infos.nodes;
                            var modelIndices = infos.modelIndices;


                            var nodeIndices = info.contents.Where(t => !t.isStickyNote && contexts.Contains(t.model) || nodes.Contains(t.model)).Select(t => modelIndices[t.model]);
                            var stickNoteIndices = info.contents.Where(t => t.isStickyNote && stickyNodeIndexToCopiedIndex.ContainsKey(t.id)).Select(t => (uint)stickyNodeIndexToCopiedIndex[t.id]);

                            groupInfo.contents = nodeIndices.Concat(stickNoteIndices).ToArray();
                            groupInfo.stickNodeCount = stickNoteIndices.Count();

                        }
                    }
                }
            }
        }

        static void CopyDataEdge(ref SerializableGraph copyData, IEnumerable<VFXDataEdgeController> dataEdges, ref CopyInfo infos )
        {
            copyData.dataEdges = new DataEdge[dataEdges.Count()];
            int cpt = 0;

            var orderedEdges = new List<VFXDataEdgeController>();

            var edges = new HashSet<VFXDataEdgeController>(dataEdges);

            // Ensure that operators that can change shape always all their input edges created before their output edges and in the same order
            bool sortFailed = false;
            try
            {
                while (edges.Count > 0)
                {
                    var edgeInputs = edges.GroupBy(t => t.input.sourceNode).ToDictionary(t => t.Key, t => t.Select(u => u));

                    //Select the edges that have an input node which all its input edges have an output node that have no input edge
                    // Order them by index

                    var edgesWithoutParent = edges.Where(t => !edgeInputs[t.input.sourceNode].Any(u => edgeInputs.ContainsKey(u.output.sourceNode))).OrderBy(t => t.input.model.GetMasterSlot().owner.GetSlotIndex(t.input.model.GetMasterSlot())).ToList();
                    /*foreach(var gen in edgesWithoutParent)
                    {
                        int index = gen.input.model.GetMasterSlot().owner.GetSlotIndex(gen.input.model.GetMasterSlot());
                        Debug.Log("Edge with input:" + gen.input.sourceNode.title + "index"+ index);
                    }*/
                    orderedEdges.AddRange(edgesWithoutParent);

                    int count = edges.Count;
                    foreach (var e in edgesWithoutParent)
                    {
                        edges.Remove(e);
                    }
                    if (edges.Count >= count)
                    {
                        sortFailed = true;
                        Debug.LogError("Sorting of data edges failed. Please provide a screenshot of the graph with the selected node to @tristan");
                        break;
                    }
                    //Debug.Log("------------------------------");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Sorting of data edges threw. Please provide a screenshot of the graph with the selected node to @tristan" + e.Message);
                sortFailed = true;
            }

            IEnumerable<VFXDataEdgeController> usedEdges = sortFailed ? dataEdges : orderedEdges;

            foreach (var edge in usedEdges)
            {
                DataEdge copyPasteEdge = new DataEdge();

                var inputController = edge.input as VFXDataAnchorController;
                var outputController = edge.output as VFXDataAnchorController;

                copyPasteEdge.input.slotPath = MakeSlotPath(inputController.model, true);
                copyPasteEdge.input.targetIndex = infos.modelIndices[inputController.model.owner as VFXModel];

                copyPasteEdge.output.slotPath = MakeSlotPath(outputController.model, false);
                copyPasteEdge.output.targetIndex = infos.modelIndices[outputController.model.owner as VFXModel];

                copyData.dataEdges[cpt++] = copyPasteEdge;
            }
        }

        static void CopyFlowEdges(ref SerializableGraph copyData, IEnumerable<VFXFlowEdgeController> flowEdges, ref CopyInfo infos)
        {
            copyData.flowEdges = new FlowEdge[flowEdges.Count()];
            int cpt = 0;
            foreach (var edge in flowEdges)
            {
                FlowEdge copyPasteEdge = new FlowEdge();

                var inputController = edge.input as VFXFlowAnchorController;
                var outputController = edge.output as VFXFlowAnchorController;

                copyPasteEdge.input.contextIndex = infos.modelIndices[inputController.owner];
                copyPasteEdge.input.flowIndex = inputController.slotIndex;
                copyPasteEdge.output.contextIndex = infos.modelIndices[outputController.owner];
                copyPasteEdge.output.flowIndex = outputController.slotIndex;

                copyData.flowEdges[cpt++] = copyPasteEdge;
            }
        }

        static void CopyDatas(ref SerializableGraph copyData, ref CopyInfo infos)
        {
            copyData.datas = new Data[infos.datas.Length];
            for (int i = 0; i < infos.datas.Length; ++i)
            {
                CopyModelSettings(ref copyData.datas[i].settings, infos.datas[i]);
            }
        }


        static Dictionary<Type, List<FieldInfo>> s_SerializableFieldByType = new Dictionary<Type, List<FieldInfo>>();

        static void CopyModelSettings(ref Property[] properties, VFXModel model)
        {
            // Copy all fields that are either VFXSettings or serialized by unity
            Type type = model.GetType();

            List<FieldInfo> fields = null;
            if (!s_SerializableFieldByType.TryGetValue(type, out fields))
            {
                fields = new List<FieldInfo>();
                while (type != typeof(VFXModel))
                {
                    var typeFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    foreach (var field in typeFields)
                    {
                        if (!field.IsPublic)
                        {
                            object[] attributes = field.GetCustomAttributes(true);

                            if (!attributes.Any(t => t is VFXSettingAttribute || t is SerializeField))
                                continue;
                        }

                        fields.Add(field);
                    }
                    type = type.BaseType;
                }
                s_SerializableFieldByType[type] = fields;
            }

            properties = new Property[fields.Count()];

            for(int i = 0; i < properties.Length; ++i)
            {
                properties[i].name = fields[i].Name;
                properties[i].value = new VFXSerializableObject(fields[i].FieldType, fields[i].GetValue(model));
            }
        }


        public struct CopyInfo
        {
            public VFXContext[] contexts;
            public VFXModel[] nodes;
            public VFXData[] datas;
            public Dictionary<VFXModel, uint> modelIndices;
        }

        static void CopyNodes(SerializableGraph copyData, IEnumerable<Controller> elements, IEnumerable<VFXContextController> contexts, IEnumerable<VFXNodeController> slotContainers, Rect bounds)
        {
            copyData.bounds = bounds;
            IEnumerable<VFXNodeController> dataEdgeTargets = slotContainers.Concat(contexts.Cast<VFXNodeController>()).Concat(contexts.SelectMany(t => t.blockControllers).Cast<VFXNodeController>()).ToArray();

            // consider only edges contained in the selection

            IEnumerable<VFXDataEdgeController> dataEdges = elements.OfType<VFXDataEdgeController>().Where(t => dataEdgeTargets.Contains((t.input as VFXDataAnchorController).sourceNode as VFXNodeController) && dataEdgeTargets.Contains((t.output as VFXDataAnchorController).sourceNode as VFXNodeController)).ToArray();
            IEnumerable<VFXFlowEdgeController> flowEdges = elements.OfType<VFXFlowEdgeController>().Where(t =>
                    contexts.Contains((t.input as VFXFlowAnchorController).context) &&
                    contexts.Contains((t.output as VFXFlowAnchorController).context)
                    ).ToArray();


            CopyInfo infos = new CopyInfo();
            infos.contexts = contexts.Select(t => t.model).ToArray();
            infos.nodes = slotContainers.Select(t => t.model).ToArray();
            CopyNodesAndContexts(ref copyData, ref infos);

            VFXParameterNodeController[] parameters = slotContainers.OfType<VFXParameterNodeController>().ToArray();

            copyData.parameters = parameters.GroupBy(t => t.parentController, t => t.infos, (p, i) => new Parameter() {
                originalInstanceID = p.model.GetInstanceID(),
                name = p.model.exposedName,
                value = new VFXSerializableObject(p.model.type, p.model.value),
                exposed = p.model.exposed,
                range = p.hasRange,
                min = p.hasRange ? p.model.m_Min : null,
                max = p.hasRange ? p.model.m_Max : null,
                tooltip = p.model.tooltip,
                nodes = i.Select(u=>CopyParameterNode(u)).ToArray()
            }).ToArray();

            infos.datas = infos.contexts.Select(t => t.GetData()).Where(t => t != null).ToArray();

            CopyGroupNodesAndStickyNotes(ref copyData, elements, ref infos);

            CopyDatas(ref copyData, ref infos);

            CopyDataEdge(ref copyData, dataEdges, ref infos);

            CopyFlowEdges(ref copyData, flowEdges, ref infos);
        }

        static ParameterNode CopyParameterNode(VFXParameter.Node node)
        {
            ParameterNode n = new ParameterNode();
            n.position = node.position;
            n.collapsed = !node.expanded;
            n.expandedOutput = node.expandedSlots.Select(t => t.path).ToArray();
            return n;
        }

        static void CopyNodesAndContexts(ref SerializableGraph copyData,ref CopyInfo infos)
        {
            copyData.contexts = new Context[infos.contexts.Length];

            for (int i = 0; i < infos.contexts.Length; ++i)
            {
                NodeID id = CopyContext(ref copyData.contexts[i], infos.contexts[i],i, ref infos);
                infos.modelIndices[infos.contexts[i]] = id;
            }

            var operators = infos.nodes.Where(t => t is VFXOperator).ToArray();

            copyData.operators = new Node[operators.Length];

            for(int i = 0; i < operators.Length; ++i)
            {
                uint id = CopyNode(ref copyData.operators[i],operators[i],i);
                infos.modelIndices[operators[i]] = id;
            }
        }

        static NodeID CopyNode(ref Node node, VFXModel model,int index)
        {
            // Copy node infos
            node.position = model.position;
            node.type = model.GetType();
            node.flags = 0;
            if (model.collapsed)
                node.flags = Node.Flags.Collapsed;
            if (model.superCollapsed)
                node.flags = Node.Flags.SuperCollapsed;

            uint id = 0;
            if( model is VFXOperator)
            {
                id = OperatorFlag;
            }
            else if( model is VFXContext)
            {
                id = ContextFlag;
            }

            id |= (uint)index;

            //Copy settings value
            CopyModelSettings(ref node.settings, model);

            var inputSlots = (model as IVFXSlotContainer).inputSlots;
            node.inputSlots = new Property[inputSlots.Count];
            for (int i = 0; i < inputSlots.Count;i++ )
            {
                node.inputSlots[i].name = inputSlots[i].name;
                node.inputSlots[i].value = new VFXSerializableObject(inputSlots[i].property.type,inputSlots[i].value);
            }

            node.expandedInputs = AllSlots(inputSlots).Where(t => !t.collapsed).Select(t => t.path).ToArray();
            node.expandedOutputs = AllSlots((model as IVFXSlotContainer).outputSlots).Where(t => !t.collapsed).Select(t => t.path).ToArray();

            return id;
        }
        static IEnumerable<VFXSlot> AllSlots(IEnumerable<VFXSlot> slots)
        {
            foreach( var slot in slots)
            {
                yield return slot;

                foreach (var child in AllSlots(slot.children))
                {
                    yield return child;
                }
            }
        }

        static NodeID CopyContext(ref Context context,VFXContext model,int index, ref CopyInfo infos)
        {
            NodeID id = CopyNode(ref context.node, model,index);

            var blocks = model.children.ToArray();
            context.blocks = new Node[blocks.Length];
            for(uint i = 0; i< blocks.Length; ++i)
            {
                CopyNode(ref context.blocks[i], blocks[i],i);
                if( blocks[i].enabled)
                {
                    context.blocks[i].flags |= Node.Flags.Enabled;
                }

                if (index < (1 << 18) && i < (1 << 11))
                    infos.modelIndices[blocks[i]] = BlockFlag | (i << 18) | (uint)index;
                else
                    infos.modelIndices[blocks[i]] = InvalidID;
            }

            return id;
        }

        public static object CreateCopy(IEnumerable<Controller> elements, Rect bounds)
        {
            IEnumerable<VFXContextController> contexts = elements.OfType<VFXContextController>();
            IEnumerable<VFXNodeController> slotContainers = elements.Where(t => t is VFXOperatorController || t is VFXParameterNodeController).Cast<VFXNodeController>();
            IEnumerable<VFXBlockController> blocks = elements.OfType<VFXBlockController>();

            SerializableGraph copyData = new SerializableGraph();

            if (contexts.Count() == 0 && slotContainers.Count() == 0 && blocks.Count() > 0)
            {
                /*VFXBlock[] copiedBlocks = blocks.Select(t => t.block).ToArray();
                copyData.blocks = copiedBlocks;
                PrepareSerializedObjects(copyData, null);*/
                copyData.blocksOnly = true;
            }
            else
            {
                CopyNodes(copyData, elements, contexts, slotContainers, bounds);
            }

            return copyData;
        }

        public static string SerializeElements(IEnumerable<Controller> elements, Rect bounds)
        {
            var copyData = CreateCopy(elements, bounds) as SerializableGraph;

            return JsonUtility.ToJson(copyData);
        }

        static int[] MakeSlotPath(VFXSlot slot, bool input)
        {
            List<int> slotPath = new List<int>(slot.depth + 1);
            while (slot.GetParent() != null)
            {
                slotPath.Add(slot.GetParent().GetIndex(slot));
                slot = slot.GetParent();
            }
            slotPath.Add((input ? (slot.owner as IVFXSlotContainer).inputSlots : (slot.owner as IVFXSlotContainer).outputSlots).IndexOf(slot));

            return slotPath.ToArray();
        }

        static VFXSlot FetchSlot(IVFXSlotContainer container, int[] slotPath, bool input)
        {
            int containerSlotIndex = slotPath[slotPath.Length - 1];

            VFXSlot slot = null;
            if (input)
            {
                if (container.GetNbInputSlots() > containerSlotIndex)
                {
                    slot = container.GetInputSlot(slotPath[slotPath.Length - 1]);
                }
            }
            else
            {
                if (container.GetNbOutputSlots() > containerSlotIndex)
                {
                    slot = container.GetOutputSlot(slotPath[slotPath.Length - 1]);
                }
            }
            if (slot == null)
            {
                return null;
            }

            for (int i = slotPath.Length - 2; i >= 0; --i)
            {
                if (slot.GetNbChildren() > slotPath[i])
                {
                    slot = slot[slotPath[i]];
                }
                else
                {
                    return null;
                }
            }

            return slot;
        }

        public static void UnserializeAndPasteElements(VFXViewController viewController, Vector2 center, string data, VFXView view = null, VFXGroupNodeController groupNode = null)
        {
            var copyData = JsonUtility.FromJson<SerializableGraph>(data);

            ScriptableObject[] allSerializedObjects = VFXMemorySerializer.ExtractObjects(copyData.serializedObjects, true);

            copyData.contexts = allSerializedObjects.OfType<VFXContext>().ToArray();
            copyData.slotContainers = allSerializedObjects.OfType<IVFXSlotContainer>().Cast<VFXModel>().Where(t => !(t is VFXContext)).ToArray();
            if (copyData.contexts.Length == 0 && copyData.slotContainers.Length == 0)
            {
                copyData.contexts = null;
                copyData.slotContainers = null;
                copyData.blocks = allSerializedObjects.OfType<VFXBlock>().ToArray();
            }

            PasteCopy(viewController, center, copyData, allSerializedObjects, view, groupNode);
        }

        public static void PasteCopy(VFXViewController viewController, Vector2 center, object data, ScriptableObject[] allSerializedObjects, VFXView view, VFXGroupNodeController groupNode)
        {
            SerializableGraph copyData = (SerializableGraph)data;

            if (copyData.blocksOnly)
            {
                if (view != null)
                {
                    copyData.blocks = allSerializedObjects.OfType<VFXBlock>().ToArray();
                    PasteBlocks(view, copyData);
                }
            }
            else
            {
                PasteNodes(viewController, center, copyData, allSerializedObjects, view, groupNode);
            }
        }

        static readonly GUIContent m_BlockPasteError = EditorGUIUtility.TextContent("To paste blocks, please select one target block or one target context.");

        static void PasteBlocks(VFXView view, SerializableGraph copyData)
        {
            var selectedContexts = view.selection.OfType<VFXContextUI>();
            var selectedBlocks = view.selection.OfType<VFXBlockUI>();

            VFXBlockUI targetBlock = null;
            VFXContextUI targetContext = null;

            if (selectedBlocks.Count() > 0)
            {
                targetBlock = selectedBlocks.OrderByDescending(t => t.context.controller.model.GetIndex(t.controller.block)).First();
                targetContext = targetBlock.context;
            }
            else if (selectedContexts.Count() == 1)
            {
                targetContext = selectedContexts.First();
            }
            else
            {
                Debug.LogError(m_BlockPasteError.text);
                return;
            }

            VFXContext targetModelContext = targetContext.controller.model;

            int targetIndex = -1;
            if (targetBlock != null)
            {
                targetIndex = targetModelContext.GetIndex(targetBlock.controller.block) + 1;
            }

            var newBlocks = new HashSet<VFXBlock>();

            foreach (var block in copyData.blocks)
            {
                if (targetModelContext.AcceptChild(block, targetIndex))
                {
                    newBlocks.Add(block);

                    foreach (var slot in block.inputSlots)
                    {
                        slot.UnlinkAll(true, false);
                    }
                    foreach (var slot in block.outputSlots)
                    {
                        slot.UnlinkAll(true, false);
                    }
                    targetModelContext.AddChild(block, targetIndex, false); // only notify once after all blocks have been added
                }
            }

            targetModelContext.Invalidate(VFXModel.InvalidationCause.kStructureChanged);

            // Create all ui based on model
            view.controller.LightApplyChanges();

            view.ClearSelection();

            foreach (var uiBlock in targetContext.Query().OfType<VFXBlockUI>().Where(t => newBlocks.Contains(t.controller.block)).ToList())
            {
                view.AddToSelection(uiBlock);
            }
        }

        static void ClearLinks(VFXContext container)
        {
            ClearLinks(container as IVFXSlotContainer);

            foreach (var block in container.children)
            {
                ClearLinks(block);
            }
            container.UnlinkAll();
            container.SetDefaultData(false);
        }

        static void ClearLinks(IVFXSlotContainer container)
        {
            foreach (var slot in container.inputSlots)
            {
                slot.UnlinkAll(true, false);
            }
            foreach (var slot in container.outputSlots)
            {
                slot.UnlinkAll(true, false);
            }
        }

        private static void CopyDataEdges(SerializableGraph copyData, ScriptableObject[] allSerializedObjects)
        {
            if (copyData.dataEdges != null)
            {
                foreach (var dataEdge in copyData.dataEdges)
                {
                    VFXSlot inputSlot = null;
                    if (dataEdge.inputContext)
                    {
                        VFXContext targetContext = allSerializedObjects[dataEdge.input.targetIndex] as VFXContext;
                        if (dataEdge.inputBlockIndex == -1)
                        {
                            inputSlot = FetchSlot(targetContext, dataEdge.input.slotPath, true);
                        }
                        else
                        {
                            inputSlot = FetchSlot(targetContext[dataEdge.inputBlockIndex], dataEdge.input.slotPath, true);
                        }
                    }
                    else
                    {
                        VFXModel model = allSerializedObjects[dataEdge.input.targetIndex] as VFXModel;
                        inputSlot = FetchSlot(model as IVFXSlotContainer, dataEdge.input.slotPath, true);
                    }

                    IVFXSlotContainer outputContainer = null;
                    if (dataEdge.outputParameter)
                    {
                        var parameter = copyData.parameters[dataEdge.outputParameterIndex];
                        outputContainer = parameter.parameter;
                    }
                    else
                    {
                        outputContainer = allSerializedObjects[dataEdge.output.targetIndex] as IVFXSlotContainer;
                    }

                    VFXSlot outputSlot = FetchSlot(outputContainer, dataEdge.output.slotPath, false);

                    if (inputSlot != null && outputSlot != null)
                    {
                        if (inputSlot.Link(outputSlot) && dataEdge.outputParameter)
                        {
                            var parameter = copyData.parameters[dataEdge.outputParameterIndex];
                            var node = parameter.parameter.nodes[dataEdge.outputParameterNodeIndex + parameter.infoIndexOffset];
                            if (node.linkedSlots == null)
                                node.linkedSlots = new List<VFXParameter.NodeLinkedSlot>();
                            node.linkedSlots.Add(new VFXParameter.NodeLinkedSlot() { inputSlot = inputSlot, outputSlot = outputSlot });
                        }
                    }
                }
            }
        }

        static void PasteNodes(VFXViewController viewController, Vector2 center, SerializableGraph copyData, ScriptableObject[] allSerializedObjects, VFXView view, VFXGroupNodeController groupNode)
        {
            var graph = viewController.graph;
            Vector2 pasteOffset = (copyData.bounds.width > 0 && copyData.bounds.height > 0) ? center - copyData.bounds.center : Vector2.zero;

            // look if pasting there will result in the first element beeing exactly on top of other
            while (true)
            {
                bool foundSamePosition = false;
                if (copyData.contexts != null && copyData.contexts.Length > 0)
                {
                    VFXContext firstContext = copyData.contexts[0];

                    foreach (var existingContext in viewController.graph.children.OfType<VFXContext>())
                    {
                        if ((firstContext.position + pasteOffset - existingContext.position).sqrMagnitude < 1)
                        {
                            foundSamePosition = true;
                            break;
                        }
                    }
                }
                else if (copyData.slotContainers != null && copyData.slotContainers.Length > 0)
                {
                    VFXModel firstContainer = copyData.slotContainers[0];

                    foreach (var existingSlotContainer in viewController.graph.children.Where(t => t is IVFXSlotContainer))
                    {
                        if ((firstContainer.position + pasteOffset - existingSlotContainer.position).sqrMagnitude < 1)
                        {
                            foundSamePosition = true;
                            break;
                        }
                    }
                }
                else
                {
                    VFXUI ui = allSerializedObjects.OfType<VFXUI>().First();

                    if (ui != null)
                    {
                        if (ui.stickyNoteInfos != null && ui.stickyNoteInfos.Length > 0)
                        {
                            foreach (var stickyNote in viewController.stickyNotes)
                            {
                                if ((ui.stickyNoteInfos[0].position.position + pasteOffset - stickyNote.position.position).sqrMagnitude < 1)
                                {
                                    foundSamePosition = true;
                                    break;
                                }
                            }
                        }
                        else if (ui.groupInfos != null && ui.groupInfos.Length > 0)
                        {
                            foreach (var gn in viewController.groupNodes)
                            {
                                if ((ui.groupInfos[0].position.position + pasteOffset - gn.position.position).sqrMagnitude < 1)
                                {
                                    foundSamePosition = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (foundSamePosition)
                {
                    pasteOffset += Vector2.one * 30;
                }
                else
                {
                    break;
                }
            }


            if (copyData.contexts != null)
            {
                foreach (var slotContainer in copyData.contexts)
                {
                    var newContext = slotContainer;
                    newContext.position += pasteOffset;
                    ClearLinks(newContext);
                }
            }

            if (copyData.slotContainers != null)
            {
                foreach (var slotContainer in copyData.slotContainers)
                {
                    var newSlotContainer = slotContainer;
                    newSlotContainer.position += pasteOffset;
                    ClearLinks(newSlotContainer as IVFXSlotContainer);
                }
            }


            for (int i = 0; i < allSerializedObjects.Length; ++i)
            {
                ScriptableObject obj = allSerializedObjects[i];

                if (obj is VFXContext || obj is VFXOperator)
                {
                    graph.AddChild(obj as VFXModel);
                }
                else if (obj is VFXParameter)
                {
                    int paramIndex = System.Array.FindIndex(copyData.parameters, t => t.index == i);

                    VFXParameter existingParameter = graph.children.OfType<VFXParameter>().FirstOrDefault(t => t.GetInstanceID() == copyData.parameters[paramIndex].originalInstanceID);
                    if (existingParameter != null)
                    {
                        // The original parameter is from the current graph, add the nodes to the original
                        copyData.parameters[paramIndex].parameter = existingParameter;
                        copyData.parameters[paramIndex].copiedParameter = obj as VFXParameter;

                        copyData.parameters[paramIndex].infoIndexOffset = existingParameter.nodes.Count;

                        foreach (var info in copyData.parameters[paramIndex].infos)
                        {
                            info.position += pasteOffset;
                        }

                        var oldIDs = copyData.parameters[paramIndex].infos.ToDictionary(t => t, t => t.id);

                        existingParameter.AddNodeRange(copyData.parameters[paramIndex].infos);

                        //keep track of new ids for groupnodes
                        copyData.parameters[paramIndex].idMap = copyData.parameters[paramIndex].infos.ToDictionary(t => oldIDs[t], t => t.id);
                    }
                    else
                    {
                        // The original parameter is from another graph : create the parameter in the other graph, but replace the infos with only the ones copied.
                        copyData.parameters[paramIndex].parameter = obj as VFXParameter;
                        copyData.parameters[paramIndex].parameter.SetNodes(copyData.parameters[paramIndex].infos);

                        graph.AddChild(obj as VFXModel);
                    }
                }
            }


            VFXUI copiedUI = allSerializedObjects.OfType<VFXUI>().FirstOrDefault();
            int firstCopiedGroup = -1;
            int firstCopiedStickyNote = -1;
            if (copiedUI != null)
            {
                VFXUI ui = viewController.graph.UIInfos;
                firstCopiedStickyNote = ui.stickyNoteInfos != null ? ui.stickyNoteInfos.Length : 0;

                if (copiedUI.groupInfos != null && copiedUI.groupInfos.Length > 0)
                {
                    if (ui.groupInfos == null)
                    {
                        ui.groupInfos = new VFXUI.GroupInfo[0];
                    }
                    firstCopiedGroup = ui.groupInfos.Length;

                    foreach (var groupInfos in copiedUI.groupInfos)
                    {
                        for (int i = 0; i < groupInfos.contents.Length; ++i)
                        {
                            // if we link the parameter node to an existing parameter instead of the copied parameter we have to patch the groupnode content to point the that parameter with the correct id.
                            if (groupInfos.contents[i].model is VFXParameter)
                            {
                                VFXParameter parameter = groupInfos.contents[i].model as VFXParameter;
                                var paramInfo = copyData.parameters.FirstOrDefault(t => t.copiedParameter == parameter);
                                if (paramInfo.parameter != null) // parameter will not be null unless the struct returned is the default.
                                {
                                    groupInfos.contents[i].model = paramInfo.parameter;
                                    groupInfos.contents[i].id = paramInfo.idMap[groupInfos.contents[i].id];
                                }
                            }
                            else if (groupInfos.contents[i].isStickyNote)
                            {
                                groupInfos.contents[i].id += firstCopiedStickyNote;
                            }
                        }
                    }

                    ui.groupInfos = ui.groupInfos.Concat(copiedUI.groupInfos.Select(t => new VFXUI.GroupInfo(t) { position = new Rect(t.position.position + pasteOffset, t.position.size) })).ToArray();
                }
                if (copiedUI.stickyNoteInfos != null && copiedUI.stickyNoteInfos.Length > 0)
                {
                    if (ui.stickyNoteInfos == null)
                    {
                        ui.stickyNoteInfos = new VFXUI.StickyNoteInfo[0];
                    }
                    ui.stickyNoteInfos = ui.stickyNoteInfos.Concat(copiedUI.stickyNoteInfos.Select(t => new VFXUI.StickyNoteInfo(t) { position = new Rect(t.position.position + pasteOffset, t.position.size) })).ToArray();
                }
            }

            CopyDataEdges(copyData, allSerializedObjects);


            if (copyData.flowEdges != null)
            {
                foreach (var flowEdge in copyData.flowEdges)
                {
                    VFXContext inputContext = allSerializedObjects[flowEdge.input.contextIndex] as VFXContext;
                    VFXContext outputContext = allSerializedObjects[flowEdge.output.contextIndex] as VFXContext;

                    inputContext.LinkFrom(outputContext, flowEdge.input.flowIndex, flowEdge.output.flowIndex);
                }
            }

            foreach (var dataAndContexts in copyData.dataAndContexts)
            {
                VFXData data = allSerializedObjects[dataAndContexts.dataIndex] as VFXData;

                foreach (var contextIndex in dataAndContexts.contextsIndexes)
                {
                    VFXContext context = allSerializedObjects[contextIndex] as VFXContext;
                    data.CopySettings(context.GetData());
                }
            }

            // Create all ui based on model
            viewController.LightApplyChanges();

            if (view != null)
            {
                view.ClearSelection();

                var elements = view.graphElements.ToList();


                List<VFXNodeUI> newSlotContainerUIs = new List<VFXNodeUI>();
                List<VFXContextUI> newContextUIs = new List<VFXContextUI>();

                foreach (var slotContainer in allSerializedObjects.OfType<VFXContext>())
                {
                    VFXContextUI contextUI = elements.OfType<VFXContextUI>().FirstOrDefault(t => t.controller.model == slotContainer);
                    if (contextUI != null)
                    {
                        newSlotContainerUIs.Add(contextUI);
                        newSlotContainerUIs.AddRange(contextUI.GetAllBlocks().Cast<VFXNodeUI>());
                        newContextUIs.Add(contextUI);
                        view.AddToSelection(contextUI);
                    }
                }
                foreach (var slotContainer in allSerializedObjects.OfType<VFXOperator>())
                {
                    VFXOperatorUI slotContainerUI = elements.OfType<VFXOperatorUI>().FirstOrDefault(t => t.controller.model == slotContainer);
                    if (slotContainerUI != null)
                    {
                        newSlotContainerUIs.Add(slotContainerUI);
                        view.AddToSelection(slotContainerUI);
                    }
                }

                foreach (var param in copyData.parameters)
                {
                    foreach (var parameterUI in elements.OfType<VFXParameterUI>().Where(t => t.controller.model == param.parameter && param.parameter.nodes.IndexOf(t.controller.infos) >= param.infoIndexOffset))
                    {
                        newSlotContainerUIs.Add(parameterUI);
                        view.AddToSelection(parameterUI);
                    }
                }

                // Simply selected all data edge with the context or slot container, they can be no other than the copied ones
                foreach (var dataEdge in elements.OfType<VFXDataEdge>())
                {
                    if (newSlotContainerUIs.Contains(dataEdge.input.GetFirstAncestorOfType<VFXNodeUI>()))
                    {
                        view.AddToSelection(dataEdge);
                    }
                }
                // Simply selected all data edge with the context or slot container, they can be no other than the copied ones
                foreach (var flowEdge in elements.OfType<VFXFlowEdge>())
                {
                    if (newContextUIs.Contains(flowEdge.input.GetFirstAncestorOfType<VFXContextUI>()))
                    {
                        view.AddToSelection(flowEdge);
                    }
                }

                if (groupNode != null)
                {
                    foreach (var newSlotContainerUI in newSlotContainerUIs)
                    {
                        groupNode.AddNode(newSlotContainerUI.controller);
                    }
                }

                //Select all groups that are new
                if (firstCopiedGroup >= 0)
                {
                    foreach (var gn in elements.OfType<VFXGroupNode>())
                    {
                        if (gn.controller.index >= firstCopiedGroup)
                        {
                            view.AddToSelection(gn);
                        }
                    }
                }

                //Select all groups that are new
                if (firstCopiedStickyNote >= 0)
                {
                    foreach (var gn in elements.OfType<VFXStickyNote>())
                    {
                        if (gn.controller.index >= firstCopiedStickyNote)
                        {
                            view.AddToSelection(gn);
                        }
                    }
                }
            }
        }
    }
}
