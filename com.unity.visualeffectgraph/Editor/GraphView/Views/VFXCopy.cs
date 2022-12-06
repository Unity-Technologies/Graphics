using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.VFX;
using UnityEngine.UIElements;
using System.Reflection;

using NodeID = System.UInt32;

namespace UnityEditor.VFX.UI
{
    class VFXCopy : VFXCopyPasteCommon
    {
        VFXContextController[] contexts;
        int[] contextsIndices;
        VFXOperatorController[] operators;
        int[] operatorIndices;
        VFXParameterNodeController[] parameters;
        int[] parameterIndices;
        VFXData[] datas;
        Dictionary<VFXNodeController, uint> modelIndices = new Dictionary<VFXNodeController, NodeID>();

        static VFXCopy s_Instance;

        public static object Copy(IEnumerable<Controller> elements, Rect bounds)
        {
            if (s_Instance == null)
                s_Instance = new VFXCopy();
            return s_Instance.CreateCopy(elements, bounds);
        }

        public static string SerializeElements(IEnumerable<Controller> elements, Rect bounds)
        {
            if (s_Instance == null)
                s_Instance = new VFXCopy();
            var serializableGraph = s_Instance.CreateCopy(elements, bounds) as SerializableGraph;

            return JsonUtility.ToJson(serializableGraph);
        }

        public static object CopyBlocks(IEnumerable<VFXBlockController> blocks, IEnumerable<Controller> elements = null)
        {
            if (s_Instance == null)
                s_Instance = new VFXCopy();
            var serializableGraph = s_Instance.DoCopyBlocks(blocks);
            if (elements != null)
            {
                s_Instance.CopyGroupNodesAndStickyNotes(ref serializableGraph, elements);
            }

            return serializableGraph;
        }

        object CreateCopy(IEnumerable<Controller> elements, Rect bounds)
        {
            IEnumerable<VFXContextController> contexts = elements.OfType<VFXContextController>();
            IEnumerable<VFXNodeController> nodes = elements.Where(t => t is VFXOperatorController || t is VFXParameterNodeController).Cast<VFXNodeController>();
            IEnumerable<VFXBlockController> blocks = elements.OfType<VFXBlockController>();

            SerializableGraph serializableGraph = new SerializableGraph();

            serializableGraph.controllerCount = elements.Count();

            if (contexts.Count() == 0 && nodes.Count() == 0 && blocks.Count() > 0)
            {
                var copiedBlocks = new List<VFXBlockController>(blocks);
                modelIndices.Clear();
                serializableGraph.operators = CopyBlocks(copiedBlocks, 0);
                serializableGraph.blocksOnly = true;
            }
            else
            {
                //Don't copy VFXBlockSubgraphContext because they can't be pasted anywhere.
                CopyNodes(serializableGraph, elements, contexts.Where(t => !(t.model is VFXBlockSubgraphContext)), nodes, bounds);
            }

            return serializableGraph;
        }

        void CopyNodes(SerializableGraph serializableGraph, IEnumerable<Controller> elements, IEnumerable<VFXContextController> copiedContexts, IEnumerable<VFXNodeController> nodes, Rect bounds)
        {
            Controller[] copiedElements = elements.ToArray();
            serializableGraph.bounds = bounds;
            IEnumerable<VFXNodeController> dataEdgeTargets = nodes.Concat(copiedContexts.Cast<VFXNodeController>()).Concat(copiedContexts.SelectMany(t => t.blockControllers).Cast<VFXNodeController>()).ToArray();

            // consider only edges contained in the selection

            IEnumerable<VFXDataEdgeController> dataEdges = elements.OfType<VFXDataEdgeController>().Where(t =>
                dataEdgeTargets.Contains((t.input as VFXDataAnchorController).sourceNode as VFXNodeController) &&
                dataEdgeTargets.Contains((t.output as VFXDataAnchorController).sourceNode as VFXNodeController)).ToArray();
            IEnumerable<VFXFlowEdgeController> flowEdges = elements.OfType<VFXFlowEdgeController>().Where(t =>
                copiedContexts.Contains((t.input as VFXFlowAnchorController).context) &&
                copiedContexts.Contains((t.output as VFXFlowAnchorController).context)
                ).ToArray();


            modelIndices.Clear();
            contexts = copiedContexts.ToArray();
            operators = nodes.OfType<VFXOperatorController>().ToArray();
            parameters = nodes.OfType<VFXParameterNodeController>().ToArray();

            contextsIndices = contexts.Select(t => Array.IndexOf(copiedElements, t)).ToArray();
            operatorIndices = operators.Select(t => Array.IndexOf(copiedElements, t)).ToArray();
            parameterIndices = parameters.Select(t => Array.IndexOf(copiedElements, t)).ToArray();

            datas = contexts.Select(t => t.model.GetData()).Where(t => t != null).ToArray();

            CopyOperatorsAndContexts(ref serializableGraph);

            CopyParameters(ref serializableGraph);

            CopyGroupNodesAndStickyNotes(ref serializableGraph, elements);

            CopyDatas(ref serializableGraph);

            CopyDataEdge(ref serializableGraph, dataEdges);

            CopyFlowEdges(ref serializableGraph, flowEdges);
        }

