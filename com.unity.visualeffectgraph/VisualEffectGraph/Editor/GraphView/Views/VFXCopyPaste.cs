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
        class Data
        {
            public VFXContext[] contexts;
            public VFXData[] data;
            public VFXModel[] slotContainers;
            public VFXBlock[] blocks;
            public DataEdge[] dataEdges;
            public FlowEdge[] flowEdges;
        }


        static void CopyNodes(Data copyData, IEnumerable<GraphElementPresenter> elements, IEnumerable<VFXContextPresenter> contexts, IEnumerable<VFXSlotContainerPresenter> slotContainers)
        {
            IEnumerable<VFXSlotContainerPresenter> dataEdgeTargets = slotContainers.Concat(contexts.Select(t => t.slotPresenter as VFXSlotContainerPresenter)).Concat(contexts.SelectMany(t => t.blockPresenters).Cast<VFXSlotContainerPresenter>()).ToArray();

            // consider only edges contained in the selection

            IEnumerable<VFXDataEdgePresenter> dataEdges = elements.OfType<VFXDataEdgePresenter>().Where(t => dataEdgeTargets.Contains((t.input as VFXDataAnchorPresenter).sourceNode as VFXSlotContainerPresenter) && dataEdgeTargets.Contains((t.output as VFXDataAnchorPresenter).sourceNode as VFXSlotContainerPresenter)).ToArray();
            IEnumerable<VFXFlowEdgePresenter> flowEdges = elements.OfType<VFXFlowEdgePresenter>().Where(t =>
                    contexts.Contains((t.input as VFXFlowAnchorPresenter).context) &&
                    contexts.Contains((t.output as VFXFlowAnchorPresenter).context)
                    ).ToArray();


            VFXContext[] copiedContexts = contexts.Select(t => t.context).ToArray();
            copyData.contexts = copiedContexts.Select(t => t.Clone<VFXContext>()).ToArray();
            VFXModel[] copiedSlotContainers = slotContainers.Select(t => t.model).ToArray();
            copyData.slotContainers = copiedSlotContainers.Select(t => t.Clone<VFXModel>()).ToArray();

            copyData.dataEdges = new DataEdge[dataEdges.Count()];
            int cpt = 0;
            foreach (var edge in dataEdges)
            {
                DataEdge copyPasteEdge = new DataEdge();

                var inputPresenter = edge.input as VFXDataAnchorPresenter;
                var outputPresenter = edge.output as VFXDataAnchorPresenter;

                copyPasteEdge.input.slotPath = MakeSlotPath(inputPresenter.model, true);

                if (inputPresenter.model.owner is VFXContext)
                {
                    VFXContext context = inputPresenter.model.owner as VFXContext;
                    copyPasteEdge.inputContext = true;
                    copyPasteEdge.input.targetIndex = System.Array.IndexOf(copiedContexts, context);
                    copyPasteEdge.inputBlockIndex = -1;
                }
                else if (inputPresenter.model.owner is VFXBlock)
                {
                    VFXBlock block = inputPresenter.model.owner as VFXBlock;
                    copyPasteEdge.inputContext = true;
                    copyPasteEdge.input.targetIndex = System.Array.IndexOf(copiedContexts, block.GetParent());
                    copyPasteEdge.inputBlockIndex = block.GetParent().GetIndex(block);
                }
                else
                {
                    copyPasteEdge.inputContext = false;
                    copyPasteEdge.input.targetIndex = System.Array.IndexOf(copiedSlotContainers, inputPresenter.model.owner as VFXModel);
                    copyPasteEdge.inputBlockIndex = -1;
                }


                copyPasteEdge.output.slotPath = MakeSlotPath(outputPresenter.model, false);
                copyPasteEdge.output.targetIndex = System.Array.IndexOf(copiedSlotContainers, outputPresenter.model.owner as VFXModel);

                copyData.dataEdges[cpt++] = copyPasteEdge;
            }


            copyData.flowEdges = new FlowEdge[flowEdges.Count()];
            cpt = 0;
            foreach (var edge in flowEdges)
            {
                FlowEdge copyPasteEdge = new FlowEdge();

                var inputPresenter = edge.input as VFXFlowAnchorPresenter;
                var outputPresenter = edge.output as VFXFlowAnchorPresenter;

                copyPasteEdge.input.contextIndex = System.Array.IndexOf(copiedContexts, inputPresenter.owner);
                copyPasteEdge.input.flowIndex = inputPresenter.slotIndex;
                copyPasteEdge.output.contextIndex = System.Array.IndexOf(copiedContexts, outputPresenter.owner);
                copyPasteEdge.output.flowIndex = outputPresenter.slotIndex;

                copyData.flowEdges[cpt++] = copyPasteEdge;
            }

            copyData.data = VFXContext.ReproduceDataSettings(copiedContexts.Select((t, i) => new KeyValuePair<VFXContext, VFXContext>(t, copyData.contexts[i])).ToList()).ToArray();
        }

        public static object CreateCopy(IEnumerable<GraphElementPresenter> elements)
        {
            IEnumerable<VFXContextPresenter> contexts = elements.OfType<VFXContextPresenter>();
            IEnumerable<VFXSlotContainerPresenter> slotContainers = elements.Where(t => t is VFXOperatorPresenter || t is VFXParameterPresenter).Cast<VFXSlotContainerPresenter>();
            IEnumerable<VFXBlockPresenter> blocks = elements.OfType<VFXBlockPresenter>();

            Data copyData = new Data();

            if (contexts.Count() == 0 && slotContainers.Count() == 0 && blocks.Count() > 0)
            {
                VFXBlock[] copiedBlocks = blocks.Select(t => t.block).ToArray();
                copyData.blocks = copiedBlocks.Select(t => t.Clone<VFXBlock>()).ToArray();
            }
            else
            {
                CopyNodes(copyData, elements, contexts, slotContainers);
            }

            return copyData;
        }

        public static string SerializeElements(IEnumerable<GraphElementPresenter> elements)
        {
            return JsonUtility.ToJson(CreateCopy(elements));
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
            Data copyData = (Data)JsonUtility.FromJson<Data>(data);

            PasteCopy(view, pasteOffset, copyData);
        }

        public static void PasteCopy(VFXView view, Vector2 pasteOffset, object data)
        {
            Data copyData = (Data)data;

            if (copyData.blocks != null && copyData.blocks.Length > 0)
            {
                PasteBlocks(view, copyData);
            }
            else
            {
                PasteNodes(view, pasteOffset, copyData);
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
                targetBlock = selectedBlocks.OrderByDescending(t => t.context.GetPresenter<VFXContextPresenter>().context.GetIndex(t.GetPresenter<VFXBlockPresenter>().block)).First();
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

            VFXContext targetModelContext = targetContext.GetPresenter<VFXContextPresenter>().context;

            int targetIndex = -1;
            if (targetBlock != null)
            {
                targetIndex = targetModelContext.GetIndex(targetBlock.GetPresenter<VFXBlockPresenter>().block) + 1;
            }

            var newBlocks = new HashSet<VFXBlock>();

            foreach (var block in copyData.blocks)
            {
                var newBlock = block.Clone<VFXBlock>();
                newBlocks.Add(newBlock);

                targetModelContext.AddChild(newBlock, targetIndex, false); // only notify once after all blocks have been added
            }

            targetModelContext.Invalidate(VFXModel.InvalidationCause.kStructureChanged);

            // Create all ui based on model
            targetContext.OnDataChanged();

            view.ClearSelection();

            foreach (var uiBlock in targetContext.Query().OfType<VFXBlockUI>().Where(t => newBlocks.Contains(t.GetPresenter<VFXBlockPresenter>().block)).ToList())
            {
                view.AddToSelection(uiBlock);
            }
        }

        static void PasteNodes(VFXView view, Vector2 pasteOffset, Data copyData)
        {
            var graph = view.GetPresenter<VFXViewPresenter>().GetGraph();

            List<VFXContext> newContexts = new List<VFXContext>(copyData.contexts.Length);

            foreach (var slotContainer in copyData.contexts)
            {
                var newContext = slotContainer.Clone<VFXContext>();
                newContext.position += pasteOffset;
                newContexts.Add(newContext);
            }

            List<VFXModel> newSlotContainers = new List<VFXModel>(copyData.slotContainers.Length);

            foreach (var slotContainer in copyData.slotContainers)
            {
                var newSlotContainer = slotContainer.Clone<VFXModel>();
                newSlotContainer.position += pasteOffset;
                newSlotContainers.Add(newSlotContainer);
            }

            foreach (var dataEdge in copyData.dataEdges)
            {
                VFXSlot inputSlot = null;
                if (dataEdge.inputContext)
                {
                    VFXContext targetContext = newContexts[dataEdge.input.targetIndex];
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
                    VFXModel model = newSlotContainers[dataEdge.input.targetIndex];
                    inputSlot = FetchSlot(model as IVFXSlotContainer, dataEdge.input.slotPath, true);
                }

                VFXSlot outputSlot = FetchSlot(newSlotContainers[dataEdge.output.targetIndex] as IVFXSlotContainer, dataEdge.output.slotPath, false);

                if (inputSlot != null && outputSlot != null)
                    inputSlot.Link(outputSlot);
            }


            foreach (var flowEdge in copyData.flowEdges)
            {
                VFXContext inputContext = newContexts[flowEdge.input.contextIndex];
                VFXContext outputContext = newContexts[flowEdge.output.contextIndex];

                inputContext.LinkFrom(outputContext, flowEdge.input.flowIndex, flowEdge.output.flowIndex);
            }

            VFXContext.ReproduceDataSettings(copyData.contexts.Select((t, i) => new KeyValuePair<VFXContext, VFXContext>(t, newContexts[i])).ToList()).ToArray();

            foreach (var m in newContexts)
                graph.AddChild(m);
            foreach (var m in newSlotContainers)
                graph.AddChild(m);

            // Create all ui based on model
            view.OnDataChanged();

            view.ClearSelection();

            var elements = view.graphElements.ToList();


            List<VFXNodeUI> newSlotContainerUIs = new List<VFXNodeUI>();
            List<VFXContextUI> newContainerUIs = new List<VFXContextUI>();

            foreach (var slotContainer in newContexts)
            {
                VFXContextUI contextUI = elements.OfType<VFXContextUI>().FirstOrDefault(t => t.GetPresenter<VFXContextPresenter>().model == slotContainer);
                if (contextUI != null)
                {
                    newSlotContainerUIs.Add(contextUI.ownData);
                    newSlotContainerUIs.AddRange(contextUI.GetAllBlocks().Cast<VFXNodeUI>());
                    newContainerUIs.Add(contextUI);
                    view.AddToSelection(contextUI);
                }
            }
            foreach (var slotContainer in newSlotContainers)
            {
                VFXStandaloneSlotContainerUI slotContainerUI = elements.OfType<VFXStandaloneSlotContainerUI>().FirstOrDefault(t => t.GetPresenter<VFXSlotContainerPresenter>().model == slotContainer);
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
