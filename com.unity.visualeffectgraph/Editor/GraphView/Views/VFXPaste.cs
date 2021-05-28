using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.VFX;

using NodeID = System.UInt32;

namespace UnityEditor.VFX.UI
{
    class VFXPaste : VFXCopyPasteCommon
    {
        Vector2 pasteOffset;

        List<KeyValuePair<VFXContext, List<VFXBlock>>> newContexts = new List<KeyValuePair<VFXContext, List<VFXBlock>>>();
        List<VFXOperator> newOperators = new List<VFXOperator>();
        List<KeyValuePair<VFXParameter, List<int>>> newParameters = new List<KeyValuePair<VFXParameter, List<int>>>();

        Dictionary<NodeID, VFXNodeController> newControllers = new Dictionary<NodeID, VFXNodeController>();

        int firstCopiedGroup = -1;
        int firstCopiedStickyNote = -1;
        static VFXPaste s_Instance = null;

        public static void UnserializeAndPasteElements(VFXViewController viewController, Vector2 center, string data, VFXView view = null, VFXGroupNodeController groupNode = null)
        {
            var serializableGraph = JsonUtility.FromJson<SerializableGraph>(data);

            if (s_Instance == null)
                s_Instance = new VFXPaste();
            s_Instance.DoPaste(viewController, center, serializableGraph, view, groupNode, null);
        }

        public static void Paste(VFXViewController viewController, Vector2 center, object data, VFXView view, VFXGroupNodeController groupNode, List<VFXNodeController> nodesInTheSameOrder = null)
        {
            if (s_Instance == null)
                s_Instance = new VFXPaste();
            s_Instance.DoPaste(viewController, center, data, view, groupNode, nodesInTheSameOrder);
        }

        public static void PasteBlocks(VFXViewController viewController, object data, VFXContext targetModelContext, int targetIndex, List<VFXBlockController> blocksInTheSameOrder = null)
        {
            if (s_Instance == null)
                s_Instance = new VFXPaste();

            s_Instance.PasteBlocks(viewController, (data as SerializableGraph).operators, targetModelContext, targetIndex, blocksInTheSameOrder);
        }

        void DoPaste(VFXViewController viewController, Vector2 center, object data, VFXView view, VFXGroupNodeController groupNode, List<VFXNodeController> nodesInTheSameOrder)
        {
            SerializableGraph serializableGraph = (SerializableGraph)data;

            if (serializableGraph.blocksOnly)
            {
                if (view != null)
                {
                    PasteBlocks(view, ref serializableGraph, nodesInTheSameOrder);
                }
            }
            else
            {
                PasteAll(viewController, center, ref serializableGraph, view, groupNode, nodesInTheSameOrder);
            }
        }

        static readonly GUIContent m_BlockPasteError = EditorGUIUtility.TextContent("To paste blocks, please select one target block or one target context.");

        void PasteBlocks(VFXView view, ref SerializableGraph serializableGraph, List<VFXNodeController> nodesInTheSameOrder)
        {
            var selectedContexts = view.selection.OfType<VFXContextUI>();
            var selectedBlocks = view.selection.OfType<VFXBlockUI>();

            VFXBlockUI targetBlock = null;
            VFXContextUI targetContext = null;

            if (selectedBlocks.Count() > 0)
            {
                targetBlock = selectedBlocks.OrderByDescending(t => t.context.controller.model.GetIndex(t.controller.model)).First();
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
                targetIndex = targetModelContext.GetIndex(targetBlock.controller.model) + 1;
            }


            List<VFXBlockController> blockControllers = nodesInTheSameOrder != null ? new List<VFXBlockController>() : null;

            targetIndex = PasteBlocks(view.controller, serializableGraph.operators, targetModelContext, targetIndex, blockControllers);

            if (nodesInTheSameOrder != null)
                nodesInTheSameOrder.AddRange(blockControllers.Cast<VFXNodeController>());

            targetModelContext.Invalidate(VFXModel.InvalidationCause.kStructureChanged);

            if (view != null)
            {
                view.ClearSelection();

                foreach (var uiBlock in targetContext.Query().OfType<VFXBlockUI>().Where(t => m_NodesInTheSameOrder.Any(u => u.model == t.controller.model)).ToList())
                    view.AddToSelection(uiBlock);
            }
        }

