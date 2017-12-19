using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEditor.VFX.UI
{
    class VFXCopyPaste
    {
        [System.Serializable]
        struct DataAnchor
        {
            public int targetIndex;
            public int[] slotPath;
        }

        [System.Serializable]
        struct DataEdge
        {
            public bool inputContext;
            public int inputBlockIndex;
            public DataAnchor input;
            public DataAnchor output;
        }

        [System.Serializable]
        struct FlowAnchor
        {
            public int contextIndex;
            public int flowIndex;
        }


        [System.Serializable]
        struct FlowEdge
        {
            public FlowAnchor input;
            public FlowAnchor output;
        }

        [System.Serializable]
        struct DataAndContexts
        {
            public int dataIndex;
            public int[] contextsIndexes;
        }

        [System.Serializable]
        class Data
        {
            public string serializedObjects;


            public bool blocksOnly;

            [NonSerialized]
            public VFXContext[] contexts;


            [NonSerialized]
            public VFXModel[] slotContainers;
            [NonSerialized]
            public VFXBlock[] blocks;

            public DataAndContexts[] dataAndContexts;
            public DataEdge[] dataEdges;
            public FlowEdge[] flowEdges;


            public void CollectDependencies(HashSet<ScriptableObject> objects)
            {
                if (contexts != null)
                {
                    foreach (var context in contexts)
                    {
                        objects.Add(context);
                        context.CollectDependencies(objects);
                    }
                }
                if (slotContainers != null)
                {
                    foreach (var slotContainer in slotContainers)
                    {
                        objects.Add(slotContainer);
                        slotContainer.CollectDependencies(objects);
                    }
                }
                if (blocks != null)
                {
                    foreach (var block in blocks)
                    {
                        objects.Add(block);
                        block.CollectDependencies(objects);
                    }
                }
            }
        }

        static ScriptableObject[] PrepareSerializedObjects(Data copyData)
        {
            var objects = new HashSet<ScriptableObject>();
            copyData.CollectDependencies(objects);

            ScriptableObject[] allSerializedObjects = objects.OfType<ScriptableObject>().ToArray();

            copyData.serializedObjects = VFXMemorySerializer.StoreObjects(allSerializedObjects);

            return allSerializedObjects;
        }

        static void CopyNodes(Data copyData, IEnumerable<Controller> elements, IEnumerable<VFXContextController> contexts, IEnumerable<VFXSlotContainerController> slotContainers)
        {
            IEnumerable<VFXSlotContainerController> dataEdgeTargets = slotContainers.Concat(contexts.Select(t => t.slotContainerController as VFXSlotContainerController)).Concat(contexts.SelectMany(t => t.blockControllers).Cast<VFXSlotContainerController>()).ToArray();

            // consider only edges contained in the selection

            IEnumerable<VFXDataEdgeController> dataEdges = elements.OfType<VFXDataEdgeController>().Where(t => dataEdgeTargets.Contains((t.input as VFXDataAnchorController).sourceNode as VFXSlotContainerController) && dataEdgeTargets.Contains((t.output as VFXDataAnchorController).sourceNode as VFXSlotContainerController)).ToArray();
            IEnumerable<VFXFlowEdgeController> flowEdges = elements.OfType<VFXFlowEdgeController>().Where(t =>
                    contexts.Contains((t.input as VFXFlowAnchorController).context) &&
                    contexts.Contains((t.output as VFXFlowAnchorController).context)
                    ).ToArray();


            VFXContext[] copiedContexts = contexts.Select(t => t.context).ToArray();
            copyData.contexts = copiedContexts;
            VFXModel[] copiedSlotContainers = slotContainers.Select(t => t.model).ToArray();
            copyData.slotContainers = copiedSlotContainers;


            VFXData[] datas = copiedContexts.Select(t => t.GetData()).Where(t => t != null).ToArray();


            ScriptableObject[] allSerializedObjects = PrepareSerializedObjects(copyData);


            copyData.dataAndContexts = new DataAndContexts[datas.Length];

            for (int i = 0; i < datas.Length; ++i)
            {
                copyData.dataAndContexts[i].dataIndex = System.Array.IndexOf(allSerializedObjects, datas[i]);
                copyData.dataAndContexts[i].contextsIndexes = copiedContexts.Where(t => t.GetData() == datas[i]).Select(t => System.Array.IndexOf(allSerializedObjects, t)).ToArray();
            }


            copyData.dataEdges = new DataEdge[dataEdges.Count()];
            int cpt = 0;
            foreach (var edge in dataEdges)
            {
                DataEdge copyPasteEdge = new DataEdge();

                var inputController = edge.input as VFXDataAnchorController;
                var outputController = edge.output as VFXDataAnchorController;

                copyPasteEdge.input.slotPath = MakeSlotPath(inputController.model, true);

                if (inputController.model.owner is VFXContext)
                {
                    VFXContext context = inputController.model.owner as VFXContext;
                    copyPasteEdge.inputContext = true;
                    copyPasteEdge.input.targetIndex = System.Array.IndexOf(allSerializedObjects, context);
                    copyPasteEdge.inputBlockIndex = -1;
                }
                else if (inputController.model.owner is VFXBlock)
                {
                    VFXBlock block = inputController.model.owner as VFXBlock;
                    copyPasteEdge.inputContext = true;
                    copyPasteEdge.input.targetIndex = System.Array.IndexOf(allSerializedObjects, block.GetParent());
                    copyPasteEdge.inputBlockIndex = block.GetParent().GetIndex(block);
                }
                else
                {
                    copyPasteEdge.inputContext = false;
                    copyPasteEdge.input.targetIndex = System.Array.IndexOf(allSerializedObjects, inputController.model.owner as VFXModel);
                    copyPasteEdge.inputBlockIndex = -1;
                }


                copyPasteEdge.output.slotPath = MakeSlotPath(outputController.model, false);
                copyPasteEdge.output.targetIndex = System.Array.IndexOf(allSerializedObjects, outputController.model.owner as VFXModel);

                copyData.dataEdges[cpt++] = copyPasteEdge;
            }


            copyData.flowEdges = new FlowEdge[flowEdges.Count()];
            cpt = 0;
            foreach (var edge in flowEdges)
            {
                FlowEdge copyPasteEdge = new FlowEdge();

                var inputController = edge.input as VFXFlowAnchorController;
                var outputController = edge.output as VFXFlowAnchorController;

                copyPasteEdge.input.contextIndex = System.Array.IndexOf(allSerializedObjects, inputController.owner);
                copyPasteEdge.input.flowIndex = inputController.slotIndex;
                copyPasteEdge.output.contextIndex = System.Array.IndexOf(allSerializedObjects, outputController.owner);
                copyPasteEdge.output.flowIndex = outputController.slotIndex;

                copyData.flowEdges[cpt++] = copyPasteEdge;
            }
        }

        public static object CreateCopy(IEnumerable<Controller> elements)
        {
            IEnumerable<VFXContextController> contexts = elements.OfType<VFXContextController>();
            IEnumerable<VFXSlotContainerController> slotContainers = elements.Where(t => t is VFXOperatorController || t is VFXParameterController).Cast<VFXSlotContainerController>();
            IEnumerable<VFXBlockController> blocks = elements.OfType<VFXBlockController>();

            Data copyData = new Data();

            if (contexts.Count() == 0 && slotContainers.Count() == 0 && blocks.Count() > 0)
            {
                VFXBlock[] copiedBlocks = blocks.Select(t => t.block).ToArray();
                copyData.blocks = copiedBlocks;
                PrepareSerializedObjects(copyData);
                copyData.blocksOnly = true;
            }
            else
            {
                CopyNodes(copyData, elements, contexts, slotContainers);
            }

            return copyData;
        }

        public static string SerializeElements(IEnumerable<Controller> elements)
        {
            var copyData = CreateCopy(elements) as Data;

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

        public static void UnserializeAndPasteElements(VFXView view, Vector2 pasteOffset, string data)
        {
            var copyData = JsonUtility.FromJson<Data>(data);

            ScriptableObject[] allSerializedObjects = VFXMemorySerializer.ExtractObjects(copyData.serializedObjects, true);

            copyData.contexts = allSerializedObjects.OfType<VFXContext>().ToArray();
            copyData.slotContainers = allSerializedObjects.OfType<IVFXSlotContainer>().Cast<VFXModel>().Where(t => !(t is VFXContext)).ToArray();
            if (copyData.contexts.Length == 0 && copyData.slotContainers.Length == 0)
            {
                copyData.contexts = null;
                copyData.slotContainers = null;
                copyData.blocks = allSerializedObjects.OfType<VFXBlock>().ToArray();
            }

            PasteCopy(view, pasteOffset, copyData, allSerializedObjects);
        }

        public static void PasteCopy(VFXView view, Vector2 pasteOffset, object data, ScriptableObject[] allSerializedObjects)
        {
            Data copyData = (Data)data;

            if (copyData.blocksOnly)
            {
                copyData.blocks = allSerializedObjects.OfType<VFXBlock>().ToArray();
                PasteBlocks(view, copyData);
            }
            else
            {
                PasteNodes(view, pasteOffset, copyData, allSerializedObjects);
            }
        }

        static readonly GUIContent m_BlockPasteError = EditorGUIUtility.TextContent("To paste blocks, please select one target block or one target context.");

        static void PasteBlocks(VFXView view, Data copyData)
        {
            var selectedContexts = view.selection.OfType<VFXContextUI>();
            var selectedBlocks = view.selection.OfType<VFXBlockUI>();

            VFXBlockUI targetBlock = null;
            VFXContextUI targetContext = null;

            if (selectedBlocks.Count() > 0)
            {
                targetBlock = selectedBlocks.OrderByDescending(t => t.context.controller.context.GetIndex(t.controller.block)).First();
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

            VFXContext targetModelContext = targetContext.controller.context;

            int targetIndex = -1;
            if (targetBlock != null)
            {
                targetIndex = targetModelContext.GetIndex(targetBlock.controller.block) + 1;
            }

            var newBlocks = new HashSet<VFXBlock>();

            foreach (var block in copyData.blocks)
            {
                newBlocks.Add(block);

                if (targetModelContext.AcceptChild(block, targetIndex))
                {
                    targetModelContext.AddChild(block, targetIndex, false); // only notify once after all blocks have been added
                }
            }

            targetModelContext.Invalidate(VFXModel.InvalidationCause.kStructureChanged);

            // Create all ui based on model
            view.controller.ApplyChanges();

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
        }

        static void PasteNodes(VFXView view, Vector2 pasteOffset, Data copyData, ScriptableObject[] allSerializedObjects)
        {
            var graph = view.controller.graph;

            foreach (var slotContainer in copyData.contexts)
            {
                var newContext = slotContainer;
                newContext.position += pasteOffset;
                ClearLinks(newContext);
            }

            foreach (var slotContainer in copyData.slotContainers)
            {
                var newSlotContainer = slotContainer;
                newSlotContainer.position += pasteOffset;
            }

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

                    VFXSlot outputSlot = FetchSlot(allSerializedObjects[dataEdge.output.targetIndex] as IVFXSlotContainer, dataEdge.output.slotPath, false);

                    if (inputSlot != null && outputSlot != null)
                        inputSlot.Link(outputSlot);
                }
            }

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

            foreach (var m in allSerializedObjects.OfType<VFXContext>())
                graph.AddChild(m);
            foreach (var m in allSerializedObjects.OfType<VFXOperator>())
                graph.AddChild(m);
            foreach (var m in allSerializedObjects.OfType<VFXParameter>())
                graph.AddChild(m);

            // Create all ui based on model
            view.controller.ApplyChanges();

            view.ClearSelection();

            var elements = view.graphElements.ToList();


            List<VFXNodeUI> newSlotContainerUIs = new List<VFXNodeUI>();
            List<VFXContextUI> newContainerUIs = new List<VFXContextUI>();

            foreach (var slotContainer in allSerializedObjects.OfType<VFXContext>())
            {
                VFXContextUI contextUI = elements.OfType<VFXContextUI>().FirstOrDefault(t => t.controller.model == slotContainer);
                if (contextUI != null)
                {
                    newSlotContainerUIs.Add(contextUI.ownData);
                    newSlotContainerUIs.AddRange(contextUI.GetAllBlocks().Cast<VFXNodeUI>());
                    newContainerUIs.Add(contextUI);
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
            foreach (var slotContainer in allSerializedObjects.OfType<VFXParameter>())
            {
                VFXParameterUI slotContainerUI = elements.OfType<VFXParameterUI>().FirstOrDefault(t => t.controller.model == slotContainer);
                if (slotContainerUI != null)
                {
                    newSlotContainerUIs.Add(slotContainerUI);
                    view.AddToSelection(slotContainerUI);
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
                if (newContainerUIs.Contains(flowEdge.input.GetFirstAncestorOfType<VFXContextUI>()))
                {
                    view.AddToSelection(flowEdge);
                }
            }
        }
    }
}
