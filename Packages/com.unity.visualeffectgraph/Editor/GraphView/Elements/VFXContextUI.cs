using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;
using UnityEngine.UIElements;
using UnityEngine.Profiling;
using System.Reflection;

using PositionType = UnityEngine.UIElements.Position;

namespace UnityEditor.VFX.UI
{
    class VFXContextUI : VFXNodeUI
    {
        // TODO: Unused except for debugging
        readonly CustomStyleProperty<Color> RectColorProperty = new CustomStyleProperty<Color>("--rect-color");

        Image m_HeaderIcon;
        Image m_HeaderSpace;

        VisualElement m_Footer;
        Image m_FooterIcon;
        Label m_FooterTitle;

        VisualElement m_FlowInputConnectorContainer;
        VisualElement m_FlowOutputConnectorContainer;
        VisualElement m_BlockContainer;
        VisualElement m_NoBlock;

        VisualElement m_DragDisplay;

        Label m_Label;
        TextField m_TextField;

        public new VFXContextController controller
        {
            get { return base.controller as VFXContextController; }
        }
        protected override void OnNewController()
        {
            var blocks = new List<VFXModelDescriptor<VFXBlock>>(VFXLibrary.GetBlocks());

            m_CanHaveBlocks = blocks.Any(t => controller.model.AcceptChild(t.model));
        }

        public bool canHaveBlocks { get => m_CanHaveBlocks; }

        public static string ContextEnumToClassName(string name)
        {
            if (name[0] == 'k')
            {
                Debug.LogError("Fix this since k should have been removed from enums");
            }

            return name.ToLower();
        }

        public void UpdateLabel()
        {
            var graph = controller.model.GetGraph();
            if (graph != null && controller.model.contextType == VFXContextType.Spawner)
                m_Label.text = graph.systemNames.GetUniqueSystemName(controller.model.GetData());
            else
                m_Label.text = controller.model.label;
        }