        private int PasteBlocks(VFXViewController viewController, Node[] blocks, VFXContext targetModelContext, int targetIndex, List<VFXBlockController> blocksInTheSameOrder = null)
        {
            newControllers.Clear();
            m_NodesInTheSameOrder = new VFXNodeID[blocks.Length];
            int cpt = 0;
            foreach (var block in blocks)
            {
                Node blk = block;
                VFXBlock newBlock = PasteAndInitializeNode<VFXBlock>(viewController, ref blk);

                if (targetModelContext.AcceptChild(newBlock, targetIndex))
                {
                    m_NodesInTheSameOrder[cpt] = new VFXNodeID(newBlock, 0);
                    targetModelContext.AddChild(newBlock, targetIndex, false); // only notify once after all blocks have been added

                    targetIndex++;
                }

                ++cpt;
            }


            targetModelContext.Invalidate(VFXModel.InvalidationCause.kStructureChanged);

            var targetContextController = viewController.GetRootNodeController(targetModelContext, 0) as VFXContextController;
            targetContextController.ApplyChanges();

            if (blocksInTheSameOrder != null)
            {
                blocksInTheSameOrder.Clear();
                for (int i = 0; i < m_NodesInTheSameOrder.Length; ++i)
                {
                    blocksInTheSameOrder.Add(m_NodesInTheSameOrder[i].model != null ? targetContextController.blockControllers.First(t => t.model == m_NodesInTheSameOrder[i].model as VFXBlock) : null);
                }
            }

            return targetIndex;
        }

        VFXNodeID[] m_NodesInTheSameOrder = null;

        void PasteAll(VFXViewController viewController, Vector2 center, ref SerializableGraph serializableGraph, VFXView view, VFXGroupNodeController groupNode, List<VFXNodeController> nodesInTheSameOrder)
        {
            newControllers.Clear();

            m_NodesInTheSameOrder = new VFXNodeID[serializableGraph.controllerCount];

            var graph = viewController.graph;
            pasteOffset = (serializableGraph.bounds.width > 0 && serializableGraph.bounds.height > 0) ? center - serializableGraph.bounds.center : Vector2.zero;
            MakePasteOffsetUnique(viewController, serializableGraph);

            // Can't paste context within subgraph block/operator
            if (viewController.model.visualEffectObject is VisualEffectSubgraphOperator || viewController.model.visualEffectObject is VisualEffectSubgraphBlock)
            {
                if (serializableGraph.contexts != null)
                {
                    var count = serializableGraph.contexts.Count();
                    if (count != 0)
                        Debug.LogWarningFormat("{0} context{1} been skipped during the paste operation. Contexts aren't available in this kind of subgraph.", count, count > 1 ? "s have" : " has");
                }
            }
            else
            {
                PasteContexts(viewController, ref serializableGraph);
            }

            PasteOperators(viewController, ref serializableGraph);
            PasteParameters(viewController, ref serializableGraph);

            // Create controllers for all new nodes
            viewController.LightApplyChanges();

            // Register all nodes for usage in groupNodes and edges
            RegisterContexts(viewController);
            RegisterOperators(viewController);
            RegisterParameterNodes(viewController);

            VFXUI ui = viewController.graph.UIInfos;
            firstCopiedGroup = -1;
            firstCopiedStickyNote = ui.stickyNoteInfos != null ? ui.stickyNoteInfos.Length : 0;

            //Paste Everything else
            PasteGroupNodes(ref serializableGraph, ui);
            PasteStickyNotes(ref serializableGraph, ui);

            PasteDatas(ref serializableGraph); // TODO Data settings should be pasted at context creation. This can lead to issues as blocks are added before data is initialized
            PasteDataEdges(ref serializableGraph);
            PasteFlowEdges(ref serializableGraph);

            // Create all ui based on model
            viewController.LightApplyChanges();

            if (nodesInTheSameOrder != null)
            {
                nodesInTheSameOrder.Clear();
                nodesInTheSameOrder.AddRange(m_NodesInTheSameOrder.Select(t => t.model == null ? null : viewController.GetNodeController(t.model, t.id)));
            }

            if (view != null)
            {
                SelectCopiedElements(view, groupNode);
            }
        }

