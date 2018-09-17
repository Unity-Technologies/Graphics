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
        const NodeID TypeMask = 3u << 30;
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
                            var contexts = infos.contexts;
                            var operators = infos.operators;
                            var parameters = infos.parameters;
                            var modelIndices = infos.modelIndices;

                            var nodeIndices = groupNode.nodes.OfType<VFXNodeController>().Where(t=>contexts.Contains(t) || operators.Contains(t) || parameters.Contains(t)).Select(t => modelIndices[t ]);
                            var stickNoteIndices = info.contents.Where(t => t.isStickyNote && stickyNodeIndexToCopiedIndex.ContainsKey(t.id)).Select(t => (uint)stickyNodeIndexToCopiedIndex[t.id]);

                            data.groupNodes[i].contents = nodeIndices.Concat(stickNoteIndices).ToArray();
                            data.groupNodes[i].stickNodeCount = stickNoteIndices.Count();

                        }
                    }
                }
            }
        }

        static void CopyDataEdge(ref SerializableGraph copyData, IEnumerable<VFXDataEdgeController> dataEdges, ref CopyInfo infos )
        {
            copyData.dataEdges = new DataEdge[dataEdges.Count()];
            int cpt = 0;
            foreach (var edge in dataEdges)
            {
                DataEdge copyPasteEdge = new DataEdge();

                var inputController = edge.input as VFXDataAnchorController;
                var outputController = edge.output as VFXDataAnchorController;

                copyPasteEdge.input.slotPath = MakeSlotPath(inputController.model, true);
                copyPasteEdge.input.targetIndex = infos.modelIndices[inputController.sourceNode];

                copyPasteEdge.output.slotPath = MakeSlotPath(outputController.model, false);
                copyPasteEdge.output.targetIndex = infos.modelIndices[outputController.sourceNode];
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

                copyPasteEdge.input.contextIndex = infos.modelIndices[inputController.context];
                copyPasteEdge.input.flowIndex = inputController.slotIndex;
                copyPasteEdge.output.contextIndex = infos.modelIndices[outputController.context];
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


        static List<FieldInfo> GetFields(Type type)
        {
            List<FieldInfo> fields = null;
            if (!s_SerializableFieldByType.TryGetValue(type, out fields))
            {
                fields = new List<FieldInfo>();
                while (type != typeof(VFXContext) && type != typeof(VFXOperator) && type != typeof(VFXBlock) && type != typeof(VFXData))
                {
                    var typeFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                    foreach (var field in typeFields)
                    {
                        if (!field.IsPublic)
                        {
                            object[] attributes = field.GetCustomAttributes(true);

                            if (!attributes.Any(t => t is VFXSettingAttribute || t is SerializeField))
                                continue;
                        }

                        if (field.IsNotSerialized || !field.FieldType.IsSerializable)
                            continue;
                        /*
                        if (typeof(VFXModel).IsAssignableFrom(field.FieldType) || field.FieldType.IsGenericType && typeof(VFXModel).IsAssignableFrom(field.FieldType.GetGenericArguments()[0]))
                            continue;*/

                        fields.Add(field);
                    }
                    type = type.BaseType;
                }
                //TODO reactive to get cache
                //s_SerializableFieldByType[type] = fields;
            }

            return fields;
        }


        static Dictionary<Type, List<FieldInfo>> s_SerializableFieldByType = new Dictionary<Type, List<FieldInfo>>();

        static void CopyModelSettings(ref Property[] properties, VFXModel model)
        {
            // Copy all fields that are either VFXSettings or serialized by unity
            Type type = model.GetType();

            var fields = GetFields(type);

            properties = new Property[fields.Count()];

            for(int i = 0; i < properties.Length; ++i)
            {
                properties[i].name = fields[i].Name;
                properties[i].value = new VFXSerializableObject(fields[i].FieldType, fields[i].GetValue(model));
            }
        }


        public struct CopyInfo
        {
            public VFXContextController[] contexts;
            public VFXOperatorController[] operators;
            public VFXParameterNodeController[] parameters;
            public VFXData[] datas;
            public Dictionary<VFXNodeController, uint> modelIndices;
        }

        static void CopyNodes(SerializableGraph copyData, IEnumerable<Controller> elements, IEnumerable<VFXContextController> contexts, IEnumerable<VFXNodeController> nodes, Rect bounds)
        {
            copyData.bounds = bounds;
            IEnumerable<VFXNodeController> dataEdgeTargets = nodes.Concat(contexts.Cast<VFXNodeController>()).Concat(contexts.SelectMany(t => t.blockControllers).Cast<VFXNodeController>()).ToArray();

            // consider only edges contained in the selection

            IEnumerable<VFXDataEdgeController> dataEdges = elements.OfType<VFXDataEdgeController>().Where(t =>
                    dataEdgeTargets.Contains((t.input as VFXDataAnchorController).sourceNode as VFXNodeController) &&
                    dataEdgeTargets.Contains((t.output as VFXDataAnchorController).sourceNode as VFXNodeController)).ToArray();
            IEnumerable<VFXFlowEdgeController> flowEdges = elements.OfType<VFXFlowEdgeController>().Where(t =>
                    contexts.Contains((t.input as VFXFlowAnchorController).context) &&
                    contexts.Contains((t.output as VFXFlowAnchorController).context)
                    ).ToArray();


            CopyInfo infos = new CopyInfo();
            infos.modelIndices = new Dictionary<VFXNodeController, NodeID>();
            infos.contexts = contexts.ToArray();
            infos.operators = nodes.OfType<VFXOperatorController>().ToArray();
            infos.parameters = nodes.OfType<VFXParameterNodeController>().ToArray();

            CopyOperatorsAndContexts(ref copyData, ref infos);

            CopyParameters(ref copyData,ref infos);

            infos.datas = infos.contexts.Select(t => t.model.GetData()).Where(t => t != null).ToArray();

            CopyGroupNodesAndStickyNotes(ref copyData, elements, ref infos);

            CopyDatas(ref copyData, ref infos);

            CopyDataEdge(ref copyData, dataEdges, ref infos);

            CopyFlowEdges(ref copyData, flowEdges, ref infos);
        }

        static ParameterNode CopyParameterNode(int parameterIndex, int nodeIndex, VFXParameterNodeController controller, ref CopyInfo infos)
        {
            ParameterNode n = new ParameterNode();
            n.position = controller.position;
            n.collapsed = controller.superCollapsed;
            n.expandedOutput = controller.infos.expandedSlots.Select(t => t.path).ToArray();

            if (parameterIndex < (1 << 18) && nodeIndex < (1 << 11))
                infos.modelIndices[controller] = GetParameterNodeID((uint)parameterIndex, (uint)nodeIndex);
            else
                infos.modelIndices[controller] = InvalidID;
            return n;
        }

        static void CopyParameters(ref SerializableGraph copyData, ref CopyInfo infos)
        {
            int cpt = 0;
            CopyInfo infosCpy = infos;
            copyData.parameters = infos.parameters.GroupBy(t => t.parentController, t => t, (p, c) =>
            {
                ++cpt;

                return new Parameter()
                {
                    originalInstanceID = p.model.GetInstanceID(),
                    name = p.model.exposedName,
                    value = new VFXSerializableObject(p.model.type, p.model.value),
                    exposed = p.model.exposed,
                    range = p.hasRange,
                    min = p.hasRange ? p.model.m_Min : null,
                    max = p.hasRange ? p.model.m_Max : null,
                    tooltip = p.model.tooltip,
                    nodes = c.Select((u, i) => CopyParameterNode(cpt-1, i, u, ref infosCpy)).ToArray()
                };
            }
            ).ToArray();
        }

        static void CopyOperatorsAndContexts(ref SerializableGraph copyData,ref CopyInfo infos)
        {
            copyData.contexts = new Context[infos.contexts.Length];

            for (int i = 0; i < infos.contexts.Length; ++i)
            {
                NodeID id = CopyContext(ref copyData.contexts[i], infos.contexts[i],i, ref infos);
                infos.modelIndices[infos.contexts[i]] = id;
            }

            copyData.operators = new Node[infos.operators.Length];

            for(int i = 0; i < infos.operators.Length; ++i)
            {
                uint id = CopyNode(ref copyData.operators[i], infos.operators[i].model,(NodeID)i);
                infos.modelIndices[infos.operators[i]] = id;
            }
        }

        static NodeID CopyNode(ref Node node, VFXModel model,uint index)
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

        static NodeID CopyContext(ref Context context,VFXContextController controller,int index, ref CopyInfo infos)
        {
            NodeID id = CopyNode(ref context.node, controller.model, (NodeID)index);

            var blocks = controller.blockControllers;
            context.blocks = new Node[blocks.Count];
            for(uint i = 0; i< context.blocks.Length; ++i)
            {
                CopyNode(ref context.blocks[(int)i], blocks[(int)i].model,i);
                if( blocks[(int)i].model.enabled)
                {
                    context.blocks[i].flags |= Node.Flags.Enabled;
                }

                if (index < (1 << 18) && i < (1 << 11))
                    infos.modelIndices[blocks[(int)i]] = BlockFlag | (i << 18) | (uint)index;
                else
                    infos.modelIndices[blocks[(int)i]] = InvalidID;
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

            if (copyData.contexts.Length == 0 && copyData.operators.Length == 0)
            {
                /*copyData.contexts = null;
                copyData.slotContainers = null;
                copyData.blocks = allSerializedObjects.OfType<VFXBlock>().ToArray();*/
            }

            PasteCopy(viewController, center, copyData, view, groupNode);
        }

        public static void PasteCopy(VFXViewController viewController, Vector2 center, object data, VFXView view, VFXGroupNodeController groupNode)
        {
            SerializableGraph copyData = (SerializableGraph)data;

            if (copyData.blocksOnly)
            {
                /*if (view != null)
                {
                    copyData.blocks = allSerializedObjects.OfType<VFXBlock>().ToArray();
                    PasteBlocks(view, copyData);
                }*/
            }
            else
            {
                PasteAll(viewController, center, copyData, view, groupNode);
            }
        }

        static readonly GUIContent m_BlockPasteError = EditorGUIUtility.TextContent("To paste blocks, please select one target block or one target context.");
#if false
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

#endif


        private static void PasteDataEdges(ref SerializableGraph copyData, ref PasteInfo infos)
        {
            if (copyData.dataEdges != null)
            {
                foreach (var dataEdge in copyData.dataEdges)
                {
                    if (dataEdge.input.targetIndex == InvalidID || dataEdge.output.targetIndex == InvalidID)
                        continue;

                    //TODO: This bypasses viewController.CreateLink, and all its additional checks it shouldn't.
                    VFXModel inputModel = infos.newControllers.ContainsKey(dataEdge.input.targetIndex) ?infos.newControllers[dataEdge.input.targetIndex].model:null;


                    VFXNodeController outputController = infos.newControllers.ContainsKey(dataEdge.output.targetIndex) ? infos.newControllers[dataEdge.output.targetIndex] : null;
                    VFXModel outputModel = outputController != null ? outputController.model : null;
                    if( inputModel != null && outputModel != null)
                    {
                        VFXSlot outputSlot = FetchSlot(outputModel as IVFXSlotContainer, dataEdge.output.slotPath, false);
                        VFXSlot inputSlot = FetchSlot(inputModel as IVFXSlotContainer, dataEdge.input.slotPath, true);

                        inputSlot.Link(outputSlot);

                        if (outputController is VFXParameterNodeController)
                        {
                            var parameterNodeController = outputController as VFXParameterNodeController;

                            parameterNodeController.infos.linkedSlots.Add(new VFXParameter.NodeLinkedSlot { inputSlot = inputSlot, outputSlot = outputSlot });
                        }
                    }
                }
            }
        }

        static NodeID GetBlockID(uint contextIndex, uint blockIndex)
        {
            if (contextIndex < (1 << 18) && blockIndex < (1 << 11))
            {
                return BlockFlag | (blockIndex << 18) | contextIndex;
            }
            return InvalidID;
        }
        static NodeID GetParameterNodeID(uint parameterIndex, uint nodeIndex)
        {
            if (parameterIndex < (1 << 18) && nodeIndex < (1 << 11))
            {
                return ParameterFlag | (nodeIndex << 18) | parameterIndex;
            }
            return InvalidID;
        }


        static VFXContext PasteContext(VFXViewController controller,ref Context context,ref PasteInfo infos)
        {
            VFXContext newContext = PasteAndInitializeNode<VFXContext>(controller, ref context.node, ref infos);

            if (newContext == null)
            {
                infos.newContexts.Add(new KeyValuePair<VFXContext, List<VFXBlock>>(null, null));
                return null;
            }


            List<VFXBlock> blocks = new List<VFXBlock>();
            foreach(var block in context.blocks)
            {
                var blk = block;

                VFXBlock newBlock = PasteAndInitializeNode<VFXBlock>(null,ref blk,ref infos);

                newBlock.enabled = (blk.flags & Node.Flags.Enabled) == Node.Flags.Enabled;

                blocks.Add(newBlock);

                if ( newBlock != null)
                    newContext.AddChild(newBlock);
            }
            infos.newContexts.Add(new KeyValuePair<VFXContext, List<VFXBlock>>(newContext, blocks));

            return newContext;
        }

        static T PasteAndInitializeNode<T>(VFXViewController controller, ref Node node, ref PasteInfo infos) where T : VFXModel
        {
            Type type = node.type;
            if (type == null)
                return null;
            var newNode = ScriptableObject.CreateInstance(type) as T;
            if (newNode == null)
                return null;

            var ope = node;
            PasteNode(newNode, ref ope, ref infos);

            if( ! (newNode is VFXBlock))
                controller.graph.AddChild(newNode);

            return newNode;
        }

        static void PasteNode(VFXModel model,ref Node node,ref PasteInfo infos)
        {
            model.position = node.position + infos.pasteOffset;

            var fields = GetFields(node.type);

            for(int i = 0 ; i < node.settings.Length ; ++i)
            {
                string name = node.settings[i].name;
                var field = fields.Find(t=>t.Name == name);

                field.SetValue(model,node.settings[i].value.Get());
            }
            model.Invalidate(VFXModel.InvalidationCause.kSettingChanged);

            var slotContainer = model as IVFXSlotContainer;
            var inputSlots = slotContainer.inputSlots;
            for(int i = 0 ; i < node.inputSlots.Length ; ++i)
            {
                if( inputSlots[i].name == node.inputSlots[i].name )
                {
                    inputSlots[i].value = node.inputSlots[i].value.Get();
                }
            }

            foreach (var slot in AllSlots(slotContainer.inputSlots))
            {
                slot.collapsed = !node.expandedInputs.Contains(slot.path);
            }
            foreach (var slot in AllSlots(slotContainer.outputSlots))
            {
                slot.collapsed = !node.expandedOutputs.Contains(slot.path);
            }

        }

        struct PasteInfo
        {
            public Dictionary<NodeID, VFXNodeController> newControllers;
            public Vector2 pasteOffset;

            public List<KeyValuePair<VFXContext, List<VFXBlock>>> newContexts;
        }

        static void PasteAll(VFXViewController viewController, Vector2 center, SerializableGraph copyData, VFXView view, VFXGroupNodeController groupNode)
        {
            PasteInfo infos = new PasteInfo();
            infos.newControllers = new Dictionary<NodeID, VFXNodeController>();


            var graph = viewController.graph;
            infos.pasteOffset = (copyData.bounds.width > 0 && copyData.bounds.height > 0) ? center - copyData.bounds.center : Vector2.zero;

            // look if pasting there will result in the first element beeing exactly on top of other
            while (true)
            {
                bool foundSamePosition = false;
                if (copyData.contexts != null && copyData.contexts.Length > 0)
                {
                    var type = copyData.contexts[0].node.type;

                    foreach (var existingContext in viewController.graph.children.OfType<VFXContext>())
                    {
                        if ((copyData.contexts[0].node.position + infos.pasteOffset - existingContext.position).sqrMagnitude < 1)
                        {
                            foundSamePosition = true;
                            break;
                        }
                    }
                }
                else if (copyData.operators != null && copyData.operators.Length > 0)
                {
                    foreach (var existingSlotContainer in viewController.graph.children.Where(t => t is IVFXSlotContainer))
                    {
                        if ((copyData.operators[0].position + infos.pasteOffset - existingSlotContainer.position).sqrMagnitude < 1)
                        {
                            foundSamePosition = true;
                            break;
                        }
                    }
                }
                //TODO take stickyNote and groupNode in account if nothing else
                /*else
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
                }*/

                if (foundSamePosition)
                {
                    infos.pasteOffset += Vector2.one * 30;
                }
                else
                {
                    break;
                }
            }

            PasteContexts(viewController, ref copyData, ref infos);

            List<VFXOperator> newOperators = PasteOperators(viewController, ref copyData, ref infos);

            List<KeyValuePair<VFXParameter, List<int>>> newParameters = PasteParameters(viewController, ref copyData, ref infos);

            // Create controllers for all new nodes
            viewController.LightApplyChanges();

            for (int i = 0; i < infos.newContexts.Count; ++i)
            {
                if (infos.newContexts[i].Key != null)
                {

                    VFXContextController controller = viewController.GetNodeController(infos.newContexts[i].Key, 0) as VFXContextController;
                    infos.newControllers[ContextFlag | (uint)i] = controller;

                    for (int j = 0; j < infos.newContexts[i].Value.Count; ++j)
                    {
                        var block = infos.newContexts[i].Value[j];
                        if (block != null)
                        {
                            VFXBlockController blockController = controller.blockControllers.First(t => t.model == block);
                            if (blockController != null)
                                infos.newControllers[GetBlockID((uint)i, (uint)j)] = blockController;
                        }
                    }
                }
            }
            for (int i = 0; i < newOperators.Count; ++i)
            {
                infos.newControllers[OperatorFlag | (uint)i] = viewController.GetNodeController(newOperators[i], 0);
            }
            for (int i = 0; i < newParameters.Count; ++i)
            {
                viewController.GetParameterController(newParameters[i].Key).ApplyChanges();

                for (int j = 0; j < newParameters[i].Value.Count; j++)
                {
                    var nodeController = viewController.GetNodeController(newParameters[i].Key, newParameters[i].Value[j]) as VFXParameterNodeController;
                    infos.newControllers[GetParameterNodeID((uint)i, (uint)j)] = nodeController;
                }
            }

            int firstCopiedGroup = -1;
            int firstCopiedStickyNote = -1;
            VFXUI ui = viewController.graph.UIInfos;
            firstCopiedStickyNote = ui.stickyNoteInfos != null ? ui.stickyNoteInfos.Length : 0;

            if (copyData.groupNodes != null && copyData.groupNodes.Length > 0)
            {
                if (ui.groupInfos == null)
                {
                    ui.groupInfos = new VFXUI.GroupInfo[0];
                }
                firstCopiedGroup = ui.groupInfos.Length;

                List<VFXUI.GroupInfo> newGroupInfos = new List<VFXUI.GroupInfo>();
                foreach (var groupInfos in copyData.groupNodes)
                {
                    var newGroupInfo = new VFXUI.GroupInfo();
                    newGroupInfo.position = new Rect(groupInfos.infos.position.position + infos.pasteOffset, groupInfos.infos.position.size);
                    newGroupInfo.title = groupInfos.infos.title;
                    newGroupInfos.Add(newGroupInfo);
                    newGroupInfo.contents = groupInfos.contents.Take(groupInfos.contents.Length - groupInfos.stickNodeCount).Select(t => { VFXNodeController node = null; infos.newControllers.TryGetValue(t, out node); return node; }).Where(t => t != null).Select(node => new VFXNodeID(node.model, node.id))
                                            .Concat(groupInfos.contents.Skip(groupInfos.contents.Length - groupInfos.stickNodeCount).Select(t => new VFXNodeID((int)t + firstCopiedStickyNote)))
                                            .ToArray();

                }

                ui.groupInfos = ui.groupInfos.Concat(newGroupInfos).ToArray();

            }
            if (copyData.stickyNotes != null && copyData.stickyNotes.Length > 0)
            {
                if (ui.stickyNoteInfos == null)
                {
                    ui.stickyNoteInfos = new VFXUI.StickyNoteInfo[0];
                }
                ui.stickyNoteInfos = ui.stickyNoteInfos.Concat(copyData.stickyNotes.Select(t => new VFXUI.StickyNoteInfo(t) { position = new Rect(t.position.position + infos.pasteOffset, t.position.size) })).ToArray();
            }

            PasteDataEdges(ref copyData, ref infos);

            if (copyData.flowEdges != null)
            {
                foreach (var flowEdge in copyData.flowEdges)
                {
                    VFXContext inputContext = infos.newControllers.ContainsKey(flowEdge.input.contextIndex) ? (infos.newControllers[flowEdge.input.contextIndex] as VFXContextController).model : null;
                    VFXContext outputContext = infos.newControllers.ContainsKey(flowEdge.output.contextIndex) ? (infos.newControllers[flowEdge.output.contextIndex] as VFXContextController).model : null;

                    if (inputContext != null && outputContext != null)
                        inputContext.LinkFrom(outputContext, flowEdge.input.flowIndex, flowEdge.output.flowIndex);
                }
            }
            /*
            foreach (var dataAndContexts in copyData.dataAndContexts)
            {
                VFXData data = allSerializedObjects[dataAndContexts.dataIndex] as VFXData;

                foreach (var contextIndex in dataAndContexts.contextsIndexes)
                {
                    VFXContext context = allSerializedObjects[contextIndex] as VFXContext;
                    data.CopySettings(context.GetData());
                }
            }*/

            // Create all ui based on model
            viewController.LightApplyChanges();

            if (view != null)
            {
                view.ClearSelection();

                var elements = view.graphElements.ToList();


                List<VFXNodeUI> newSlotContainerUIs = new List<VFXNodeUI>();
                List<VFXContextUI> newContextUIs = new List<VFXContextUI>();

                foreach (var slotContainer in infos.newContexts.Select(t => t.Key).OfType<VFXContext>())
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
                foreach (var slotContainer in infos.newControllers.OfType<VFXOperatorController>())
                {
                    VFXOperatorUI slotContainerUI = elements.OfType<VFXOperatorUI>().FirstOrDefault(t => t.controller == slotContainer);
                    if (slotContainerUI != null)
                    {
                        newSlotContainerUIs.Add(slotContainerUI);
                        view.AddToSelection(slotContainerUI);
                    }
                }

                foreach (var param in infos.newControllers.OfType<VFXParameterNodeController>())
                {
                    foreach (var parameterUI in elements.OfType<VFXParameterUI>().Where(t => t.controller == param))
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

        private static PasteInfo PasteContexts(VFXViewController viewController, ref SerializableGraph copyData, ref PasteInfo infos)
        {
            if (copyData.contexts != null)
            {
                infos.newContexts = new List<KeyValuePair<VFXContext, List<VFXBlock>>>();
                foreach (var context in copyData.contexts)
                {
                    var ctx = context;
                    PasteContext(viewController, ref ctx, ref infos);
                }
            }

            return infos;
        }

        private static List<VFXOperator> PasteOperators(VFXViewController viewController, ref SerializableGraph copyData, ref PasteInfo infos)
        {
            List<VFXOperator> newOperators = new List<VFXOperator>();
            if (copyData.operators != null)
            {
                foreach (var operat in copyData.operators)
                {
                    Node ope = operat;
                    VFXOperator newOperator = PasteAndInitializeNode<VFXOperator>(viewController, ref ope, ref infos);

                    newOperators.Add(newOperator); // add even they are null so that the index is correct
                }
            }

            return newOperators;
        }

        private static List<KeyValuePair<VFXParameter, List<int>>> PasteParameters(VFXViewController viewController, ref SerializableGraph copyData, ref PasteInfo infos)
        {
            List<KeyValuePair<VFXParameter, List<int>>> newParameters = new List<KeyValuePair<VFXParameter, List<int>>>();

            if (copyData.parameters != null)
            {
                foreach (var parameter in copyData.parameters)
                {
                    // if we have a parameter with the same name use it else create it with the copied data
                    VFXParameter p = viewController.graph.children.OfType<VFXParameter>().FirstOrDefault(t => t.GetInstanceID() == parameter.originalInstanceID);

                    if (p == null)
                    {
                        Type type = parameter.value.type;
                        VFXModelDescriptorParameters desc = VFXLibrary.GetParameters().FirstOrDefault(t => t.model.type == type);
                        if (desc != null)
                        {
                            p = viewController.AddVFXParameter(Vector2.zero, desc);
                            p.value = parameter.value.Get();
                            p.hasRange = parameter.range;
                            if (parameter.range)
                            {
                                p.m_Min = parameter.min;
                                p.m_Max = parameter.max;
                            }
                            p.SetSettingValue("m_exposedName", parameter.name); // the controller will take care or name unicity later
                            p.tooltip = parameter.tooltip;
                        }

                    }

                    if (p == null)
                    {
                        newParameters.Add(new KeyValuePair<VFXParameter, List<int>>(null, null));
                        continue;
                    }


                    var newParameterNodes = new List<int>();
                    foreach (var node in parameter.nodes)
                    {
                        int nodeIndex = p.AddNode(node.position + infos.pasteOffset);

                        var nodeModel = p.nodes.LastOrDefault(t => t.id == nodeIndex);
                        nodeModel.expanded = !node.collapsed;
                        nodeModel.expandedSlots = AllSlots(p.outputSlots).Where(t => node.expandedOutput.Contains(t.path)).ToList();

                        newParameterNodes.Add(nodeIndex);
                    }

                    newParameters.Add(new KeyValuePair<VFXParameter, List<int>>(p, newParameterNodes));
                }
            }

            return newParameters;
        }
    }
}