        protected override void SelfChange()
        {
            base.SelfChange();

            Profiler.BeginSample("VFXContextUI.CreateBlockProvider");
            if (m_BlockProvider == null)
            {
                m_BlockProvider = new VFXBlockProvider(controller, (d, mPos) =>
                {
                    if (d is VFXBlockProvider.NewBlockDescriptor)
                    {
                        UpdateSelectionWithNewBlocks();
                        AddBlock(mPos, (d as VFXBlockProvider.NewBlockDescriptor).newBlock);
                    }
                    else
                    {
                        var subgraphBlock = AssetDatabase.LoadAssetAtPath<VisualEffectSubgraphBlock>((d as VFXBlockProvider.SubgraphBlockDescriptor).item.path);

                        int blockIndex = GetDragBlockIndex(mPos);
                        VFXBlock newModel = ScriptableObject.CreateInstance<VFXSubgraphBlock>();

                        newModel.SetSettingValue("m_Subgraph", subgraphBlock);
                        UpdateSelectionWithNewBlocks();
                        using (var growContext = new GrowContext(this))
                        {
                            controller.AddBlock(blockIndex, newModel, true);
                        }
                    }
                });
            }
            Profiler.EndSample();

            if (inputContainer.childCount == 0 && !hasSettings)
            {
                mainContainer.AddToClassList("empty");
            }
            else
            {
                mainContainer.RemoveFromClassList("empty");
            }

            m_Divider.visible = hasSettings;

            m_HeaderIcon.image = GetIconForVFXType(controller.model.inputType);
            m_HeaderIcon.visible = m_HeaderIcon.image != null;


            Profiler.BeginSample("VFXContextUI.SetAllStyleClasses");

            VFXContextType contextType = controller.model.contextType;
            foreach (VFXContextType value in System.Enum.GetValues(typeof(VFXContextType)))
            {
                if (value != contextType)
                    RemoveFromClassList(ContextEnumToClassName(value.ToString()));
            }
            AddToClassList(ContextEnumToClassName(contextType.ToString()));

            var inputType = controller.model.inputType;
            if (inputType == VFXDataType.None)
            {
                inputType = controller.model.ownedType;
            }
            foreach (VFXDataType value in System.Enum.GetValues(typeof(VFXDataType)))
            {
                if (inputType != value)
                    RemoveFromClassList("inputType" + ContextEnumToClassName(value.ToString()));
            }
            AddToClassList("inputType" + ContextEnumToClassName(inputType.ToString()));

            var outputType = controller.model.outputType;
            foreach (VFXDataType value in System.Enum.GetValues(typeof(VFXDataType)))
            {
                if (value != outputType)
                    RemoveFromClassList("outputType" + ContextEnumToClassName(value.ToString()));
            }
            AddToClassList("outputType" + ContextEnumToClassName(outputType.ToString()));

            var type = controller.model.ownedType;
            foreach (VFXDataType value in System.Enum.GetValues(typeof(VFXDataType)))
            {
                if (value != type)
                    RemoveFromClassList("type" + ContextEnumToClassName(value.ToString()));
            }
            AddToClassList("type" + ContextEnumToClassName(type.ToString()));

            var space = controller.model.space;
            foreach (VFXCoordinateSpace val in System.Enum.GetValues(typeof(VFXCoordinateSpace)))
            {
                if (val != space || !controller.model.spaceable)
                    m_HeaderSpace.RemoveFromClassList("space" + val.ToString());
            }
            if (controller.model.spaceable)
                m_HeaderSpace.AddToClassList("space" + (controller.model.space).ToString());

            Profiler.EndSample();
            if (controller.model.outputType == VFXDataType.None)
            {
                if (m_Footer.parent != null)
                    m_Footer.RemoveFromHierarchy();
            }
            else
            {
                if (m_Footer.parent == null)
                    mainContainer.Add(m_Footer);

                if (controller.model.outputFlowSlot.Any())
                {
                    m_FooterTitle.text = controller.model.outputType.ToString();
                    m_FooterIcon.image = GetIconForVFXType(controller.model.outputType);
                }
                else
                {
                    m_FooterTitle.text = string.Empty;
                    m_FooterIcon.image = null;
                }
                m_FooterIcon.visible = m_FooterIcon.image != null;
            }

            Profiler.BeginSample("VFXContextUI.CreateInputFlow");
            HashSet<VisualElement> newInAnchors = new HashSet<VisualElement>();
            foreach (var inanchorcontroller in controller.flowInputAnchors.Take(VFXContext.kMaxFlowCount))
            {
                var existing = m_FlowInputConnectorContainer.Children().Select(t => t as VFXFlowAnchor).FirstOrDefault(t => t.controller == inanchorcontroller);
                if (existing == null)
                {
                    var anchor = VFXFlowAnchor.Create(inanchorcontroller);
                    m_FlowInputConnectorContainer.Add(anchor);
                    newInAnchors.Add(anchor);
                }
                else
                {
                    newInAnchors.Add(existing);
                }
            }

            foreach (var nonLongerExistingAnchor in m_FlowInputConnectorContainer.Children().Where(t => !newInAnchors.Contains(t)).ToList()) // ToList to make a copy because the enumerable will change when we delete
            {
                m_FlowInputConnectorContainer.Remove(nonLongerExistingAnchor);
            }
            Profiler.EndSample();

            Profiler.BeginSample("VFXContextUI.CreateInputFlow");
            HashSet<VisualElement> newOutAnchors = new HashSet<VisualElement>();

            foreach (var outanchorcontroller in controller.flowOutputAnchors.Take(VFXContext.kMaxFlowCount))
            {
                var existing = m_FlowOutputConnectorContainer.Children().Select(t => t as VFXFlowAnchor).FirstOrDefault(t => t.controller == outanchorcontroller);
                if (existing == null)
                {
                    var anchor = VFXFlowAnchor.Create(outanchorcontroller);
                    m_FlowOutputConnectorContainer.Add(anchor);
                    newOutAnchors.Add(anchor);
                }
                else
                {
                    newOutAnchors.Add(existing);
                }
            }

            foreach (var nonLongerExistingAnchor in m_FlowOutputConnectorContainer.Children().Where(t => !newOutAnchors.Contains(t)).ToList()) // ToList to make a copy because the enumerable will change when we delete
            {
                m_FlowOutputConnectorContainer.Remove(nonLongerExistingAnchor);
            }
            Profiler.EndSample();

            UpdateLabel();

            if (string.IsNullOrEmpty(m_Label.text))
            {
                m_Label.AddToClassList("empty");
            }
            else
            {
                m_Label.RemoveFromClassList("empty");
            }

            foreach (var inEdge in m_FlowInputConnectorContainer.Children().OfType<VFXFlowAnchor>().SelectMany(t => t.connections))
                inEdge.UpdateEdgeControl();
            foreach (var outEdge in m_FlowOutputConnectorContainer.Children().OfType<VFXFlowAnchor>().SelectMany(t => t.connections))
                outEdge.UpdateEdgeControl();

            RefreshContext();
        }