        void PasteDataEdges(ref SerializableGraph serializableGraph)
        {
            if (serializableGraph.dataEdges != null)
            {
                foreach (var dataEdge in serializableGraph.dataEdges)
                {
                    if (dataEdge.input.targetIndex == InvalidID || dataEdge.output.targetIndex == InvalidID)
                        continue;

                    VFXModel inputModel = newControllers.ContainsKey(dataEdge.input.targetIndex) ? newControllers[dataEdge.input.targetIndex].model : null;

                    VFXNodeController outputController = newControllers.ContainsKey(dataEdge.output.targetIndex) ? newControllers[dataEdge.output.targetIndex] : null;
                    VFXModel outputModel = outputController != null ? outputController.model : null;
                    if (inputModel != null && outputModel != null)
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

        void PasteSubOutputs(VFXAbstractRenderedOutput output, ref Context src)
        {
            if (src.subOutputs == null)
                return;

            var newSubOutputs = new List<VFXSRPSubOutput>(src.subOutputs.Length);
            for (int i = 0; i < src.subOutputs.Length; ++i)
            {
                Type type = (Type)src.subOutputs[i].type;
                if (type != null)
                {
                    newSubOutputs.Add((VFXSRPSubOutput)ScriptableObject.CreateInstance(type));
                    PasteModelSettings(newSubOutputs.Last(), src.subOutputs[i].settings, type);
                }
            }

            output.InitSubOutputs(newSubOutputs);
        }

        VFXContext PasteContext(VFXViewController controller, ref Context context)
        {
            VFXContext newContext = PasteAndInitializeNode<VFXContext>(controller, ref context.node);

            if (newContext == null)
            {
                newContexts.Add(new KeyValuePair<VFXContext, List<VFXBlock>>(null, null));
                return null;
            }

            newContext.label = context.label;
            VFXSystemNames.SetSystemName(newContext, context.systemName);

            if (newContext is VFXAbstractRenderedOutput)
                PasteSubOutputs((VFXAbstractRenderedOutput)newContext, ref context);

            List<VFXBlock> blocks = new List<VFXBlock>();
            foreach (var block in context.blocks)
            {
                var blk = block;

                VFXBlock newBlock = PasteAndInitializeNode<VFXBlock>(null, ref blk);

                newBlock.enabled = (blk.flags & Node.Flags.Enabled) == Node.Flags.Enabled;

                blocks.Add(newBlock);

                if (newBlock != null)
                    newContext.AddChild(newBlock);
            }
            newContexts.Add(new KeyValuePair<VFXContext, List<VFXBlock>>(newContext, blocks));

            return newContext;
        }

        T PasteAndInitializeNode<T>(VFXViewController controller, ref Node node) where T : VFXModel
        {
            Type type = node.type;
            if (type == null)
                return null;
            var newNode = ScriptableObject.CreateInstance(type) as T;
            if (newNode == null)
                return null;

            var ope = node;
            PasteNode(newNode, ref ope);

            if (!(newNode is VFXBlock))
            {
                controller.graph.AddChild(newNode);

                m_NodesInTheSameOrder[node.indexInClipboard] = new VFXNodeID(newNode, 0);
            }


            return newNode;
        }

        void PasteModelSettings(VFXModel model, Property[] settings, Type type)
        {
            var fields = GetFields(type);

            for (int i = 0; i < settings.Length; ++i)
            {
                string name = settings[i].name;
                var field = fields.Find(t => t.Name == name);
                if (field != null)
                    field.SetValue(model, settings[i].value.Get());
            }
        }

        void PasteNode(VFXModel model, ref Node node)
        {
            model.position = node.position + pasteOffset;

            PasteModelSettings(model, node.settings, model.GetType());

            model.Invalidate(VFXModel.InvalidationCause.kSettingChanged);

            var slotContainer = model as IVFXSlotContainer;
            var inputSlots = slotContainer.inputSlots;
            for (int i = 0; i < node.inputSlots.Length && i < inputSlots.Count; ++i)
            {
                if (inputSlots[i].name == node.inputSlots[i].name)
                {
                    inputSlots[i].value = node.inputSlots[i].value.Get();
                    if (inputSlots[i].spaceable)
                        inputSlots[i].space = node.inputSlots[i].space;
                }
            }

            if ((node.flags & Node.Flags.Collapsed) == Node.Flags.Collapsed)
                model.collapsed = true;

            if ((node.flags & Node.Flags.SuperCollapsed) == Node.Flags.SuperCollapsed)
                model.superCollapsed = true;

            foreach (var slot in AllSlots(slotContainer.inputSlots))
            {
                slot.collapsed = !node.expandedInputs.Contains(slot.path);
            }
            foreach (var slot in AllSlots(slotContainer.outputSlots))
            {
                slot.collapsed = !node.expandedOutputs.Contains(slot.path);
            }
        }

        HashSet<VFXNodeUI> newNodesUI = new HashSet<VFXNodeUI>();
        HashSet<VFXContextUI> newContextUIs = new HashSet<VFXContextUI>();

        private void SelectCopiedElements(VFXView view, VFXGroupNodeController groupNode)
        {
            view.ClearSelection();

            var elements = view.graphElements.ToList();

            newNodesUI.Clear();
            newContextUIs.Clear();
            FindContextUIsAndSelect(view, elements);
            FindOperatorsUIsAndSelect(view, elements);
            FindParameterUIsAndSelect(view, elements);
            SelectEdges(view, elements);

            //Select all groups that are new
            SelectGroupNodes(view, elements);

            // Add all copied element that are not in a copied groupNode to the potentially selected groupnode
            if (groupNode != null)
            {
                foreach (var newSlotContainerUI in newNodesUI)
                {
                    groupNode.AddNode(newSlotContainerUI.controller);
                }
            }

            SelectStickyNotes(view, elements);
        }

        private void SelectGroupNodes(VFXView view, List<Experimental.GraphView.GraphElement> elements)
        {
            if (firstCopiedGroup >= 0)
            {
                foreach (var gn in elements.OfType<VFXGroupNode>())
                {
                    if (gn.controller.index >= firstCopiedGroup)
                    {
                        view.AddToSelection(gn);

                        foreach (var node in gn.containedElements.OfType<VFXNodeUI>())
                        {
                            newNodesUI.Remove(node);
                        }
                    }
                }
            }
        }

        private void SelectStickyNotes(VFXView view, List<Experimental.GraphView.GraphElement> elements)
        {
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

        private void SelectEdges(VFXView view, List<Experimental.GraphView.GraphElement> elements)
        {
            // Simply selected all data edge with the context or slot container, they can be no other than the copied ones
            foreach (var dataEdge in elements.OfType<VFXDataEdge>())
            {
                if (newNodesUI.Contains(dataEdge.input.GetFirstAncestorOfType<VFXNodeUI>()))
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
        }

        private void FindParameterUIsAndSelect(VFXView view, List<Experimental.GraphView.GraphElement> elements)
        {
            foreach (var param in newControllers.Values.OfType<VFXParameterNodeController>())
            {
                foreach (var parameterUI in elements.OfType<VFXParameterUI>().Where(t => t.controller == param))
                {
                    newNodesUI.Add(parameterUI);
                    view.AddToSelection(parameterUI);
                }
            }
        }

        private void FindOperatorsUIsAndSelect(VFXView view, List<Experimental.GraphView.GraphElement> elements)
        {
            foreach (var slotContainer in newControllers.Values.OfType<VFXOperatorController>())
            {
                VFXOperatorUI slotContainerUI = elements.OfType<VFXOperatorUI>().FirstOrDefault(t => t.controller == slotContainer);
                if (slotContainerUI != null)
                {
                    newNodesUI.Add(slotContainerUI);
                    view.AddToSelection(slotContainerUI);
                }
            }
        }

        private void FindContextUIsAndSelect(VFXView view, List<Experimental.GraphView.GraphElement> elements)
        {
            foreach (var slotContainer in newContexts.Select(t => t.Key).OfType<VFXContext>())
            {
                VFXContextUI contextUI = elements.OfType<VFXContextUI>().FirstOrDefault(t => t.controller.model == slotContainer);
                if (contextUI != null)
                {
                    newNodesUI.Add(contextUI);
                    foreach (var block in contextUI.GetAllBlocks().Cast<VFXNodeUI>())
                    {
                        newNodesUI.Add(block);
                    }
                    newContextUIs.Add(contextUI);
                    view.AddToSelection(contextUI);
                }
            }
        }

        private void MakePasteOffsetUnique(VFXViewController viewController, SerializableGraph serializableGraph)
        {
            // look if pasting there will result in the first element beeing exactly on top of other
            while (true)
            {
                bool foundSamePosition = false;
                if (serializableGraph.contexts != null && serializableGraph.contexts.Length > 0)
                {
                    foreach (var existingContext in viewController.graph.children.OfType<VFXContext>())
                    {
                        if ((serializableGraph.contexts[0].node.position + pasteOffset - existingContext.position).sqrMagnitude < 1)
                        {
                            foundSamePosition = true;
                            break;
                        }
                    }
                }
                else if (serializableGraph.operators != null && serializableGraph.operators.Length > 0)
                {
                    foreach (var existingSlotContainer in viewController.graph.children.Where(t => t is IVFXSlotContainer))
                    {
                        if ((serializableGraph.operators[0].position + pasteOffset - existingSlotContainer.position).sqrMagnitude < 1)
                        {
                            foundSamePosition = true;
                            break;
                        }
                    }
                }
                else if (serializableGraph.parameters != null && serializableGraph.parameters.Length > 0 && serializableGraph.parameters[0].nodes.Length > 0)
                {
                    foreach (var existingSlotContainer in viewController.graph.children.Where(t => t is IVFXSlotContainer))
                    {
                        if ((serializableGraph.parameters[0].nodes[0].position + pasteOffset - existingSlotContainer.position).sqrMagnitude < 1)
                        {
                            foundSamePosition = true;
                            break;
                        }
                    }
                }
                else if (serializableGraph.stickyNotes != null && serializableGraph.stickyNotes.Length > 0)
                {
                    foreach (var stickyNote in viewController.stickyNotes)
                    {
                        if ((serializableGraph.stickyNotes[0].position.position + pasteOffset - stickyNote.position.position).sqrMagnitude < 1)
                        {
                            foundSamePosition = true;
                            break;
                        }
                    }
                }
                else if (serializableGraph.groupNodes != null && serializableGraph.groupNodes.Length > 0)
                {
                    foreach (var gn in viewController.groupNodes)
                    {
                        if ((serializableGraph.groupNodes[0].infos.position.position + pasteOffset - gn.position.position).sqrMagnitude < 1)
                        {
                            foundSamePosition = true;
                            break;
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
        }

        private void PasteGroupNodes(ref SerializableGraph serializableGraph, VFXUI ui)
        {
            if (serializableGraph.groupNodes != null && serializableGraph.groupNodes.Length > 0)
            {
                if (ui.groupInfos == null)
                {
                    ui.groupInfos = new VFXUI.GroupInfo[0];
                }
                firstCopiedGroup = ui.groupInfos.Length;

                List<VFXUI.GroupInfo> newGroupInfos = new List<VFXUI.GroupInfo>();
                foreach (var groupInfos in serializableGraph.groupNodes)
                {
                    var newGroupInfo = new VFXUI.GroupInfo();
                    newGroupInfo.position = new Rect(groupInfos.infos.position.position + pasteOffset, groupInfos.infos.position.size);
                    newGroupInfo.title = groupInfos.infos.title;
                    newGroupInfos.Add(newGroupInfo);
                    newGroupInfo.contents = groupInfos.contents.Take(groupInfos.contents.Length - groupInfos.stickNodeCount).Select(t => { VFXNodeController node = null; newControllers.TryGetValue(t, out node); return node; }).Where(t => t != null).Select(node => new VFXNodeID(node.model, node.id))
                        .Concat(groupInfos.contents.Skip(groupInfos.contents.Length - groupInfos.stickNodeCount).Select(t => new VFXNodeID((int)t + firstCopiedStickyNote)))
                        .ToArray();
                }
                ui.groupInfos = ui.groupInfos.Concat(newGroupInfos).ToArray();
            }
        }

        private void RegisterParameterNodes(VFXViewController viewController)
        {
            for (int i = 0; i < newParameters.Count; ++i)
            {
                viewController.GetParameterController(newParameters[i].Key).ApplyChanges();

                for (int j = 0; j < newParameters[i].Value.Count; j++)
                {
                    var nodeController = viewController.GetNodeController(newParameters[i].Key, newParameters[i].Value[j]) as VFXParameterNodeController;
                    newControllers[GetParameterNodeID((uint)i, (uint)j)] = nodeController;
                }
            }
        }

        private void RegisterOperators(VFXViewController viewController)
        {
            for (int i = 0; i < newOperators.Count; ++i)
            {
                newControllers[OperatorFlag | (uint)i] = viewController.GetNodeController(newOperators[i], 0);
            }
        }

        private void RegisterContexts(VFXViewController viewController)
        {
            for (int i = 0; i < newContexts.Count; ++i)
            {
                if (newContexts[i].Key != null)
                {
                    VFXContextController controller = viewController.GetNodeController(newContexts[i].Key, 0) as VFXContextController;
                    newControllers[ContextFlag | (uint)i] = controller;

                    for (int j = 0; j < newContexts[i].Value.Count; ++j)
                    {
                        var block = newContexts[i].Value[j];
                        if (block != null)
                        {
                            VFXBlockController blockController = controller.blockControllers.FirstOrDefault(t => t.model == block);
                            if (blockController != null)
                                newControllers[GetBlockID((uint)i, (uint)j)] = blockController;
                        }
                    }
                }
            }
        }

        private void PasteStickyNotes(ref SerializableGraph serializableGraph, VFXUI ui)
        {
            if (serializableGraph.stickyNotes != null && serializableGraph.stickyNotes.Length > 0)
            {
                if (ui.stickyNoteInfos == null)
                {
                    ui.stickyNoteInfos = new VFXUI.StickyNoteInfo[0];
                }
                ui.stickyNoteInfos = ui.stickyNoteInfos.Concat(serializableGraph.stickyNotes.Select(t => new VFXUI.StickyNoteInfo(t) { position = new Rect(t.position.position + pasteOffset, t.position.size) })).ToArray();
            }
        }

        private void PasteDatas(ref SerializableGraph serializableGraph)
        {
            for (int i = 0; i < newContexts.Count; ++i)
            {
                VFXNodeController nodeController = null;
                newControllers.TryGetValue(ContextFlag | (uint)i, out nodeController);
                var contextController = nodeController as VFXContextController;

                if (contextController != null)
                {
                    if ((contextController.flowInputAnchors.Count() == 0 ||
                         contextController.flowInputAnchors.First().connections.Count() == 0 ||
                         contextController.flowInputAnchors.First().connections.First().output.context.model.GetData() == null) &&
                        serializableGraph.contexts[i].dataIndex >= 0)
                    {
                        var data = serializableGraph.datas[serializableGraph.contexts[i].dataIndex];
                        VFXData targetData = contextController.model.GetData();
                        if (targetData != null)
                        {
                            PasteModelSettings(targetData, data.settings, targetData.GetType());
                            targetData.Invalidate(VFXModel.InvalidationCause.kSettingChanged);
                        }
                    }
                }
            }
        }

        private void PasteFlowEdges(ref SerializableGraph serializableGraph)
        {
            if (serializableGraph.flowEdges != null)
            {
                foreach (var flowEdge in serializableGraph.flowEdges)
                {
                    VFXContext inputContext = newControllers.ContainsKey(flowEdge.input.contextIndex) ? (newControllers[flowEdge.input.contextIndex] as VFXContextController).model : null;
                    VFXContext outputContext = newControllers.ContainsKey(flowEdge.output.contextIndex) ? (newControllers[flowEdge.output.contextIndex] as VFXContextController).model : null;

                    if (inputContext != null && outputContext != null)
                        inputContext.LinkFrom(outputContext, flowEdge.output.flowIndex, flowEdge.input.flowIndex);
                }
            }
        }

        private void PasteContexts(VFXViewController viewController, ref SerializableGraph serializableGraph)
        {
            if (serializableGraph.contexts != null)
            {
                newContexts.Clear();
                foreach (var context in serializableGraph.contexts)
                {
                    var ctx = context;
                    PasteContext(viewController, ref ctx);
                }
            }
        }

        private void PasteOperators(VFXViewController viewController, ref SerializableGraph serializableGraph)
        {
            newOperators.Clear();
            if (serializableGraph.operators != null)
            {
                foreach (var operat in serializableGraph.operators)
                {
                    Node ope = operat;
                    VFXOperator newOperator = PasteAndInitializeNode<VFXOperator>(viewController, ref ope);

                    newOperators.Add(newOperator); // add even they are null so that the index is correct
                }
            }
        }

        private void PasteParameters(VFXViewController viewController, ref SerializableGraph serializableGraph)
        {
            newParameters.Clear();

            if (serializableGraph.parameters != null)
            {
                foreach (var parameter in serializableGraph.parameters)
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
                            p.valueFilter = parameter.valueFilter;
                            if (parameter.valueFilter == VFXValueFilter.Range)
                            {
                                p.min = parameter.min.Get();
                                p.max = parameter.max.Get();
                            }
                            else if( parameter.valueFilter == VFXValueFilter.Enum)
                            {
                                p.enumValues = parameter.enumValue.ToList();
                            }
                            p.SetSettingValue("m_Exposed", parameter.exposed);
                            if (viewController.model.visualEffectObject is VisualEffectSubgraphOperator)
                                p.isOutput = parameter.isOutput;
                            p.SetSettingValue("m_ExposedName", parameter.name); // the controller will take care or name unicity later
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
                        int nodeIndex = p.AddNode(node.position + pasteOffset);

                        var nodeModel = p.nodes.LastOrDefault(t => t.id == nodeIndex);
                        nodeModel.expanded = !node.collapsed;
                        nodeModel.expandedSlots = AllSlots(p.outputSlots).Where(t => node.expandedOutput.Contains(t.path)).ToList();


                        m_NodesInTheSameOrder[node.indexInClipboard] = new VFXNodeID(p, nodeModel.id);
                        newParameterNodes.Add(nodeIndex);
                    }

                    newParameters.Add(new KeyValuePair<VFXParameter, List<int>>(p, newParameterNodes));
                }
            }
        }

        static VFXSlot FetchSlot(IVFXSlotContainer container, int[] slotPath, bool input)
        {
            int containerSlotIndex = slotPath[slotPath.Length - 1];

            VFXSlot slot = null;
            if (input)
            {
                if (container.GetNbInputSlots() > containerSlotIndex)
                    slot = container.GetInputSlot(slotPath[slotPath.Length - 1]);
            }
            else
            {
                if (container.GetNbOutputSlots() > containerSlotIndex)
                    slot = container.GetOutputSlot(slotPath[slotPath.Length - 1]);
            }
            if (slot == null)
                return null;

            for (int i = slotPath.Length - 2; i >= 0; --i)
            {
                if (slot.GetNbChildren() > slotPath[i])
                    slot = slot[slotPath[i]];
                else
                    return null;
            }

            return slot;
        }
    }
}