        void CopyGroupNodesAndStickyNotes(ref SerializableGraph serializableGraph, IEnumerable<Controller> elements)
        {
            VFXGroupNodeController[] groupNodes = elements.OfType<VFXGroupNodeController>().ToArray();
            VFXStickyNoteController[] stickyNotes = elements.OfType<VFXStickyNoteController>().ToArray();

            if (groupNodes.Length > 0 || stickyNotes.Length > 0)
            {
                var stickyNodeIndexToCopiedIndex = new Dictionary<int, int>();

                if (stickyNotes.Length > 0)
                {
                    serializableGraph.stickyNotes = new VFXUI.StickyNoteInfo[stickyNotes.Length];

                    for (int i = 0; i < stickyNotes.Length; ++i)
                    {
                        VFXStickyNoteController stickyNote = stickyNotes[i];
                        stickyNodeIndexToCopiedIndex[stickyNote.index] = i;
                        VFXUI.StickyNoteInfo info = stickyNote.model.stickyNoteInfos[stickyNote.index];
                        serializableGraph.stickyNotes[i] = new VFXUI.StickyNoteInfo(info);
                    }
                }

                if (groupNodes.Length > 0)
                {
                    serializableGraph.groupNodes = new GroupNode[groupNodes.Length];
                    for (int i = 0; i < groupNodes.Length; ++i)
                    {
                        VFXGroupNodeController groupNode = groupNodes[i];
                        VFXUI.GroupInfo info = groupNode.model.groupInfos[groupNode.index];

                        serializableGraph.groupNodes[i] = new GroupNode { infos = new VFXUI.UIInfo(info) };

                        // only keep nodes and sticky notes that are copied because a element can not be in two groups at the same time.
                        if (info.contents != null)
                        {
                            var nodeIndices = groupNode.nodes.OfType<VFXNodeController>().Where(t => contexts.Contains(t) || operators.Contains(t) || parameters.Contains(t)).Select(t => modelIndices[t]);
                            var stickNoteIndices = info.contents.Where(t => t.isStickyNote && stickyNodeIndexToCopiedIndex.ContainsKey(t.id)).Select(t => (uint)stickyNodeIndexToCopiedIndex[t.id]);

                            serializableGraph.groupNodes[i].contents = nodeIndices.Concat(stickNoteIndices).ToArray();
                            serializableGraph.groupNodes[i].stickNodeCount = stickNoteIndices.Count();
                        }
                    }
                }
            }
        }

        void CopyDataEdge(ref SerializableGraph serializableGraph, IEnumerable<VFXDataEdgeController> dataEdges)
        {
            serializableGraph.dataEdges = new DataEdge[dataEdges.Count()];
            int cpt = 0;
            foreach (var edge in dataEdges)
            {
                DataEdge copyPasteEdge = new DataEdge();

                var inputController = edge.input as VFXDataAnchorController;
                var outputController = edge.output as VFXDataAnchorController;

                copyPasteEdge.input.slotPath = MakeSlotPath(inputController.model, true);
                copyPasteEdge.input.targetIndex = modelIndices[inputController.sourceNode];

                copyPasteEdge.output.slotPath = MakeSlotPath(outputController.model, false);
                copyPasteEdge.output.targetIndex = modelIndices[outputController.sourceNode];
                serializableGraph.dataEdges[cpt++] = copyPasteEdge;
            }
        }