        VisualElement m_Divider;

        public VFXContextUI() : base("uxml/VFXContext")
        {
            capabilities |= Capabilities.Selectable | Capabilities.Movable | Capabilities.Deletable | Capabilities.Ascendable;

            styleSheets.Add(VFXView.LoadStyleSheet("VFXContext"));
            styleSheets.Add(VFXView.LoadStyleSheet("Selectable"));

            AddToClassList("VFXContext");
            AddToClassList("selectable");

            this.mainContainer.style.overflow = Overflow.Visible;


            m_Divider = this.mainContainer.Q("divider");

            m_FlowInputConnectorContainer = this.Q("flow-inputs");

            m_FlowOutputConnectorContainer = this.Q("flow-outputs");

            m_HeaderIcon = titleContainer.Q<Image>("icon");
            m_HeaderSpace = titleContainer.Q<Image>("header-space");
            m_HeaderSpace.AddManipulator(new Clickable(OnSpace));

            m_BlockContainer = this.Q("block-container");
            m_NoBlock = m_BlockContainer.Q("no-blocks");

            m_Footer = this.Q("footer");

            m_FooterTitle = m_Footer.Q<Label>("title-label");
            m_FooterIcon = m_Footer.Q<Image>("icon");


            m_DragDisplay = new VisualElement();
            m_DragDisplay.AddToClassList("dragdisplay");

            m_Label = this.Q<Label>("user-label");
            m_TextField = this.Q<TextField>("user-title-textfield");
            m_TextField.maxLength = 175;
            m_TextField.style.display = DisplayStyle.None;

            m_Label.RegisterCallback<MouseDownEvent>(OnTitleMouseDown);
            m_TextField.RegisterCallback<ChangeEvent<string>>(OnTitleChange);
            m_TextField.Q(TextField.textInputUssName).RegisterCallback<FocusOutEvent>(OnTitleBlur, TrickleDown.TrickleDown);

            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
            RegisterCallback<DragExitedEvent>(OnDragExited);
            RegisterCallback<DragLeaveEvent>(OnDragExited);
        }

        bool m_CanHaveBlocks = false;
        void OnSpace()
        {
            if (controller.model.space == VFXCoordinateSpace.World)
                controller.model.space = VFXCoordinateSpace.Local;
            else
                controller.model.space = VFXCoordinateSpace.World;
        }

        public bool CanDrop(IEnumerable<VFXBlockUI> blocks)
        {
            bool accept = true;
            if (blocks.Count() == 0) return false;
            foreach (var block in blocks)
            {
                if (!controller.model.AcceptChild(block.controller.model))
                {
                    accept = false;
                    break;
                }
            }
            return accept;
        }

        public override bool HitTest(Vector2 localPoint)
        {
            // needed so that if we click on a block we won't select the context as well.
            /*if (m_NoBlock.parent ==  null && m_BlockContainer.ContainsPoint(this.ChangeCoordinatesTo(m_BlockContainer, localPoint)))
            {
                return false;
            }*/
            return ContainsPoint(localPoint);
        }

        public void DraggingBlocks(IEnumerable<VFXBlockUI> blocks, int index)
        {
            m_DragDisplay.RemoveFromHierarchy();

            if (!CanDrop(blocks))
            {
                return;
            }

            float y = GetBlockIndexY(index, false);

            m_DragDisplay.style.top = y;

            m_BlockContainer.Add(m_DragDisplay);
        }

        public void RemoveDragIndicator()
        {
            if (m_DragDisplay.parent != null)
                m_BlockContainer.Remove(m_DragDisplay);
        }

        bool m_DragStarted;


        public bool CanAcceptDrop(List<ISelectable> selection)
        {
            IEnumerable<VFXBlockUI> blocksUI = selection.Select(t => t as VFXBlockUI).Where(t => t != null);

            return CanDrop(blocksUI);
        }

        public float GetBlockIndexY(int index, bool middle)
        {
            float y = 0;
            if (controller.blockControllers.Count == 0)
            {
                return 0;
            }
            if (index >= controller.blockControllers.Count)
            {
                return blocks[controller.blockControllers.Last()].layout.yMax;
            }
            else if (middle)
            {
                return blocks[controller.blockControllers[index]].layout.center.y;
            }
            else
            {
                y = blocks[controller.blockControllers[index]].layout.yMin;

                if (index > 0)
                {
                    y = (y + blocks[controller.blockControllers[index - 1]].layout.yMax) * 0.5f;
                }
            }

            return y;
        }

        public int GetDragBlockIndex(Vector2 mousePosition)
        {
            for (int i = 0; i < controller.blockControllers.Count; ++i)
            {
                float y = GetBlockIndexY(i, true);

                if (mousePosition.y < y)
                {
                    return i;
                }
            }

            return controller.blockControllers.Count;
        }