        void CopyFlowEdges(ref SerializableGraph serializableGraph, IEnumerable<VFXFlowEdgeController> flowEdges)
        {
            serializableGraph.flowEdges = new FlowEdge[flowEdges.Count()];
            int cpt = 0;
            foreach (var edge in flowEdges)
            {
                FlowEdge copyPasteEdge = new FlowEdge();

                var inputController = edge.input as VFXFlowAnchorController;
                var outputController = edge.output as VFXFlowAnchorController;

                copyPasteEdge.input.contextIndex = modelIndices[inputController.context];
                copyPasteEdge.input.flowIndex = inputController.slotIndex;
                copyPasteEdge.output.contextIndex = modelIndices[outputController.context];
                copyPasteEdge.output.flowIndex = outputController.slotIndex;

                serializableGraph.flowEdges[cpt++] = copyPasteEdge;
            }
        }

        void CopyDatas(ref SerializableGraph serializableGraph)
        {
            serializableGraph.datas = new Data[datas.Length];
            for (int i = 0; i < datas.Length; ++i)
            {
                CopyModelSettings(ref serializableGraph.datas[i].settings, datas[i]);
            }
        }

        void CopyModelSettings(ref Property[] properties, VFXModel model)
        {
            // Copy all fields that are either VFXSettings or serialized by unity
            Type type = model.GetType();

            var fields = GetFields(type);

            properties = new Property[fields.Count()];

            for (int i = 0; i < properties.Length; ++i)
            {
                properties[i].name = fields[i].Name;
                properties[i].value = new VFXSerializableObject(fields[i].FieldType, fields[i].GetValue(model));
            }
        }

        ParameterNode CopyParameterNode(int parameterIndex, int nodeIndex, VFXParameterNodeController controller, int indexInClipboard)
        {
            ParameterNode n = new ParameterNode();
            n.position = controller.position;
            n.collapsed = controller.superCollapsed;
            n.expandedOutput = controller.infos.expandedSlots.Select(t => t.path).ToArray();
            n.indexInClipboard = indexInClipboard;

            if (parameterIndex < (1 << 18) && nodeIndex < (1 << 11))
                modelIndices[controller] = GetParameterNodeID((uint)parameterIndex, (uint)nodeIndex);
            else
                modelIndices[controller] = InvalidID;
            return n;
        }

        void CopyParameters(ref SerializableGraph serializableGraph)
        {
            int cpt = 0;
            serializableGraph.parameters = parameters.GroupBy(t => t.parentController, t => t, (p, c) =>
            {
                ++cpt;

                return new Parameter()
                {
                    originalInstanceID = p.model.GetInstanceID(),
                    name = p.model.exposedName,
                    value = new VFXSerializableObject(p.model.type, p.model.value),
                    exposed = p.model.exposed,
                    isOutput = p.model.isOutput,
                    valueFilter = p.valueFilter,
                    min = p.valueFilter == VFXValueFilter.Range ? new VFXSerializableObject(p.model.type, p.model.min) : null,
                    max = p.valueFilter == VFXValueFilter.Range ? new VFXSerializableObject(p.model.type, p.model.max) : null,
                    enumValue = p.valueFilter == VFXValueFilter.Enum ? p.model.enumValues.ToArray() : null,
                    tooltip = p.model.tooltip,
                    space = p.space,
                    nodes = c.Select((u, i) => CopyParameterNode(cpt - 1, i, u, parameterIndices[Array.IndexOf(parameters, u)])).ToArray()
                };
            }
                ).ToArray();
        }

        void CopyOperatorsAndContexts(ref SerializableGraph serializableGraph)
        {
            serializableGraph.contexts = new Context[contexts.Length];

            for (int i = 0; i < contexts.Length; ++i)
            {
                NodeID id = CopyContext(ref serializableGraph.contexts[i], contexts[i], i);
                modelIndices[contexts[i]] = id;
            }

            serializableGraph.operators = new Node[operators.Length];

            for (int i = 0; i < operators.Length; ++i)
            {
                uint id = CopyNode(ref serializableGraph.operators[i], operators[i].model, (NodeID)i);
                serializableGraph.operators[i].indexInClipboard = operatorIndices[i];
                modelIndices[operators[i]] = id;
            }
        }