        void OnDragUpdated(DragUpdatedEvent evt)
        {
            Vector2 mousePosition = m_BlockContainer.WorldToLocal(evt.mousePosition);

            int blockIndex = GetDragBlockIndex(mousePosition);

            if (DragAndDrop.GetGenericData("DragSelection") is List<ISelectable> dragSelection)
            {
                var blocksUI = dragSelection.OfType<VFXBlockUI>().ToArray();

                DragAndDrop.visualMode = evt.ctrlKey ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
                DraggingBlocks(blocksUI, blockIndex);
                if (!m_DragStarted)
                {
                    // TODO: Do something on first DragUpdated event (initiate drag)
                    m_DragStarted = true;
                    AddToClassList("dropping");
                }
                else
                {
                    // TODO: Do something on subsequent DragUpdated events
                }
            }
            else
            {
                var references = DragAndDrop.objectReferences.OfType<VisualEffectSubgraphBlock>();

                if (references.Any() && (!controller.viewController.model.isSubgraph || !references.Any(t => t.GetResource().GetOrCreateGraph().subgraphDependencies.Contains(controller.viewController.model.subgraph) || t.GetResource() == controller.viewController.model)))
                {
                    var compatibleReferences = references
                        .Where(x => x != null && x.GetResource().GetOrCreateGraph().children.OfType<VFXBlockSubgraphContext>().First().compatibleContextType.HasFlag(controller.model.contextType));

                    if (compatibleReferences.Any())
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        evt.StopPropagation();
                        DraggingBlocks(Enumerable.Empty<VFXBlockUI>(), blockIndex);
                        if (!m_DragStarted)
                        {
                            // TODO: Do something on first DragUpdated event (initiate drag)
                            m_DragStarted = true;
                            AddToClassList("dropping");
                        }
                    }
                    else
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                        evt.StopPropagation();
                    }
                }
            }
        }

        void OnDragPerform(DragPerformEvent evt)
        {
            RemoveDragIndicator();
            if (DragAndDrop.GetGenericData("DragSelection") is List<ISelectable> dragSelection)
            {
                Vector2 mousePosition = m_BlockContainer.WorldToLocal(evt.mousePosition);

                var blocksUI = dragSelection.OfType<VFXBlockUI>().ToArray();
                if (!CanDrop(blocksUI))
                    return;

                int blockIndex = GetDragBlockIndex(mousePosition);

                BlocksDropped(blockIndex, blocksUI, evt.ctrlKey);

                DragAndDrop.AcceptDrag();

                m_DragStarted = false;
                RemoveFromClassList("dropping");
            }
            else
            {
                var references = DragAndDrop.objectReferences.OfType<VisualEffectSubgraphBlock>().ToArray();

                if (references.Any() && (!controller.viewController.model.isSubgraph || !references.Any(t => t.GetResource().GetOrCreateGraph().subgraphDependencies.Contains(controller.viewController.model.subgraph) || t.GetResource() == controller.viewController.model)))
                {
                    foreach (var reference in references)
                    {
                        if (reference != null && reference.GetResource().GetOrCreateGraph().children.OfType<VFXBlockSubgraphContext>().First().compatibleContextType.HasFlag(controller.model.contextType))
                        {
                            DragAndDrop.AcceptDrag();
                            Vector2 mousePosition = m_BlockContainer.WorldToLocal(evt.mousePosition);

                            int blockIndex = GetDragBlockIndex(mousePosition);
                            VFXBlock newModel = ScriptableObject.CreateInstance<VFXSubgraphBlock>();

                            newModel.SetSettingValue("m_Subgraph", reference);

                            UpdateSelectionWithNewBlocks();
                            controller.AddBlock(blockIndex, newModel);
                        }
                        else if (reference != null)
                        {
                            Debug.LogWarning($"Could not drag & drop asset '{reference.name}' because it's not supported in a context of type '{controller.model.contextType}'");
                        }
                    }

                    evt.StopPropagation();
                }
            }

            m_DragStarted = false;
            RemoveFromClassList("dropping");
        }

        public void BlocksDropped(int blockIndex, IEnumerable<VFXBlockUI> draggedBlocks, bool copy)
        {
            HashSet<VFXContextController> contexts = new HashSet<VFXContextController>();
            foreach (var draggedBlock in draggedBlocks)
            {
                contexts.Add(draggedBlock.context.controller);
            }

            using (var growContext = new GrowContext(this))
            {
                controller.BlocksDropped(blockIndex, draggedBlocks.Select(t => t.controller), copy);

                foreach (var context in contexts)
                {
                    context.ApplyChanges();
                }
            }
        }

        void OnDragExited(EventBase e)
        {
            // TODO: Do something when current drag is canceled
            RemoveDragIndicator();
            m_DragStarted = false;
        }

        public void RemoveBlock(VFXBlockUI block)
        {
            if (block == null)
                return;

            controller.RemoveBlock(block.controller.model);
        }

        private VFXBlockUI InstantiateBlock(VFXBlockController blockController)
        {
            Profiler.BeginSample("VFXContextUI.InstantiateBlock");
            Profiler.BeginSample("VFXContextUI.new VFXBlockUI");
            var blockUI = new VFXBlockUI();
            Profiler.EndSample();
            blockUI.controller = blockController;
            blocks[blockController] = blockUI;
            Profiler.EndSample();

            return blockUI;
        }

        Dictionary<VFXBlockController, VFXBlockUI> blocks = new Dictionary<VFXBlockController, VFXBlockUI>();


        public void RefreshContext()
        {
            Profiler.BeginSample("VFXContextUI.RefreshContext");
            var blockControllers = controller.blockControllers;
            int blockControllerCount = blockControllers.Count();

            bool somethingChanged = m_BlockContainer.childCount < blockControllerCount || (!m_CanHaveBlocks && m_NoBlock.parent != null);

            int cptBlock = 0;
            foreach (var child in m_BlockContainer.Children().OfType<VFXBlockUI>())
            {
                if (!somethingChanged && blockControllerCount > cptBlock && child.controller != blockControllers[cptBlock])
                {
                    somethingChanged = true;
                }
                cptBlock++;
            }
            if (somethingChanged || cptBlock != blockControllerCount)
            {
                VFXView view = GetFirstAncestorOfType<VFXView>();

                foreach (var controllerToRemove in blocks.Keys.Except(blockControllers).ToArray())
                {
                    view.RemoveNodeEdges(blocks[controllerToRemove]);
                    m_BlockContainer.Remove(blocks[controllerToRemove]);
                    blocks.Remove(controllerToRemove);
                }
                if (blockControllers.Any() || !m_CanHaveBlocks)
                {
                    m_NoBlock.RemoveFromHierarchy();
                }
                else if (m_NoBlock.parent == null)
                {
                    m_BlockContainer.Add(m_NoBlock);
                }
                if (blockControllers.Any())
                {
                    VFXBlockUI prevBlock = null;

                    var addedBlocks = new List<ISelectable>();
                    foreach (var blockController in blockControllers)
                    {
                        if (!blocks.TryGetValue(blockController, out var blockUI))
                        {
                            blockUI = InstantiateBlock(blockController);
                            m_BlockContainer.Insert(prevBlock == null ? 0 : m_BlockContainer.IndexOf(prevBlock) + 1, blockUI);

                            if (m_UpdateSelectionWithNewBlocks)
                            {
                                addedBlocks.Add(blockUI);
                            }
                            //Refresh error can only be called after the block has been instantiated
                            blockController.model.RefreshErrors();
                        }

                        if (prevBlock != null)
                            blockUI.PlaceInFront(prevBlock);
                        else
                        {
                            blockUI.SendToBack();
                            blockUI.AddToClassList("first");
                        }

                        prevBlock = blockUI;
                    }

                    if (addedBlocks.Any())
                    {
                        view.ClearSelection();
                        view.AddRangeToSelection(addedBlocks);
                    }

                    m_UpdateSelectionWithNewBlocks = false;
                }
            }
            Profiler.EndSample();
        }

        Texture2D GetIconForVFXType(VFXDataType type)
        {
            switch (type)
            {
                case VFXDataType.SpawnEvent:
                    return VFXView.LoadImage("Execution");
                case VFXDataType.Particle:
                    return VFXView.LoadImage("Particles");
                case VFXDataType.ParticleStrip:
                    return VFXView.LoadImage("ParticleStrips");
            }
            return null;
        }

        internal class GrowContext : IDisposable
        {
            VFXContextUI m_Context;
            float m_PrevSize;
            public GrowContext(VFXContextUI context)
            {
                m_Context = context;
                m_PrevSize = context.layout.size.y;
            }

            void IDisposable.Dispose()
            {
                VFXView view = m_Context.GetFirstAncestorOfType<VFXView>();
                m_Context.controller.ApplyChanges();
                m_Context.panel.InternalValidateLayout();

                view.PushUnderContext(m_Context, m_Context.layout.size.y - m_PrevSize);
            }
        }

        void AddBlock(Vector2 position, VFXModelDescriptor<VFXBlock> descriptor)
        {
            int blockIndex = -1;

            var blocks = m_BlockContainer.Query().OfType<VFXBlockUI>().ToList();
            for (int i = 0; i < blocks.Count; ++i)
            {
                Rect worldBounds = blocks[i].worldBound;
                if (worldBounds.Contains(position))
                {
                    if (position.y > worldBounds.center.y)
                    {
                        blockIndex = i + 1;
                    }
                    else
                    {
                        blockIndex = i;
                    }
                    break;
                }
            }

            using (new GrowContext(this))
            {
                controller.AddBlock(blockIndex, descriptor.CreateInstance(), true /* freshly created block, should init space */);
            }
        }

        public void OnCreateBlock(DropdownMenuAction evt)
        {
            Vector2 referencePosition = evt.eventInfo.mousePosition;

            OnCreateBlock(referencePosition);
        }

        public void OnCreateBlock(Vector2 referencePosition)
        {
            VFXView view = GetFirstAncestorOfType<VFXView>();

            Vector2 screenPosition = view.ViewToScreenPosition(referencePosition);

            var window = VFXViewWindow.GetWindow(view);
            VFXFilterWindow.Show(window, referencePosition, screenPosition, m_BlockProvider);
        }

        VFXBlockProvider m_BlockProvider = null;

        // TODO: Remove, unused except for debugging
        // Declare new USS rect-color and use it
        protected override void OnCustomStyleResolved(ICustomStyle styles)
        {
            base.OnCustomStyleResolved(styles);
            styles.TryGetValue(RectColorProperty, out m_RectColor);
        }

        // TODO: Remove, unused except for debugging
        Color m_RectColor = Color.magenta;
        Color rectColor { get { return m_RectColor; } }

        public IEnumerable<VFXBlockUI> GetAllBlocks()
        {
            foreach (VFXBlockUI block in m_BlockContainer.Children().OfType<VFXBlockUI>())
            {
                yield return block;
            }
        }

        public IEnumerable<Port> GetAllAnchors(bool input, bool output)
        {
            return (IEnumerable<Port>)GetFlowAnchors(input, output);
        }

        public IEnumerable<VFXFlowAnchor> GetFlowAnchors(bool input, bool output)
        {
            if (input)
                foreach (VFXFlowAnchor anchor in m_FlowInputConnectorContainer.Children())
                {
                    yield return anchor;
                }
            if (output)
                foreach (VFXFlowAnchor anchor in m_FlowOutputConnectorContainer.Children())
                {
                    yield return anchor;
                }
        }

        public class VFXContextOnlyVFXNodeProvider : VFXNodeProvider
        {
            public VFXContextOnlyVFXNodeProvider(VFXViewController controller, Action<Descriptor, Vector2> onAddBlock, Func<Descriptor, bool> filter) :
                base(controller, onAddBlock, filter, new Type[] { typeof(VFXContext) })
            {
            }

            protected override string GetCategory(Descriptor desc)
            {
                return string.Empty;
            }
        }

        bool ProviderFilter(VFXNodeProvider.Descriptor d)
        {
            VFXModelDescriptor desc = d.modelDescriptor as VFXModelDescriptor;
            if (desc == null)
                return false;

            if (!(desc.model is VFXAbstractParticleOutput))
                return false;

            foreach (var links in controller.model.inputFlowSlot.Select((t, i) => new { index = i, links = t.link }))
            {
                foreach (var link in links.links)
                {
                    if (!VFXContext.CanLink(link.context, (VFXContext)desc.model, links.index, link.slotIndex))
                        return false;
                }
            }

            return (desc.model as VFXContext).contextType == VFXContextType.Output;
        }

        void OnConvertContext(DropdownMenuAction action)
        {
            VFXView view = this.GetFirstAncestorOfType<VFXView>();
            var window = VFXViewWindow.GetWindow(view);
            VFXFilterWindow.Show(window, action.eventInfo.mousePosition, view.ViewToScreenPosition(action.eventInfo.mousePosition), new VFXContextOnlyVFXNodeProvider(view.controller, ConvertContext, ProviderFilter));
        }

        void ConvertContext(VFXNodeProvider.Descriptor d, Vector2 mPos)
        {
            VFXView view = GetFirstAncestorOfType<VFXView>();
            VFXViewController viewController = controller.viewController;
            if (view == null) return;

            mPos = view.contentViewContainer.ChangeCoordinatesTo(view, controller.position);
            var newNodeController = view.AddNode(d, mPos);
            var newContextController = newNodeController as VFXContextController;
            newContextController.model.label = controller.model.label;

            //transfer blocks
            foreach (var block in controller.model.children.ToArray()) // To array needed as the IEnumerable content will change
                newContextController.AddBlock(-1, block);


            //transfer settings
            var contextType = controller.model.GetType();
            foreach (var setting in newContextController.model.GetSettings(true))
            {
                if (!newContextController.model.CanTransferSetting(setting))
                    continue;

                if (!setting.valid || setting.field.GetCustomAttributes(typeof(VFXSettingAttribute), true).Length == 0)
                    continue;

                var sourceSetting = controller.model.GetSetting(setting.name);
                if (!sourceSetting.valid)
                    continue;

                object value;
                if (VFXConverter.TryConvertTo(sourceSetting.value, setting.field.FieldType, out value))
                    newContextController.model.SetSettingValue(setting.field.Name, value);
            }

            //transfer flow edges
            if (controller.flowInputAnchors.Count == 1)
            {
                foreach (var output in controller.flowInputAnchors[0].connections.Select(t => t.output).ToArray())
                    newContextController.model.LinkFrom(output.context.model, output.slotIndex);
            }

            // Apply the slot changes that can be the result of settings changes
            newContextController.ApplyChanges();

            VFXSlot firstTextureSlot = null;

            //transfer master slot values
            foreach (var slot in newContextController.model.inputSlots)
            {
                VFXSlot mySlot = controller.model.inputSlots.FirstOrDefault(t => t.name == slot.name);
                if (mySlot == null)
                {
                    if (slot.valueType == VFXValueType.Texture2D && firstTextureSlot == null)
                        firstTextureSlot = slot;
                    continue;
                }

                object value;
                if (VFXConverter.TryConvertTo(mySlot.value, slot.property.type, out value))
                    slot.value = value;
            }
            //Hack to copy the first texture in the first texture slot if not found by name
            if (firstTextureSlot != null)
            {
                VFXSlot mySlot = controller.model.inputSlots.FirstOrDefault(t => t.valueType == VFXValueType.Texture2D);

                if (mySlot != null)
                    firstTextureSlot.value = mySlot.value;
            }

            foreach (var anchor in newContextController.inputPorts)
            {
                string path = anchor.path;
                var myAnchor = controller.inputPorts.FirstOrDefault(t => t.path == path);

                if (myAnchor == null || !myAnchor.HasLink())
                    continue;

                //There should be only one
                var output = myAnchor.connections.First().output;

                viewController.CreateLink(anchor, output);
            }

            // Apply the change so that it won't unlink the blocks links
            controller.ApplyChanges();

            viewController.RemoveElement(controller);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is VFXContextUI || evt.target is VFXBlockUI)
            {
                if (m_CanHaveBlocks)
                {
                    evt.menu.InsertAction(0, "Create Block", OnCreateBlock, e => DropdownMenuAction.Status.Normal);
                    evt.menu.AppendSeparator();
                }
            }

            if (evt.target is VFXContextUI && controller.model is VFXAbstractParticleOutput)
            {
                evt.menu.InsertAction(1, "Convert Output", OnConvertContext, e => DropdownMenuAction.Status.Normal);
            }
        }

        void OnTitleMouseDown(MouseDownEvent e)
        {
            if (e.clickCount == 2)
            {
                OnRename();
                e.StopPropagation();
                e.PreventDefault();
            }
        }

        public void OnRename()
        {
            m_Label.RemoveFromClassList("empty");
            m_Label.style.display = DisplayStyle.None;
            m_TextField.value = m_Label.text;
            m_TextField.style.display = DisplayStyle.Flex;
            m_TextField.Q(TextField.textInputUssName).Focus();
            m_TextField.SelectAll();
        }

        void OnTitleBlur(FocusOutEvent e)
        {
            controller.model.label = m_TextField.value
                .Trim()
                .Replace("/", "")
                .Replace("\\", "")
                .Replace(":", "")
                .Replace("<", "")
                .Replace(">", "")
                .Replace("*", "")
                .Replace("?", "")
                .Replace("\"", "")
                .Replace("|", "")
            ;
            m_TextField.style.display = DisplayStyle.None;
            m_Label.style.display = DisplayStyle.Flex;
        }

        void OnTitleChange(ChangeEvent<string> e)
        {
            m_Label.text = m_TextField.value;
        }

        bool m_UpdateSelectionWithNewBlocks;
        public void UpdateSelectionWithNewBlocks()
        {
            m_UpdateSelectionWithNewBlocks = true;
        }
    }
}