        NodeID CopyNode(ref Node node, VFXModel model, uint index)
        {
            // Copy node infos
            node.position = model.position;
            node.type = model.GetType();
            node.flags = (model as VFXBlock)?.enabled != false ? Node.Flags.Enabled : 0;
            if (model.collapsed)
                node.flags |= Node.Flags.Collapsed;
            if (model.superCollapsed)
                node.flags |= Node.Flags.SuperCollapsed;

            uint id = 0;
            if (model is VFXOperator)
            {
                id = OperatorFlag;
            }
            else if (model is VFXContext)
            {
                id = ContextFlag;
            }

            id |= (uint)index;

            //Copy settings value
            CopyModelSettings(ref node.settings, model);

            var activationSlot = (model as IVFXSlotContainer).activationSlot;
            node.activationSlotValue = activationSlot ? (bool)activationSlot.value : false;

            var inputSlots = (model as IVFXSlotContainer).inputSlots;
            node.inputSlots = new Property[inputSlots.Count];
            for (int i = 0; i < inputSlots.Count; i++)
            {
                node.inputSlots[i].name = inputSlots[i].name;
                if (inputSlots[i].spaceable)
                    node.inputSlots[i].space = inputSlots[i].space;
                node.inputSlots[i].value = new VFXSerializableObject(inputSlots[i].property.type, inputSlots[i].value);
            }

            node.expandedInputs = AllSlots(inputSlots).Where(t => !t.collapsed).Select(t => t.path).ToArray();
            node.expandedOutputs = AllSlots((model as IVFXSlotContainer).outputSlots).Where(t => !t.collapsed).Select(t => t.path).ToArray();

            return id;
        }

        SerializableGraph DoCopyBlocks(IEnumerable<VFXBlockController> blocks)
        {
            var newBlocks = new Node[blocks.Count()];
            uint cpt = 0;
            foreach (var block in blocks)
            {
                CopyNode(ref newBlocks[(int)cpt], block.model, cpt);
                ++cpt;
            }

            var graph = new SerializableGraph();
            graph.blocksOnly = true;
            graph.operators = newBlocks;

            return graph;
        }

        Node[] CopyBlocks(IList<VFXBlockController> blocks, int contextIndex)
        {
            var newBlocks = new Node[blocks.Count];
            for (uint i = 0; i < newBlocks.Length; ++i)
            {
                CopyNode(ref newBlocks[(int)i], blocks[(int)i].model, i);
                if (blocks[(int)i].model.enabled)
                {
                    newBlocks[i].flags |= Node.Flags.Enabled;
                }

                if (contextIndex < (1 << 18) && i < (1 << 11))
                    modelIndices[blocks[(int)i]] = BlockFlag | (i << 18) | (uint)contextIndex;
                else
                    modelIndices[blocks[(int)i]] = InvalidID;
            }

            return newBlocks;
        }

        NodeID CopyContext(ref Context context, VFXContextController controller, int index)
        {
            NodeID id = CopyNode(ref context.node, controller.model, (NodeID)index);

            var blocks = controller.blockControllers;

            context.label = controller.model.label;
            context.systemName = VFXSystemNames.GetSystemName(controller.model);

            if (controller.model.GetData() != null)
                context.dataIndex = Array.IndexOf(datas, controller.model.GetData());
            else
                context.dataIndex = -1;
            context.blocks = CopyBlocks(controller.blockControllers, index);
            context.node.indexInClipboard = contextsIndices[index];

            if (controller.model is VFXAbstractRenderedOutput)
                context.subOutputs = CopySubOutputs(((VFXAbstractRenderedOutput)controller.model).GetSubOutputs());
            else
                context.subOutputs = null;

            return id;
        }

        SubOutput[] CopySubOutputs(List<VFXSRPSubOutput> subOutputs)
        {
            var newSubOutputs = new SubOutput[subOutputs.Count];
            for (int i = 0; i < newSubOutputs.Length; ++i)
            {
                if (subOutputs[i] != null) // Can be null if associated SRP is unknown
                {
                    newSubOutputs[i].type = subOutputs[i].GetType();
                    CopyModelSettings(ref newSubOutputs[i].settings, subOutputs[i]);
                }
            }
            return newSubOutputs;
        }

        static int[] MakeSlotPath(VFXSlot slot, bool input)
        {
            List<int> slotPath = new List<int>(slot.depth + 1);
            while (slot.GetParent() != null)
            {
                slotPath.Add(slot.GetParent().GetIndex(slot));
                slot = slot.GetParent();
            }

            int indexInOwner = -1;
            if (ReferenceEquals(slot,slot.owner.activationSlot))
                indexInOwner = -2; // activation slot
            else
                indexInOwner = (input ? slot.owner.inputSlots : slot.owner.outputSlots).IndexOf(slot);
            slotPath.Add(indexInOwner);

            return slotPath.ToArray();
        }
    }
}
